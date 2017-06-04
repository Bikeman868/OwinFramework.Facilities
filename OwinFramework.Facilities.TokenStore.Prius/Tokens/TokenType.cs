using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;
using OwinFramework.Facilities.TokenStore.Prius.Rules;

namespace OwinFramework.Facilities.TokenStore.Prius.Tokens
{
    public class TokenType: ITokenType
    {
        private readonly IList<ITokenValidationRule> _rules = new List<ITokenValidationRule>();

        public IList<ITokenValidator> GetValidators(string identity, IEnumerable<string> purposes)
        {
            var validators = new List<ITokenValidator>();

            if (purposes != null)
            {
                var purposeList = purposes.Where(p => !string.IsNullOrEmpty(p)).ToList();
                if (purposeList.Count > 0)
                    validators.Add(new TokenPurposeRule().Initialize(purposeList));
            }

            if (!string.IsNullOrEmpty(identity))
            {
                validators.Add(new TokenIdentityRule().Initialize(identity));
            }

            validators.AddRange(_rules.Select(r => r.GetInstance()));
            return validators;
        }

        public void AddValidation(ITokenValidationRule rule)
        {
            _rules.Add(rule);
        }

        public IList<ITokenValidator> GetValidators(JObject json)
        {
            var result = new List<ITokenValidator>();

            var purposeRule = new TokenPurposeRule();
            var purposeData = json.Value<JObject>(purposeRule.Name);
            if (purposeData != null)
            {
                purposeRule.Hydrate(purposeData);
                result.Add(purposeRule);
            }

            var identityRule = new TokenIdentityRule();
            var identityData = json.Value<JObject>(identityRule.Name);
            if (identityData != null)
            {
                identityRule.Hydrate(identityData);
                result.Add(identityRule);
            }

            foreach (var rule in _rules)
            {
                var instance = rule.GetInstance();
                var instanceData = json.Value<JObject>(instance.Name);
                if (instanceData != null)
                    instance.Hydrate(instanceData);
                result.Add(instance);
            }

            return result;
        }
    }
}