using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public class ExpiryRuleConfig
    {
        [JsonProperty("expiryTime")]
        public TimeSpan ExpiryTime { get; set; }
    }

    [Rule(RuleName = "Expiry", ConfigType = typeof(ExpiryRuleConfig))]
    public class TokenExpiryRule : TokenRule
    {
        private ExpiryRuleConfig _config;

        public override ITokenValidationRule Initialize(object config)
        {
            _config = (ExpiryRuleConfig)config;
            return this;
        }

        public override ITokenValidator GetInstance()
        {
            if(_config != null)
                return new Instance { Expiry = DateTime.UtcNow + _config.ExpiryTime };
            
            return new Instance();;
        }

        private class Instance : ITokenValidator
        {
            public string Name { get { return "expiry"; } }

            public DateTime Expiry;

            public CheckResult CheckIsValid(string identity, string purpose)
            {
                return new CheckResult
                {
                    Validity = DateTime.UtcNow < Expiry 
                        ? Validity.Valid 
                        : Validity.PermenantInvalid
                };
            }

            public bool CheckIsExpired()
            {
                return DateTime.UtcNow >= Expiry;
            }

            public JObject Serialize()
            {
                return new JObject {{"d", Expiry}};
            }

            public void Hydrate(JObject json)
            {
                Expiry = json.Value<DateTime>("d");
            }
        }
    }
}