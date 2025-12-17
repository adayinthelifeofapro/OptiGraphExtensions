using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

public interface IPinnedResultsGraphSyncService
{
    Task<bool> SyncCollectionToOptimizelyGraphAsync(PinnedResultsCollection collection);
    Task<bool> SyncPinnedResultsToOptimizelyGraphAsync(Guid collectionId);
    Task<string> SyncSinglePinnedResultToOptimizelyGraphAsync(PinnedResult pinnedResult, string graphCollectionId);
    Task<bool> SyncPinnedResultsFromOptimizelyGraphAsync(Guid collectionId);
    Task<bool> DeletePinnedResultFromOptimizelyGraphAsync(string graphCollectionId, string graphId);
    Task<bool> DeleteAllItemsFromCollectionAsync(string graphCollectionId);
    Task<bool> DeleteCollectionFromOptimizelyGraphAsync(string graphCollectionId);
    Task<IList<GraphCollectionResponse>> SyncCollectionsFromOptimizelyGraphAsync();
}