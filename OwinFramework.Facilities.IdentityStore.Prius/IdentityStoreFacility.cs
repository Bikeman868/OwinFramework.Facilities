using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using OwinFramework.Builder;
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
                        command.AddParameter("identity", identity);
                        context.ExecuteNonQuery(command);
                    }
                }

                using (var command = _commandFactory.CreateStoredProcedure("sp_AddCredential"))
                {
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

            using (var context = _contextFactory.Create(_configuration.PriusRepositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetUserNameCredential"))
                {
                    command.AddParameter("userName", userName);
                    using (var rows = context.ExecuteEnumerable<CredentialRecord>(command))
                    {
                        credential = rows.FirstOrDefault();
                        if (credential == null)
                        {
                            return new AuthenticationResult
                            {
                                Status = AuthenticationStatus.NotFound
                            };
                        }
                    }
                }
            }

            var result = new AuthenticationResult
            {
                Identity = credential.Identity,
                Purposes = SplitPurposes(credential.Purposes),
                Status = AuthenticationStatus.Authenticated
            };

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

            return result;
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
            byte[] passwordBytes;
            byte[] dataToHash;
            HashAlgorithm hashProvider;

            if (version == 1)
            {
                passwordBytes = Encoding.UTF8.GetBytes(password);
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
