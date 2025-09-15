using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace OptiGraphExtensions.Features.Common.Caching
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);

        public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return value;
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default(T);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.Normal
                };

                options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (k, v, r, s) =>
                    {
                        _cacheKeys.Remove(key);
                        _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", key, r);
                    }
                });

                _memoryCache.Set(key, value, options);
                _cacheKeys.Add(key);

                _logger.LogDebug("Cache entry set: {Key}, Expiration: {Expiration}", key, expiration ?? _defaultExpiration);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveAsync(string key)
        {
            await _semaphore.WaitAsync();
            try
            {
                _memoryCache.Remove(key);
                _cacheKeys.Remove(key);
                _logger.LogDebug("Cache entry removed: {Key}", key);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            await _semaphore.WaitAsync();
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var keysToRemove = _cacheKeys.Where(key => regex.IsMatch(key)).ToList();

                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                    _cacheKeys.Remove(key);
                }

                _logger.LogDebug("Cache entries removed by pattern: {Pattern}, Count: {Count}", pattern, keysToRemove.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var keysToRemove = _cacheKeys.ToList();
                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }
                _cacheKeys.Clear();

                _logger.LogDebug("All cache entries cleared. Count: {Count}", keysToRemove.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public bool Exists(string key)
        {
            return _memoryCache.TryGetValue(key, out _);
        }
    }
}