using System;
using Newtonsoft.Json;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public class TokenIdentityRule : ITokenValidationRule, ITokenValidator
    {
        private string _identity;

        public ITokenValidator Initialize(string identity)
        {
            _identity = identity;
            return this;
        }

        public CheckResult CheckIsValid(string identity, string purpose)
        {
            var result = new CheckResult();

            if (_identity == null)
                result.Validity = Validity.Valid;

            else if (string.IsNullOrEmpty(identity))
                result.Validity = Validity.TemporaryInvalid;

            else
                result.Validity = string.Equals(_identity, identity, StringComparison.Ordinal) 
                    ? Validity.Valid : Validity.TemporaryInvalid;

            return result;
        }

        public bool CheckIsExpired()
        {
            return false;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(_identity);
        }

        public void Hydrate(string serializedData)
        {
            _identity = JsonConvert.DeserializeObject<string>(serializedData);
        }

        public ITokenValidator GetInstance()
        {
            return this;
        }
    }
}