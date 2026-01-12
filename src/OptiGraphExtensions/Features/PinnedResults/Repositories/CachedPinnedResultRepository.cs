using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Caching;

namespace OptiGraphExtensions.Features.PinnedResults.Repositories
{
    public class CachedPinnedResultRepository : IPinnedResultRepository
    {
        private readonly IPinnedResultRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public CachedPinnedResultRepository(IPinnedResultRepository repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<PinnedResult>> GetAllAsync(Guid? collectionId = null)
        {
            var cacheKey = collectionId.HasValue
                ? CacheKeyBuilder.BuildEntityListKey<PinnedResult>("GetAll", collectionId.Value)
                : CacheKeyBuilder.BuildEntityListKey<PinnedResult>("GetAll");

            var cachedResult = await _cacheService.GetAsync<IEnumerable<PinnedResult>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetAllAsync(collectionId);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<PinnedResult?> GetByIdAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResult>(id);
            var cachedResult = await _cacheService.GetAsync<PinnedResult>(cacheKey);

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

        public async Task<PinnedResult?> GetByIdWithCollectionAsync(Guid id)
        {
            // Don't cache results with navigation properties to avoid serialization issues
            return await _repository.GetByIdWithCollectionAsync(id);
        }

        public async Task<PinnedResult> CreateAsync(PinnedResult pinnedResult)
        {
            var result = await _repository.CreateAsync(pinnedResult);

            // Invalidate related cache entries
            await InvalidatePinnedResultCacheAsync();

            // Cache the new entity
            var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResult>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<PinnedResult> UpdateAsync(PinnedResult pinnedResult)
        {
            var result = await _repository.UpdateAsync(pinnedResult);

            // Invalidate related cache entries
            await InvalidatePinnedResultCacheAsync();

            // Update cache with new data
            var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResult>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                // Invalidate related cache entries
                await InvalidatePinnedResultCacheAsync();

                // Remove the specific entity from cache
                var cacheKey = CacheKeyBuilder.BuildEntityKey<PinnedResult>(id);
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildExistsKey<PinnedResult>(id);

            if (_cacheService.Exists(cacheKey))
            {
                var cachedResult = await _cacheService.GetAsync<bool>(cacheKey);
                return cachedResult;
            }

            var result = await _repository.ExistsAsync(id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<bool> CollectionExistsAsync(Guid collectionId)
        {
            var cacheKey = CacheKeyBuilder.BuildExistsKey<PinnedResultsCollection>(collectionId);

            if (_cacheService.Exists(cacheKey))
            {
                var cachedResult = await _cacheService.GetAsync<bool>(cacheKey);
                return cachedResult;
            }

            var result = await _repository.CollectionExistsAsync(collectionId);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<IEnumerable<PinnedResult>> GetByCollectionIdAsync(Guid collectionId)
        {
            var cacheKey = CacheKeyBuilder.BuildEntityListKey<PinnedResult>("ByCollection", collectionId);
            var cachedResult = await _cacheService.GetAsync<IEnumerable<PinnedResult>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetByCollectionIdAsync(collectionId);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<bool> DeleteByCollectionIdAsync(Guid collectionId)
        {
            var result = await _repository.DeleteByCollectionIdAsync(collectionId);

            if (result)
            {
                // Invalidate related cache entries
                await InvalidatePinnedResultCacheAsync();
                await InvalidatePinnedResultsCollectionCacheAsync();
            }

            return result;
        }

        private async Task InvalidatePinnedResultCacheAsync()
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<PinnedResult>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }

        private async Task InvalidatePinnedResultsCollectionCacheAsync()
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<PinnedResultsCollection>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }
}