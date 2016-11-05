using System;
using System.Collections.Generic;
using System.Linq;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.TokenStore.Cache
{
    /// <summary>
    /// Defines a simple implementation of the ITokenStore facilicy that stores
    /// tokens using the ICache facility.
    /// </summary>
    internal class TokenStoreFacility: ITokenStore
    {
        private readonly ICache _cache;

        private readonly IDisposable _configurationRegistration;
        private Configuration _configuration;

        public TokenStoreFacility(
            ICache cache,
            IConfiguration configuration)
        {
            _cache = cache;

            _configurationRegistration = configuration.Register("/OwinFramework/Facility/TokenStore.Cache", c => _configuration = c, new Configuration());
        }

        public string CreateToken(string tokenType, string purpose, string identity)
        {
            var value = Guid.NewGuid().ToShortString();

            var token = new CachedToken
            {
                TokenType = tokenType,
                Identity = identity,
                Purposes = string.IsNullOrEmpty(purpose) ? null : new List<string> { purpose }
            };
            var cacheKey = _configuration.CachePrefix + value;
            _cache.Put(cacheKey, token, _configuration.Lifetime);

            return value;
        }

        public string CreateToken(string tokenType, IEnumerable<string> purpose, string identity)
        {
            var value = Guid.NewGuid().ToShortString();

            var token = new CachedToken
            {
                TokenType = tokenType,
                Identity = identity,
                Purposes = purpose == null ? null : purpose.ToList()
            };
            var cacheKey = _configuration.CachePrefix + value;
            _cache.Put(cacheKey, token, _configuration.Lifetime);

            return value;
        }

        public bool DeleteToken(string token)
        {
            var cacheKey = _configuration.CachePrefix + token;
            return _cache.Delete(cacheKey);
        }

        public IToken GetToken(string tokenType, string token, string purpose, string identity)
        {
            var result = new Token
            {
                Value = token,
                Identity = identity,
                Purpose = purpose,
                Status = TokenStatus.Invalid
            };

            var cacheKey = _configuration.CachePrefix + token;
            var cachedToken = _cache.Get<CachedToken>(cacheKey);

            if (cachedToken == null) 
                return result;

            result.Status = TokenStatus.NotAllowed;

            if (!string.Equals(cachedToken.TokenType, tokenType, StringComparison.OrdinalIgnoreCase))
                return result;

            if (!string.IsNullOrEmpty(cachedToken.Identity)
                    &&
                !string.Equals(cachedToken.Identity, identity, StringComparison.OrdinalIgnoreCase))
                return result;

            if (cachedToken.Purposes != null && cachedToken.Purposes.Count > 0)
            {
                if (!cachedToken.Purposes.Any(p => string.Equals(p, purpose, StringComparison.OrdinalIgnoreCase)))
                    return result;
            }

            result.Status = TokenStatus.Allowed;
            return result;
        }

        [Serializable]
        private class Configuration
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
}
