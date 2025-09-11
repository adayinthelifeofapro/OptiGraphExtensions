using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services
{
    public class PinnedResultsApiService : IPinnedResultsApiService
    {
        private readonly IPinnedResultsCollectionCrudService _collectionCrudService;
        private readonly IPinnedResultsCrudService _pinnedResultsCrudService;
        private readonly IPinnedResultsGraphSyncService _graphSyncService;

        public PinnedResultsApiService(
            IPinnedResultsCollectionCrudService collectionCrudService,
            IPinnedResultsCrudService pinnedResultsCrudService,
            IPinnedResultsGraphSyncService graphSyncService)
        {
            _collectionCrudService = collectionCrudService;
            _pinnedResultsCrudService = pinnedResultsCrudService;
            _graphSyncService = graphSyncService;
        }

        public async Task<IList<PinnedResultsCollection>> GetCollectionsAsync()
        {
            return await _collectionCrudService.GetCollectionsAsync();
        }

        public async Task<PinnedResultsCollection> CreateCollectionAsync(CreatePinnedResultsCollectionRequest request)
        {
            return await _collectionCrudService.CreateCollectionAsync(request);
        }

        public async Task<bool> UpdateCollectionAsync(Guid id, UpdatePinnedResultsCollectionRequest request)
        {
            return await _collectionCrudService.UpdateCollectionAsync(id, request);
        }

        public async Task<bool> DeleteCollectionAsync(Guid id)
        {
            return await _collectionCrudService.DeleteCollectionAsync(id);
        }

        public async Task<PinnedResultsCollection?> GetCollectionByIdAsync(Guid id)
        {
            return await _collectionCrudService.GetCollectionByIdAsync(id);
        }

        public async Task<IList<PinnedResult>> GetPinnedResultsAsync(Guid collectionId)
        {
            return await _pinnedResultsCrudService.GetPinnedResultsAsync(collectionId);
        }

        public async Task<bool> CreatePinnedResultAsync(CreatePinnedResultRequest request)
        {
            return await _pinnedResultsCrudService.CreatePinnedResultAsync(request);
        }

        public async Task<bool> UpdatePinnedResultAsync(Guid id, UpdatePinnedResultRequest request)
        {
            return await _pinnedResultsCrudService.UpdatePinnedResultAsync(id, request);
        }

        public async Task<bool> DeletePinnedResultAsync(Guid id)
        {
            return await _pinnedResultsCrudService.DeletePinnedResultAsync(id);
        }

        public async Task<bool> SyncCollectionToOptimizelyGraphAsync(PinnedResultsCollection collection)
        {
            return await _graphSyncService.SyncCollectionToOptimizelyGraphAsync(collection);
        }

        public async Task<bool> SyncPinnedResultsToOptimizelyGraphAsync(Guid collectionId)
        {
            return await _graphSyncService.SyncPinnedResultsToOptimizelyGraphAsync(collectionId);
        }

        public async Task<bool> UpdateCollectionGraphIdAsync(Guid collectionId, string graphCollectionId)
        {
            return await _collectionCrudService.UpdateCollectionGraphIdAsync(collectionId, graphCollectionId);
        }

        public async Task<IList<GraphCollectionResponse>> SyncCollectionsFromOptimizelyGraphAsync()
        {
            return await _graphSyncService.SyncCollectionsFromOptimizelyGraphAsync();
        }

    }
}