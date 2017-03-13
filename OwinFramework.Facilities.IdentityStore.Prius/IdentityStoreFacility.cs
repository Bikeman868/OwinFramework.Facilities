﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using OwinFramework.Builder;
using OwinFramework.Facilities.IdentityStore.Prius.DataContracts;
using OwinFramework.Facilities.IdentityStore.Prius.Exceptions;
using OwinFramework.Facilities.IdentityStore.Prius.Records;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;
using Prius.Contracts.Interfaces;
using Prius.Contracts.Interfaces.Connections;

namespace OwinFramework.Facilities.IdentityStore.Prius
{
    internal class IdentityStoreFacility: IIdentityStore
    {
        private readonly IContextFactory _contextFactory;
        private readonly ICommandFactory _commandFactory;

        private readonly IDisposable _configurationChange;
        private Configuration _configuration;

        public IdentityStoreFacility(
            IConfiguration configuration,
            IContextFactory contextFactory,
            ICommandFactory commandFactory)
        {
            _contextFactory = contextFactory;
            _commandFactory = commandFactory;

            _configurationChange = configuration.Register(
                "/OwinFramework/Facility/IdentityStore.Prius",
                c => _configuration = c, 
                new Configuration());
        }

        public string CreateIdentity()
        {
            var identity = "urn:" + _configuration.IdentityUrnNamespace + ":" 
                + Guid.NewGuid().ToShortString(false).ToLower();

            using (var command = _commandFactory.CreateStoredProcedure("sp_AddIdentity"))
            {
                command.AddParameter("who_identity", identity);
                command.AddParameter("reason", "Create identity");
                command.AddParameter("identity", identity);
                using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
                {
                    using (var reader = context.ExecuteReader(command))
                    {
                        if (!reader.Read())
                            throw new IdentityStoreException("The new identity failed to be added to the database");
                    }
                }
            }

            return identity;
        }

        #region Certificates

        public bool SupportsCertificates
        {
            get { return false; }
        }

        public byte[] AddCertificate(string identity, TimeSpan? lifetime = null, IEnumerable<string> purposes = null)
        {
            throw new NotImplementedException();
        }

        public IAuthenticationResult AuthenticateWithCertificate(byte[] certificate)
        {
            return new AuthenticationResult
            {
                Status = AuthenticationStatus.Unsupported
            };
        }

        public bool DeleteCertificate(byte[] cerificate)
        {
            return false;
        }

        public int DeleteCertificates(string identity)
        {
            return 0;
        }

        #endregion

        #region Credentials

        public bool SupportsCredentials
        {
            get { return true; }
        }

