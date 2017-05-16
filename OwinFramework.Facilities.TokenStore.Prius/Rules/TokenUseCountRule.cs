using System;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public class UseCountRuleConfig
    {
        [JsonProperty("maxUseCount")]
        public int MaximumUseCount { get; set; }
    }

    [Rule(RuleName = "UseCount", ConfigType = typeof(UseCountRuleConfig))]
    public class TokenUseCountRule : TokenRule
    {
        private UseCountRuleConfig _config;

        public override ITokenValidationRule Initialize(object config)
        {
            _config = (UseCountRuleConfig)config;
            return this;
        }

        public override ITokenValidator GetInstance()
        {
            if(_config != null)
                return new Instance { RemainingUsages = _config.MaximumUseCount };

            return new Instance();
        }

        private class Instance : ITokenValidator
        {
            public string Name { get { return "uses"; } }
            
            public int RemainingUsages;

            public CheckResult CheckIsValid(string identity, string purpose)
            {
                var result = new CheckResult
                {
                    IsStatusModified = true
                };
                var decrementedValue = Interlocked.Decrement(ref RemainingUsages);
                result.Validity = decrementedValue >= 0 ? Validity.Valid : Validity.PermenantInvalid;
                return result;
            }

            public bool CheckIsExpired()
            {
                return RemainingUsages <= 0;
            }

            public JObject Serialize()
            {
                return new JObject 
                {
                    {"r", new JArray(RemainingUsages)}
                };
            }

            public void Hydrate(JObject json)
            {
                RemainingUsages = json.Value<int>("r");
            }
        }
    }
}