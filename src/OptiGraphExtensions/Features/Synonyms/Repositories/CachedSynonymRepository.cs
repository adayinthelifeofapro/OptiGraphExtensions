using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Caching;

namespace OptiGraphExtensions.Features.Synonyms.Repositories
{
    public class CachedSynonymRepository : ISynonymRepository
    {
        private readonly ISynonymRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public CachedSynonymRepository(ISynonymRepository repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<Synonym>> GetAllAsync()
        {
            var cacheKey = CacheKeyBuilder.BuildEntityListKey<Synonym>();
            var cachedResult = await _cacheService.GetAsync<IEnumerable<Synonym>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetAllAsync();
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<Synonym?> GetByIdAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(id);
            var cachedResult = await _cacheService.GetAsync<Synonym>(cacheKey);

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

        public async Task<Synonym> CreateAsync(Synonym synonym)
        {
            var result = await _repository.CreateAsync(synonym);

            // Invalidate related cache entries
            await InvalidateSynonymCacheAsync();

            // Cache the new entity
            var cacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<Synonym> UpdateAsync(Synonym synonym)
        {
            var result = await _repository.UpdateAsync(synonym);

            // Invalidate related cache entries
            await InvalidateSynonymCacheAsync();

            // Update cache with new data
            var cacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                // Invalidate related cache entries
                await InvalidateSynonymCacheAsync();

                // Remove the specific entity from cache
                var cacheKey = CacheKeyBuilder.BuildEntityKey<Synonym>(id);
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildExistsKey<Synonym>(id);
            var cachedResult = await _cacheService.GetAsync<bool>(cacheKey);

            // Check if we have a cached value (default for bool is false, so we need to be more careful)
            if (_cacheService.Exists(cacheKey))
            {
                return cachedResult;
            }

            var result = await _repository.ExistsAsync(id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        private async Task InvalidateSynonymCacheAsync()
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<Synonym>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }
}