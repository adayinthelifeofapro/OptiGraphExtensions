using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Caching;

namespace OptiGraphExtensions.Features.QueryLibrary.Repositories
{
    public class CachedSavedQueryRepository : ISavedQueryRepository
    {
        private readonly ISavedQueryRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public CachedSavedQueryRepository(ISavedQueryRepository repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<SavedQuery>> GetAllAsync()
        {
            var cacheKey = CacheKeyBuilder.BuildEntityListKey<SavedQuery>();
            var cachedResult = await _cacheService.GetAsync<IEnumerable<SavedQuery>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetAllAsync();
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<IEnumerable<SavedQuery>> GetActiveAsync()
        {
            var cacheKey = $"{CacheKeyBuilder.BuildEntityListKey<SavedQuery>()}:active";
            var cachedResult = await _cacheService.GetAsync<IEnumerable<SavedQuery>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetActiveAsync();
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<SavedQuery?> GetByIdAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildEntityKey<SavedQuery>(id);
            var cachedResult = await _cacheService.GetAsync<SavedQuery>(cacheKey);

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

        public async Task<SavedQuery> CreateAsync(SavedQuery query)
        {
            var result = await _repository.CreateAsync(query);

            // Invalidate related cache entries
            await InvalidateSavedQueryCacheAsync();

            // Cache the new entity
            var cacheKey = CacheKeyBuilder.BuildEntityKey<SavedQuery>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<SavedQuery> UpdateAsync(SavedQuery query)
        {
            var result = await _repository.UpdateAsync(query);

            // Invalidate related cache entries
            await InvalidateSavedQueryCacheAsync();

            // Update cache with new data
            var cacheKey = CacheKeyBuilder.BuildEntityKey<SavedQuery>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                // Invalidate related cache entries
                await InvalidateSavedQueryCacheAsync();

                // Remove the specific entity from cache
                var cacheKey = CacheKeyBuilder.BuildEntityKey<SavedQuery>(id);
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildExistsKey<SavedQuery>(id);

            if (_cacheService.Exists(cacheKey))
            {
                var cachedResult = await _cacheService.GetAsync<bool>(cacheKey);
                return cachedResult;
            }

            var result = await _repository.ExistsAsync(id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
        {
            // Don't cache name existence checks as they depend on excludeId
            return await _repository.NameExistsAsync(name, excludeId);
        }

        private async Task InvalidateSavedQueryCacheAsync()
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<SavedQuery>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }
}
