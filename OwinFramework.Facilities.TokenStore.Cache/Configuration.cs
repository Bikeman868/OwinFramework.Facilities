using System;

namespace OwinFramework.Facilities.TokenStore.Cache
{
    [Serializable]
    public class Configuration
    {
        public TimeSpan Lifetime { get; set; }
        public string CachePrefix { get; set; }

        public Configuration()
        {
            Lifetime = TimeSpan.FromHours(1);
            CachePrefix = "/tokens/";
        }
    }
}
