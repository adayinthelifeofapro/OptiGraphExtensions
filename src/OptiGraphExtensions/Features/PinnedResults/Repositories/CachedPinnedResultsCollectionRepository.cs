using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Caching;

namespace OptiGraphExtensions.Features.PinnedResults.Repositories
{
    public class CachedPinnedResultsCollectionRepository : IPinnedResultsCollectionRepository
    {
        private readonly IPinnedResultsCollectionRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public CachedPinnedResultsCollectionRepository(IPinnedResultsCollectionRepository repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<PinnedResultsCollection>> GetAllAsync()
        {
            var cacheKey = CacheKeyBuilder.BuildEntityListKey<PinnedResultsCollection>();
            var cachedResult = await _cacheService.GetAsync<IEnumerable<PinnedResultsCollection>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetAllAsync();
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<PinnedResultsCollection?> GetByIdAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResultsCollection>(id);
            var cachedResult = await _cacheService.GetAsync<PinnedResultsCollection>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetByIdAsync(id);
            if (result != null)
            {
                await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            }
            return result;
        }

        public async Task<PinnedResultsCollection> CreateAsync(PinnedResultsCollection collection)
        {
            var result = await _repository.CreateAsync(collection);

            // Invalidate related cache entries
            await InvalidateCollectionCacheAsync();

            // Cache the new entity
            var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResultsCollection>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<PinnedResultsCollection> UpdateAsync(PinnedResultsCollection collection)
        {
            var result = await _repository.UpdateAsync(collection);

            // Invalidate related cache entries
            await InvalidateCollectionCacheAsync();

            // Update cache with new data
            var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResultsCollection>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                // Invalidate related cache entries
                await InvalidateCollectionCacheAsync();
                await InvalidatePinnedResultCacheAsync();

                // Remove the specific entity from cache
                var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResultsCollection>(id);
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildExistsKey<PinnedResultsCollection>(id);

            if (_cacheService.Exists(cacheKey))
            {
                var cachedResult = await _cacheService.GetAsync<bool>(cacheKey);
                return cachedResult;
            }

            var result = await _repository.ExistsAsync(id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<PinnedResultsCollection?> UpdateGraphCollectionIdAsync(Guid id, string? graphCollectionId)
        {
            var result = await _repository.UpdateGraphCollectionIdAsync(id, graphCollectionId);

            if (result != null)
            {
                // Invalidate related cache entries
                await InvalidateCollectionCacheAsync();

                // Update cache with new data
                var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResultsCollection>(result.Id);
                await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            }

            return result;
        }

        public async Task<PinnedResultsCollection?> GetByGraphCollectionIdAsync(string graphCollectionId)
        {
            var cacheKey = CacheKeyBuilder.BuildEntityListKey<PinnedResultsCollection>("ByGraphId", graphCollectionId);
            var cachedResult = await _cacheService.GetAsync<PinnedResultsCollection>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetByGraphCollectionIdAsync(graphCollectionId);
            if (result != null)
            {
                await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            }
            return result;
        }

        public async Task<PinnedResultsCollection> UpsertAsync(PinnedResultsCollection collection)
        {
            var result = await _repository.UpsertAsync(collection);

            // Invalidate related cache entries
            await InvalidateCollectionCacheAsync();

            // Cache the entity
            var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResultsCollection>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        private async Task InvalidateCollectionCacheAsync()
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<PinnedResultsCollection>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }

        private async Task InvalidatePinnedResultCacheAsync()
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<PinnedResult>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }
}