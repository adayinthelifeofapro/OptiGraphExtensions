
namespace OptiGraphExtensions.Features.Common.Caching
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);

        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        Task RemoveAsync(string key);

        Task RemoveByPatternAsync(string pattern);

        Task ClearAsync();

        bool Exists(string key);
    }
}