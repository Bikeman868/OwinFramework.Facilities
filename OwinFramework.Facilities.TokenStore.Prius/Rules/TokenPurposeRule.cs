using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public class TokenPurposeRule : ITokenValidationRule, ITokenValidator
    {
        private List<string> _purposes;

        public string Name { get { return "purpose"; } }
        
        public ITokenValidator Initialize(IEnumerable<string> purposes)
        {
            if (purposes == null)
                _purposes = null;
            else
                _purposes = purposes.Select(p => p.ToLower()).ToList();
            return this;
        }

        public CheckResult CheckIsValid(string identity, string purpose)
        {
            var result = new CheckResult();

            if (_purposes == null)
                result.Validity = Validity.Valid;

            else if (string.IsNullOrEmpty(purpose))
                result.Validity = Validity.TemporaryInvalid;

            else
                result.Validity = _purposes.Contains(purpose.ToLower()) ? Validity.Valid : Validity.TemporaryInvalid;

            return result;
        }

        public bool CheckIsExpired()
        {
            return false;
        }

        public JObject Serialize()
        {
            return new JObject { { "p", new JArray(_purposes) } };
        }

        public void Hydrate(JObject json)
        {
            var purposes = json.Value<JArray>("p");
            _purposes = purposes.Select(t => t.Value<string>()).ToList();
        }

        public ITokenValidator GetInstance()
        {
            return this;
        }
    }
}