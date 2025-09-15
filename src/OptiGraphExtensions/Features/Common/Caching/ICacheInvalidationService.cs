namespace OptiGraphExtensions.Features.Common.Caching
{
    public interface ICacheInvalidationService
    {
        Task InvalidateEntityCacheAsync<T>() where T : class;

        Task InvalidateEntityCacheAsync<T>(Guid id) where T : class;

        Task InvalidateAllCacheAsync();

        Task InvalidateSynonymsCacheAsync();

        Task InvalidatePinnedResultsCacheAsync();

        Task InvalidatePinnedResultsCollectionsCacheAsync();
    }
}