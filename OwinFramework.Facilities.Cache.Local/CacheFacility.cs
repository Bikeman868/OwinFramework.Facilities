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
        private readonly IDictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();

        public void Clear()
        {
            lock (_cache)
                _cache.Clear();
        }

        bool ICache.Delete(string key)
        {
            lock (_cache)
                return _cache.Remove(key);
        }

        T ICache.Get<T>(string key, T defaultValue, TimeSpan? lockTime)
        {
            while (true)
            {
                lock (_cache)
                {
                    CacheEntry cacheEntry;
                    if (!_cache.TryGetValue(key, out cacheEntry)
                        || (cacheEntry.Expires.HasValue && DateTime.UtcNow > cacheEntry.Expires))
                    {
                        if (lockTime.HasValue)
                        {
                            _cache[key] = new CacheEntry
                            {
                                Data = defaultValue,
                                Expires = DateTime.UtcNow + lockTime,
                                LockedUntil = DateTime.UtcNow + lockTime
                            };
                        }
                        return defaultValue;
                    }

                    if (!cacheEntry.LockedUntil.HasValue || DateTime.UtcNow > cacheEntry.LockedUntil)
                    {
                        cacheEntry.LockedUntil = DateTime.UtcNow + lockTime;
                        return (T)cacheEntry.Data;
                    }
                }
                Thread.Sleep(5);
            }
        }

        bool ICache.Put<T>(string key, T value, TimeSpan? lifespan)
        {
            lock (_cache)
            {
                var exists = _cache.ContainsKey(key);
                _cache[key] = new CacheEntry
                {
                    Data = value,
                    Expires = DateTime.UtcNow + lifespan
                };
                return exists;
            }
        }

        private class CacheEntry
        {
            public object Data;
            public DateTime? Expires;
            public DateTime? LockedUntil;
        }
    }
}
