using Microsoft.Extensions.Logging;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Common.Caching
{
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheInvalidationService> _logger;

        public CacheInvalidationService(ICacheService cacheService, ILogger<CacheInvalidationService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task InvalidateEntityCacheAsync<T>() where T : class
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<T>();
            await _cacheService.RemoveByPatternAsync(pattern);
            _logger.LogDebug("Invalidated all cache entries for entity type: {EntityType}", typeof(T).Name);
        }

        public async Task InvalidateEntityCacheAsync<T>(Guid id) where T : class
        {
            var cacheKey = CacheKeyBuilder.BuildEntityKey<T>(id);
            var existsKey = CacheKeyBuilder.BuildExistsKey<T>(id);

            await Task.WhenAll(
                _cacheService.RemoveAsync(cacheKey),
                _cacheService.RemoveAsync(existsKey)
            );

            _logger.LogDebug("Invalidated cache entries for entity: {EntityType} with ID: {Id}", typeof(T).Name, id);
        }

        public async Task InvalidateAllCacheAsync()
        {
            await _cacheService.ClearAsync();
            _logger.LogInformation("All cache entries have been cleared");
        }

        public async Task InvalidateSynonymsCacheAsync()
        {
            await InvalidateEntityCacheAsync<Synonym>();
        }

        public async Task InvalidatePinnedResultsCacheAsync()
        {
            await InvalidateEntityCacheAsync<PinnedResult>();
        }

        public async Task InvalidatePinnedResultsCollectionsCacheAsync()
        {
            await InvalidateEntityCacheAsync<PinnedResultsCollection>();
        }
    }
}