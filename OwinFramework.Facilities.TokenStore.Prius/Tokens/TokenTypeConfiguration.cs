using System.Collections.Generic;
using Newtonsoft.Json;

namespace OwinFramework.Facilities.TokenStore.Prius.Tokens
{
    public class TokenTypeConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rules")]
        public List<RuleConfiguration> Rules { get; set; }
    }

    public class RuleConfiguration
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("config")]
        public string Json { get; set; }
    }
}