using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

public interface IPinnedResultsGraphSyncService
{
    Task<bool> SyncCollectionToOptimizelyGraphAsync(PinnedResultsCollection collection);
    Task<bool> SyncPinnedResultsToOptimizelyGraphAsync(Guid collectionId);
    Task<IList<GraphCollectionResponse>> SyncCollectionsFromOptimizelyGraphAsync();
}