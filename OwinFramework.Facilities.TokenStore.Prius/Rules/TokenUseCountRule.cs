using System.Threading;
using Newtonsoft.Json;
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

            public string Serialize()
            {
                return JsonConvert.SerializeObject(RemainingUsages);
            }

            public void Hydrate(string serializedData)
            {
                RemainingUsages = JsonConvert.DeserializeObject<int>(serializedData);
            }
        }
    }
}