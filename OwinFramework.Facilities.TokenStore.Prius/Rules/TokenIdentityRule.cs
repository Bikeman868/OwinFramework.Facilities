using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public class TokenIdentityRule : ITokenValidationRule, ITokenValidator
    {
        private string _identity;

        public string Name { get { return "identity"; } }
        
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

        public JObject Serialize()
        {
            return new JObject { { "i", _identity } };
        }

        public void Hydrate(JObject json)
        {
            _identity = json.Value<string>("i");
        }

        public ITokenValidator GetInstance()
        {
            return this;
        }
    }
}