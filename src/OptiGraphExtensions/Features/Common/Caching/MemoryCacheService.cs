using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace OptiGraphExtensions.Features.Common.Caching
{
    public class MemoryCacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(15); // Reduced from 30 for fresher data
        private bool _disposed;

        public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            _lock.EnterReadLock();
            try
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return Task.FromResult<T?>(value);
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);
                return Task.FromResult<T?>(default);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            _lock.EnterWriteLock();
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
                        // Use write lock for eviction callback to safely remove from HashSet
                        if (_lock.TryEnterWriteLock(TimeSpan.FromMilliseconds(100)))
                        {
                            try
                            {
                                _cacheKeys.Remove(key);
                                _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", key, r);
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }
                        }
                    }
                });

                _memoryCache.Set(key, value, options);
                _cacheKeys.Add(key);

                _logger.LogDebug("Cache entry set: {Key}, Expiration: {Expiration}", key, expiration ?? _defaultExpiration);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _lock.EnterWriteLock();
            try
            {
                _memoryCache.Remove(key);
                _cacheKeys.Remove(key);
                _logger.LogDebug("Cache entry removed: {Key}", key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            _lock.EnterWriteLock();
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
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _lock.EnterWriteLock();
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
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        public bool Exists(string key)
        {
            _lock.EnterReadLock();
            try
            {
                return _memoryCache.TryGetValue(key, out _);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _lock.Dispose();
            }

            _disposed = true;
        }
    }
}