        public bool AddCredentials(string identity, string userName, string password, bool replaceExisting = true, IEnumerable<string> purposes = null)
        {
            CheckUserNameAllowed(userName);
            CheckPasswordAllowed(password);

            var purposeString = JoinPurposes(purposes);

            byte[] hash;
            byte[] salt;
            int version;
            ComputeHash(password, out version, out salt, out hash);

            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                CheckIdentityExists(context, identity);
                CheckUserNameAvailable(context, identity, userName);

                if (replaceExisting)
                {
                    using (var command = _commandFactory.CreateStoredProcedure("sp_DeleteIdentityCredentials"))
                    {
                        command.AddParameter("who_identity", identity);
                        command.AddParameter("reason", "Replace existing");
                        command.AddParameter("identity", identity);
                        context.ExecuteNonQuery(command);
                    }
                }

                using (var command = _commandFactory.CreateStoredProcedure("sp_AddCredential"))
                {
                    command.AddParameter("who_identity", identity);
                    command.AddParameter("reason", "Add credentials");
                    command.AddParameter("identity", identity);
                    command.AddParameter("userName", userName);
                    command.AddParameter("purposes", purposeString);
                    command.AddParameter("version", version);
                    command.AddParameter("hash", hash);
                    command.AddParameter("salt", salt);
                    using (var reader = context.ExecuteReader(command))
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public IAuthenticationResult AuthenticateWithCredentials(string userName, string password)
        {
            CredentialRecord credential;
            var result = new AuthenticationResult
            {
                Status = AuthenticationStatus.Authenticated
            };

            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetUserNameCredential"))
                {
                    command.AddParameter("userName", userName);
                    using (var rows = context.ExecuteEnumerable<CredentialRecord>(command))
                    {
                        credential = rows.FirstOrDefault();
                    }
                }
            }

            if (credential == null)
            {
                result.Status = AuthenticationStatus.NotFound;
            }
            else
            {
                result.Identity = credential.Identity;
                result.Purposes = SplitPurposes(credential.Purposes);

                if (credential.Locked.HasValue)
                {
                    if (credential.Locked.Value + _configuration.LockDuration < DateTime.UtcNow)
                    {
                        using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
                        {
                            using (var command = _commandFactory.CreateStoredProcedure("sp_UnlockUsername"))
                            {
                                command.AddParameter("userName", userName);
                                context.ExecuteNonQuery(command);
                            }
                        }
                    }
                    else
                    {
                        result.Status = AuthenticationStatus.Locked;
                    }
                }
            }

            if (result.Status == AuthenticationStatus.Authenticated)
            {
                byte[] hash;
                ComputeHash(password, credential.Version, credential.Salt, out hash);

                if (hash.Length != credential.Hash.Length)
                {
                    result.Status = AuthenticationStatus.InvalidCredentials;
                }
                else
                {
                    for (var i = 0; i < hash.Length; i++)
                    {
                        if (hash[i] != credential.Hash[i])
                        {
                            result.Status = AuthenticationStatus.InvalidCredentials;
                            break;
                        }
                    }
                }
            }

            switch (result.Status)
            {
                case AuthenticationStatus.Authenticated:
                {
                    result.RememberMeToken = Guid.NewGuid().ToShortString(false);
                    using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
                    {
                        using (var command = _commandFactory.CreateStoredProcedure("sp_AuthenticateSuccess"))
                        {
                            command.AddParameter("identity", credential.Identity);
                            command.AddParameter("purposes", credential.Purposes);
                            command.AddParameter("remember_me_token", result.RememberMeToken);
                            command.AddParameter("authenticate_method", "Credentials");
                            command.AddParameter("method_id", credential.CredentialId);
                            command.AddParameter("expires", DateTime.UtcNow + _configuration.RememberMeFor);
                            context.ExecuteNonQuery(command);
                        }
                    }
                    break;
                }
                case AuthenticationStatus.InvalidCredentials:
                {
                    using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
                    {
                        int failCount;
                        using (var command = _commandFactory.CreateStoredProcedure("sp_AuthenticateFail"))
                        {
                            command.AddParameter("identity", credential.Identity);
                            command.AddParameter("authenticate_method", "Credentials");
                            command.AddParameter("method_id", credential.CredentialId);
                            var failCountParam = command.AddParameter("fail_count",SqlDbType.Int);
                            context.ExecuteNonQuery(command);
                            failCount = (int)Convert.ChangeType(failCountParam.Value, typeof(int));
                        }
                        if (failCount >= _configuration.FailedLoginsToLock)
                        {
                            using (var command = _commandFactory.CreateStoredProcedure("sp_LockUsername"))
                            {
                                command.AddParameter("userName", userName);
                                context.ExecuteNonQuery(command);
                            }
                        }
                    }
                    break;
                }
            }

            return result;
        }

        public IAuthenticationResult RememberMe(string rememberMeToken)
        {
            AuthenticateRecord authenticateRecord;
            var result = new AuthenticationResult
            {
                Status = AuthenticationStatus.Authenticated,
                RememberMeToken = rememberMeToken
            };
            
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetAuthenticationToken"))
                {
                    command.AddParameter("remember_me_token", rememberMeToken);
                    using (var rows = context.ExecuteEnumerable<AuthenticateRecord>(command))
                    {
                        authenticateRecord = rows.FirstOrDefault();
                    }
                }
            }

            if (authenticateRecord == null)
            {
                result.Status = AuthenticationStatus.NotFound;
            }
            else
            {
                if (authenticateRecord.Expires.HasValue && authenticateRecord.Expires > DateTime.UtcNow)
                {
                    result.Status = AuthenticationStatus.Expired;
                }
                else
                {
                    result.Identity = authenticateRecord.Identity;
                    result.Purposes = SplitPurposes(authenticateRecord.Purposes);
                }
            }
            return result;
        }

        public bool ChangePassword(ICredential credential, string newPassword)
        {
            CheckPasswordAllowed(newPassword);

            byte[] hash;
            byte[] salt;
            int version;
            ComputeHash(newPassword, out version, out salt, out hash);

            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_UpdateCredentialPassword"))
                {
                    command.AddParameter("who_identity", credential.Identity);
                    command.AddParameter("reason", "Change password");
                    command.AddParameter("userName", credential.Username);
                    command.AddParameter("version", version);
                    command.AddParameter("hash", hash);
                    command.AddParameter("salt", salt);
                    using (var reader = context.ExecuteReader(command))
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public bool DeleteCredential(ICredential credential)
        {
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_DeleteUsernameCredentials"))
                {
                    command.AddParameter("who_identity", credential.Identity);
                    command.AddParameter("reason", "Delete credential");
                    command.AddParameter("username", credential.Username);
                    var rowsAffected = context.ExecuteNonQuery(command);
                    return rowsAffected > 0;
                }
            }
        }

        public IEnumerable<ICredential> GetCredentials(string identity)
        {
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetIdentityCredentials"))
                {
                    command.AddParameter("identity", identity);
                    using (var credentialRecords = context.ExecuteEnumerable<CredentialRecord>(command))
                    {
                        return credentialRecords.Select(c =>
                            new Credential
                            {
                                Identity = c.Identity,
                                Username = c.UserName,
                                Purposes = SplitPurposes(c.Purposes).ToList()
                            });
                    }
                }
            }
        }

