using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OwinFramework.Builder;
using OwinFramework.Facilities.TokenStore.Prius.DataContracts;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;
using OwinFramework.Facilities.TokenStore.Prius.Rules;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;
using Prius.Contracts.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius
{
    /// <summary>
    /// Defines a simple implementation of the ITokenStore facilicy that stores
    /// tokens using the ICache facility.
    /// </summary>
    internal class TokenStoreFacility: ITokenStore
    {
        private readonly ITokenFactory _tokenFactory;
        private readonly ITokenDatabase _tokenDatabase;

        private readonly IDisposable _configurationRegistration;
        private Configuration _configuration;

        public TokenStoreFacility(
            ITokenFactory tokenFactory,
            ITokenDatabase tokenDatabase,
            IConfiguration configuration)
        {
            _tokenFactory = tokenFactory;
            _tokenDatabase = tokenDatabase;

            _configurationRegistration = configuration.Register(
                "/owinFramework/facility/tokenStore.Cache", 
                c => _configuration = c, 
                new Configuration());
        }

        public string CreateToken(string tokenType, string purpose, string identity)
        {
            return CreateToken(tokenType, Enumerable.Repeat(purpose, 1), identity);
        }

        public string CreateToken(string tokenType, IEnumerable<string> purpose, string identity)
        {
            var value = Guid.NewGuid().ToShortString();

            var token = _tokenFactory.CreateToken(tokenType, identity, purpose);
            var json = token.Serialize();

            _tokenDatabase.AddToken(value, tokenType, json.ToString());

            return value;
        }

        public bool DeleteToken(string token)
        {
            return _tokenDatabase.DeleteToken(token);
        }

        public IToken GetToken(string tokenType, string tokenString, string purpose, string identity)
        {
            var result = new TokenResponse
            {
                Value = tokenString,
                Identity = identity,
                Purpose = purpose,
                Status = TokenStatus.Invalid
            };

            var tokenRecord = _tokenDatabase.GetToken(tokenString);
            var token = _tokenFactory.CreateToken(tokenRecord);
            if (token == null) return result;

            var isModified = false;
            result.Status = TokenStatus.Allowed;

            foreach (var validator in token.Validators)
            {
                var checkResult = validator.CheckIsValid(identity, purpose);
                if (checkResult.IsStatusModified) isModified = true;

                if (checkResult.Validity == Validity.TemporaryInvalid ||
                    checkResult.Validity == Validity.PermenantInvalid)
                {
                    result.Status = TokenStatus.NotAllowed;
                    break;
                }
            }

            if (isModified)
            {
                var json = token.Serialize();
                if (json != null)
                    _tokenDatabase.UpdateToken(tokenRecord.Id, json.ToString());
            }

            return result;
        }

    }
}
