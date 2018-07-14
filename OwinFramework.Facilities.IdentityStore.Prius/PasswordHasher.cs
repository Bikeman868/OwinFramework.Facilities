using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using OwinFramework.Facilities.IdentityStore.Prius.Exceptions;
using OwinFramework.InterfacesV1.Facilities;
using OwinFramework.Interfaces.Builder;

namespace OwinFramework.Facilities.IdentityStore.Prius
{
    internal class PasswordHasher: IPasswordHasher
    {
        private readonly IDisposable _configurationChange;
        private Configuration _configuration;

        private string _passwordPolicy;
        private Regex _passwordValidationRegex;

        private readonly IDictionary<int, IPasswordHashingScheme> _schemes = new Dictionary<int, IPasswordHashingScheme>();
        private int _latestVersion = 1;

        public PasswordHasher(
            IConfiguration configuration)
        {
            _configurationChange = configuration.Register(
                "/owinFramework/facility/identityStore.Prius",
                SetConfiguration, 
                new Configuration());

            SetHashingScheme(1, new SchemeVersion1());
        }

        private void SetConfiguration(Configuration configuration)
        {
            _configuration = configuration;

            _passwordPolicy = "Password must be between " + configuration.MinimumPasswordLength +
                              " and " + configuration.MaximumPasswordLength +
                              "characters long and mach this regular expression " +
                              configuration.PasswordRegex;

            _passwordValidationRegex = new Regex(configuration.PasswordRegex, RegexOptions.Compiled | RegexOptions.Singleline);
        }

        public PasswordCheckResult CheckPasswordAllowed(string identity, string password)
        {
            var result = new PasswordCheckResult
            {
                PasswordPolicy = _passwordPolicy
            };

            if (password == null || password.Length < _configuration.MinimumPasswordLength)
                result.ValidationError = "This password is too short";
            else if (password.Length > _configuration.MaximumPasswordLength)
                result.ValidationError = "This password is too long";
            else if (!_passwordValidationRegex.IsMatch(password))
                result.ValidationError = "Password contains invalid characters, it must match the pattern " + _passwordValidationRegex;
            else
                result.IsAllowed = true;

            return result;
        }

        public void ComputeHash(string identity, string password, ref int? version, out byte[] salt, out byte[] hash)
        {
            version = version ?? _latestVersion;
            salt = null;

            var scheme = GetHashingScheme(version.Value);
            hash = scheme.ComputeHash(password, ref salt);
        }

        public void ComputeHash(string password, int version, byte[] salt, out byte[] hash)
        {
            if (salt == null) throw new ArgumentNullException("salt");

            var scheme = GetHashingScheme(version);
            hash = scheme.ComputeHash(password, ref salt);
        }

        public IPasswordHashingScheme GetHashingScheme(int version)
        {
            IPasswordHashingScheme scheme;
            lock(_schemes)
                if (!_schemes.TryGetValue(version, out scheme))
                    throw new InvalidPasswordException("No hashing scheme installed for version " + version +
                        " of the password hash. This password was probably hashed by a newer version of the software");
            return scheme;
        }

        public void SetHashingScheme(int version, IPasswordHashingScheme scheme)
        {
            lock (_schemes)
                _schemes[version] = scheme;

            _latestVersion = version > _latestVersion ? version : _latestVersion;
        }

        private class SchemeVersion1 : IPasswordHashingScheme
        {
            public byte[] ComputeHash(string password, ref byte[] salt)
            {
                if (salt == null) salt = Guid.NewGuid().ToByteArray();

                var passwordBytes = Encoding.UTF8.GetBytes(password);

                var dataToHash = new byte[salt.Length + passwordBytes.Length];
                salt.CopyTo(dataToHash, 0);
                passwordBytes.CopyTo(dataToHash, salt.Length);

                var hashProvider = new SHA256CryptoServiceProvider();
                return hashProvider.ComputeHash(dataToHash);
            }
        }
    }
}