        public ICredential GetRememberMeCredential(string rememberMeToken)
        {
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                AuthenticateRecord authenticateRecord;
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetAuthenticationToken"))
                {
                    command.AddParameter("remember_me_token", rememberMeToken);
                    using (var rows = context.ExecuteEnumerable<AuthenticateRecord>(command))
                    {
                        authenticateRecord = rows.FirstOrDefault();
                    }
                }
                if (authenticateRecord == null) return null;

                var result = new Credential
                {
                    Identity = authenticateRecord.Identity,
                    Purposes = SplitPurposes(authenticateRecord.Purposes).ToList()
                };

                if (string.Equals(authenticateRecord.AuthenticateMethod, "Credentials", StringComparison.OrdinalIgnoreCase))
                {
                    using (var command = _commandFactory.CreateStoredProcedure("sp_GetCredential"))
                    {
                        command.AddParameter("credential_id", authenticateRecord.MethodId);
                        using (var rows = context.ExecuteEnumerable<CredentialRecord>(command))
                        {
                            var credentialRecord = rows.FirstOrDefault();
                            result.Username = credentialRecord.UserName;
                        }
                    }
                }
                return result;
            }
        }

        public ICredential GetUsernameCredential(string username)
        {
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetUsernameCredential"))
                {
                    command.AddParameter("username", username);
                    using (var rows = context.ExecuteEnumerable<CredentialRecord>(command))
                    {
                        var credentialRecord = rows.FirstOrDefault();
                        if (credentialRecord == null) return null;
                        return new Credential
                        {
                            Identity = credentialRecord.Identity,
                            Username = credentialRecord.UserName,
                            Purposes = SplitPurposes(credentialRecord.Purposes).ToList()
                        };
                    }
                }
            }
        }

        private void CheckIdentityExists(IContext context, string identity)
        {
            using (var command = _commandFactory.CreateStoredProcedure("sp_GetIdentity"))
            {
                command.AddParameter("identity", identity);
                using (var rows = context.ExecuteEnumerable<IdentityRecord>(command))
                {
                    if (rows.FirstOrDefault() == null)
                        throw new InvalidIdentityException("This is not a valid identity");
                }
            }
        }

        private void CheckUserNameAvailable(IContext context, string identity, string userName)
        {
            using (var command = _commandFactory.CreateStoredProcedure("sp_GetUserNameCredential"))
            {
                command.AddParameter("userName", userName);
                using (var rows = context.ExecuteEnumerable<CredentialRecord>(command))
                {
                    var credential = rows.FirstOrDefault();
                    if (credential != null && credential.Identity != identity)
                        throw new InvalidUserNameException("This user name is not available");
                }
            }
        }

        private void CheckUserNameAllowed(string userName)
        {
            if (userName == null || userName.Length < _configuration.MinimumUserNameLength)
                throw new InvalidUserNameException("This user name is too short");

            if (userName.Length > _configuration.MaximumUserNameLength)
                throw new InvalidUserNameException("This user name is too long");

            var regex = new Regex(_configuration.UserNameRegex);
            if (!regex.IsMatch(userName))
                throw new InvalidUserNameException("User name contains invalid characters, it must match the pattern "
                    + _configuration.UserNameRegex);
        }

        private void CheckPasswordAllowed(string password)
        {
            if (password == null || password.Length < _configuration.MinimumPasswordLength)
                throw new InvalidPasswordException("This password is too short");

            if (password.Length > _configuration.MaximumPasswordLength)
                throw new InvalidPasswordException("This password is too long");

            var regex = new Regex(_configuration.PasswordRegex);
            if (!regex.IsMatch(password))
                throw new InvalidPasswordException("Password contains invalid characters, it must match the pattern "
                    + _configuration.PasswordRegex);
        }

        private void ComputeHash(string password, out int version, out byte[] salt, out byte[] hash)
        {
            version = 1;

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            salt = Guid.NewGuid().ToByteArray();

            var dataToHash = new byte[salt.Length + passwordBytes.Length];
            salt.CopyTo(dataToHash, 0);
            passwordBytes.CopyTo(dataToHash, salt.Length);

            var hashProvider = new SHA256CryptoServiceProvider();
            hash = hashProvider.ComputeHash(dataToHash);
        }

        private void ComputeHash(string password, int version, byte[] salt, out byte[] hash)
        {
            byte[] dataToHash;
            HashAlgorithm hashProvider;

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            if (version == 1)
            {
                dataToHash = new byte[salt.Length + passwordBytes.Length];
                salt.CopyTo(dataToHash, 0);
                passwordBytes.CopyTo(dataToHash, salt.Length);
                hashProvider = new SHA256CryptoServiceProvider();
            }
            else
                throw new IdentityStoreException("Unsupported version of password hashing scheme. This database may have been created with a newer version of this software.");

            hash = hashProvider.ComputeHash(dataToHash);
        }

        #endregion

        #region Shared secrets

        public bool SupportsSharedSecrets
        {
            get { return true; }
        }

        public string AddSharedSecret(string identity, string name, IList<string> purposes)
        {
            var secret = Guid.NewGuid().ToShortString();
            var purposeString = JoinPurposes(purposes);

            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                CheckIdentityExists(context, identity);

                using (var command = _commandFactory.CreateStoredProcedure("sp_AddSharedSecret"))
                {
                    command.AddParameter("identity", identity);
                    command.AddParameter("name", name);
                    command.AddParameter("secret", secret);
                    command.AddParameter("purposes", purposeString);
                    using (var reader = context.ExecuteReader(command))
                    {
                        if (!reader.Read())
                            throw new IdentityStoreException("Failed to add shared secret");
                    }
                }
            }

            return secret;
        }

        public IAuthenticationResult AuthenticateWithSharedSecret(string sharedSecret)
        {
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetSharedSecret"))
                {
                    command.AddParameter("secret", sharedSecret);
                    using(var rows = context.ExecuteEnumerable<SharedSecretRecord>(command))
                    {
                        var secret = rows.FirstOrDefault();
                        if (secret == null)
                        {
                            return new AuthenticationResult
                            {
                                Status = AuthenticationStatus.NotFound
                            };
                        }
                        return new AuthenticationResult
                        {
                            Identity = secret.Identity,
                            Purposes = SplitPurposes(secret.Purposes),
                            Status = AuthenticationStatus.Authenticated
                        };
                    }
                }
            }
        }

        public bool DeleteSharedSecret(string sharedSecret)
        {
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_DeleteSharedSecret"))
                {
                    command.AddParameter("secret", sharedSecret);
                    var rowsAffected = context.ExecuteNonQuery(command);
                    return rowsAffected == 1;
                }
            }
        }

        public IList<ISharedSecret> GetAllSharedSecrets(string identity)
        {
            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetIdentitySharedSecrets"))
                {
                    command.AddParameter("identity", identity);
                    using(var rows = context.ExecuteEnumerable<SharedSecretRecord>(command))
                    {
                        return rows
                            .Select(r => new SharedSecret
                                {
                                    Name = r.Name,
                                    Secret = r.Secret,
                                    Purposes = SplitPurposes(r.Purposes)
                                })
                            .ToList<ISharedSecret>();
                    }
                }
            }
        }

        #endregion

        #region Social login

        public bool AddSocial(string identity, string userId, string socialService, string authenticationToken, IEnumerable<string> purposes = null, bool replaceExisting = true)
        {
            throw new NotImplementedException();
        }

        public bool DeleteAllSocial(string identity)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSocial(string identity, string socialService)
        {
            throw new NotImplementedException();
        }

        public ISocialAuthentication GetSocialAuthentication(string userId, string socialService)
        {
            throw new NotImplementedException();
        }

        public IList<string> SocialServices
        {
            get { return new List<string>(); }
        }

        #endregion
    
        private bool IsValidPurpose(string purpose)
        {
            if (string.IsNullOrEmpty(purpose))
                throw new IdentityStoreException("Purpose can not be an empty string");

            if (purpose.Length > 32)
                throw new IdentityStoreException("Purpose exceeds maximum length");

            if (purpose.Any(c => ", \"".Contains(c)))
                throw new IdentityStoreException("Purpose contains invalid characters");

            return true;
        }

        private string JoinPurposes(IEnumerable<string> purposes)
        {
            if (purposes == null)
                return null;

            return string.Join(",", purposes.Where(IsValidPurpose));
        }

        private IList<string> SplitPurposes(string purposes)
        {
            if (string.IsNullOrEmpty(purposes))
                return new List<string>();
            return purposes.Split(',');
        }
    }
}
