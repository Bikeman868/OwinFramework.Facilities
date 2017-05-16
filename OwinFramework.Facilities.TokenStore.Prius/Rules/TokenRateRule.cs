using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public class RateRuleConfig
    {
        [JsonProperty("window")]
        public TimeSpan Window { get; set; }

        [JsonProperty("maxUseCount")]
        public int MaximumUseCount { get; set; }
    }

    [Rule(RuleName = "Rate", ConfigType = typeof(RateRuleConfig))]
    public class TokenRateRule : TokenRule
    {
        private RateRuleConfig _config;

        public override ITokenValidationRule Initialize(object config)
        {
            _config = (RateRuleConfig)config;
            return this;
        }

        public override ITokenValidator GetInstance()
        {
            if(_config != null)
                return new Instance { TimeWindow = _config.Window, MaximumUseCount = _config.MaximumUseCount };
            
            return new Instance();;
        }

        private class Instance : ITokenValidator
        {
            public string Name { get { return "rate"; } }
            
            public TimeSpan TimeWindow;
            public int MaximumUseCount;

            private int _currentCount;
            private DateTime _endTime;
            private readonly object _locker = new object();

            public CheckResult CheckIsValid(string identity, string purpose)
            {
                var result = new CheckResult { IsStatusModified = true };

                lock (_locker)
                {
                    if (DateTime.UtcNow > _endTime)
                    {
                        _endTime = DateTime.UtcNow + TimeWindow;
                        _currentCount = 1;
                        result.Validity = Validity.Valid;
                    }
                    else result.Validity = ++_currentCount > MaximumUseCount
                        ? Validity.TemporaryInvalid : Validity.Valid;
                }
                return result;
            }

            public bool CheckIsExpired()
            {
                return false;
            }

            public JObject Serialize()
            {
                lock (_locker)
                {
                    return new JObject 
                    {
                        {"c", new JArray(_currentCount)},
                        {"e", new JArray(_endTime)},
                    };
                }
            }

            public void Hydrate(JObject json)
            {
                lock (_locker)
                {
                    _currentCount = json.Value<int>("c");
                    _endTime = json.Value<DateTime>("e");
                }
            }
        }
    }
}