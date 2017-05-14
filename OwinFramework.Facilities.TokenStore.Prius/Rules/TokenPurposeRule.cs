using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public class TokenPurposeRule : ITokenValidationRule, ITokenValidator
    {
        private IList<string> _purposes;

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

        public string Serialize()
        {
            return JsonConvert.SerializeObject(_purposes);
        }

        public void Hydrate(string serializedData)
        {
            _purposes = JsonConvert.DeserializeObject<IList<string>>(serializedData);
        }

        public ITokenValidator GetInstance()
        {
            return this;
        }
    }
}