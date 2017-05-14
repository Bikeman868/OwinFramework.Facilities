using System.Collections.Generic;
using System.Linq;
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
                var purposeList = purposes.ToList();
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

    }
}