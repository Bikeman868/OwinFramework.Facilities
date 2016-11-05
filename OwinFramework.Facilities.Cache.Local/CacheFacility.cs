using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.Cache.Local
{
    /// <summary>
    /// Defines a simple implementation of the ICache facilicy that stores
    /// cached data in process memory. Use this if you have only one web server
    /// or the web servers do not need to share cached data.
    /// </summary>
    internal class CacheFacility: ICache
    {
        private readonly IDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();

        public bool Delete(string key)
        {
            return _cache.Remove(key);
        }

        public T Get<T>(string key, T defaultValue, TimeSpan? lockTime)
        {
            while (true)
            {
                CacheEntry cacheEntry;
                if (!_cache.TryGetValue(key, out cacheEntry))
                    return defaultValue;

                if (cacheEntry.Expires.HasValue && DateTime.UtcNow > cacheEntry.Expires)
                    return defaultValue;

                if (!cacheEntry.LockedUntil.HasValue || DateTime.UtcNow > cacheEntry.LockedUntil)
                {
                    cacheEntry.LockedUntil = DateTime.UtcNow + lockTime;
                    return (T)cacheEntry.Data;
                }

                Thread.Sleep(1);
            }
        }

        public bool Put<T>(string key, T value, TimeSpan? lifespan)
        {
            var exists = _cache.ContainsKey(key);
            _cache[key] = new CacheEntry 
            { 
                Data = value,
                Expires = DateTime.UtcNow + lifespan
            };
            return exists;
        }

        private class CacheEntry
        {
            public object Data;
            public DateTime? Expires;
            public DateTime? LockedUntil;
        }
    }
}
