﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace GeneralBot.Services
{
    public class CacheHelper
    {
        private readonly IMemoryCache _cache;

        public CacheHelper(IMemoryCache cache) => _cache = cache;

        public Task<T2> TryGetValueSet<T1, T2>(T1 key, T2 value, TimeSpan duration)
        {
            T2 cacheEntry;

            // Look for cache key.
            if (!_cache.TryGetValue(key, out cacheEntry))
            {
                cacheEntry = value;

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(duration);

                _cache.Set(key, cacheEntry, cacheEntryOptions);
            }

            return Task.FromResult(cacheEntry);
        }
    }
}