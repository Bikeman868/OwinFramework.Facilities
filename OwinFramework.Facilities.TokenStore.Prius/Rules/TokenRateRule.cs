using System;
using Newtonsoft.Json;
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

            public string Serialize()
            {
                lock (_locker)
                {
                    return JsonConvert.SerializeObject(new SerializableTokenRateRule
                    {
                        CurrentCount = _currentCount,
                        EndTime = _endTime,
                        MaximumUseCount = MaximumUseCount,
                        TimeWindow = TimeWindow
                    });
                }
            }

            public void Hydrate(string serializedData)
            {
                lock (_locker)
                {

                    var tokenRuleSettings = JsonConvert.DeserializeObject<SerializableTokenRateRule>(serializedData);

                    _endTime = tokenRuleSettings.EndTime;
                    _currentCount = tokenRuleSettings.CurrentCount;
                    MaximumUseCount = tokenRuleSettings.MaximumUseCount;
                    TimeWindow = tokenRuleSettings.TimeWindow;
                }
            }
        }

        private class SerializableTokenRateRule
        {
            public TimeSpan TimeWindow { get; set; }
            public int MaximumUseCount { get; set; }
            public int CurrentCount { get; set; }
            public DateTime EndTime { get; set; }
        }
    }
}