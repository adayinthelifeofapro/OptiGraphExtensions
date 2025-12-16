using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Caching;

namespace OptiGraphExtensions.Features.StopWords.Repositories
{
    public class CachedStopWordRepository : IStopWordRepository
    {
        private readonly IStopWordRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public CachedStopWordRepository(IStopWordRepository repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<StopWord>> GetAllAsync()
        {
            var cacheKey = CacheKeyBuilder.BuildEntityListKey<StopWord>();
            var cachedResult = await _cacheService.GetAsync<IEnumerable<StopWord>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetAllAsync();
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<IEnumerable<StopWord>> GetByLanguageAsync(string language)
        {
            var cacheKey = $"{CacheKeyBuilder.BuildEntityListKey<StopWord>()}:language:{language}";
            var cachedResult = await _cacheService.GetAsync<IEnumerable<StopWord>>(cacheKey);

            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _repository.GetByLanguageAsync(language);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);
            return result;
        }

        public async Task<StopWord?> GetByIdAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildEntityKey<StopWord>(id);
            var cachedResult = await _cacheService.GetAsync<StopWord>(cacheKey);

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

        public async Task<StopWord> CreateAsync(StopWord stopWord)
        {
            var result = await _repository.CreateAsync(stopWord);

            // Invalidate related cache entries
            await InvalidateStopWordCacheAsync();

            // Cache the new entity
            var cacheKey = CacheKeyBuilder.BuildEntityKey<StopWord>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<StopWord> UpdateAsync(StopWord stopWord)
        {
            var result = await _repository.UpdateAsync(stopWord);

            // Invalidate related cache entries
            await InvalidateStopWordCacheAsync();

            // Update cache with new data
            var cacheKey = CacheKeyBuilder.BuildEntityKey<StopWord>(result.Id);
            await _cacheService.SetAsync(cacheKey, result, _cacheExpiration);

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                // Invalidate related cache entries
                await InvalidateStopWordCacheAsync();

                // Remove the specific entity from cache
                var cacheKey = CacheKeyBuilder.BuildEntityKey<StopWord>(id);
                await _cacheService.RemoveAsync(cacheKey);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var cacheKey = CacheKeyBuilder.BuildExistsKey<StopWord>(id);
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

        private async Task InvalidateStopWordCacheAsync()
        {
            var pattern = CacheKeyBuilder.BuildEntityPattern<StopWord>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }
    }
}
