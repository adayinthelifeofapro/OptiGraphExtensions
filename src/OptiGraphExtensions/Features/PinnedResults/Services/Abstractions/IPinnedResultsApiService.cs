using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions
{
    public interface IPinnedResultsApiService
    {
        Task<IList<PinnedResultsCollection>> GetCollectionsAsync();
        Task<PinnedResultsCollection> CreateCollectionAsync(CreatePinnedResultsCollectionRequest request);
        Task<bool> UpdateCollectionAsync(Guid id, UpdatePinnedResultsCollectionRequest request);
        Task<bool> DeleteCollectionAsync(Guid id);
        Task<PinnedResultsCollection?> GetCollectionByIdAsync(Guid id);
        
        Task<IList<PinnedResult>> GetPinnedResultsAsync(Guid collectionId);
        Task<bool> CreatePinnedResultAsync(CreatePinnedResultRequest request);
        Task<bool> UpdatePinnedResultAsync(Guid id, UpdatePinnedResultRequest request);
        Task<bool> DeletePinnedResultAsync(Guid id);
        
        Task<bool> SyncCollectionToOptimizelyGraphAsync(PinnedResultsCollection collection);
        Task<bool> SyncPinnedResultsToOptimizelyGraphAsync(Guid collectionId);
        Task<bool> SyncPinnedResultsFromOptimizelyGraphAsync(Guid collectionId);
        Task<bool> UpdateCollectionGraphIdAsync(Guid collectionId, string graphCollectionId);
        Task<IList<GraphCollectionResponse>> SyncCollectionsFromOptimizelyGraphAsync();
    }
}