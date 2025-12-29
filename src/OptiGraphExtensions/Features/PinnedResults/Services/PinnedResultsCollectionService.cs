using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Repositories;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services
{
    public class PinnedResultsCollectionService : IPinnedResultsCollectionService
    {
        private readonly IPinnedResultsCollectionRepository _collectionRepository;
        private readonly IPinnedResultRepository _pinnedResultRepository;
        private readonly IPinnedResultsGraphSyncService _graphSyncService;

        public PinnedResultsCollectionService(
            IPinnedResultsCollectionRepository collectionRepository,
            IPinnedResultRepository pinnedResultRepository,
            IPinnedResultsGraphSyncService graphSyncService)
        {
            _collectionRepository = collectionRepository;
            _pinnedResultRepository = pinnedResultRepository;
            _graphSyncService = graphSyncService;
        }

        public async Task<IEnumerable<PinnedResultsCollection>> GetAllCollectionsAsync()
        {
            return await _collectionRepository.GetAllAsync();
        }

        public async Task<PinnedResultsCollection?> GetCollectionByIdAsync(Guid id)
        {
            return await _collectionRepository.GetByIdAsync(id);
        }

        public async Task<PinnedResultsCollection> CreateCollectionAsync(string? title, bool isActive, string? createdBy = null)
        {
            var collection = new PinnedResultsCollection
            {
                Id = Guid.NewGuid(),
                Title = title,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            return await _collectionRepository.CreateAsync(collection);
        }

        public async Task<PinnedResultsCollection?> UpdateCollectionAsync(Guid id, string? title, bool isActive)
        {
            var collection = await _collectionRepository.GetByIdAsync(id);
            if (collection == null)
            {
                return null;
            }

            collection.Title = title;
            collection.IsActive = isActive;

            return await _collectionRepository.UpdateAsync(collection);
        }

        public async Task<bool> DeleteCollectionAsync(Guid id)
        {
            // Get the collection to check if it has a GraphCollectionId
            var collection = await _collectionRepository.GetByIdAsync(id);
            if (collection == null)
            {
                return false;
            }

            // If the collection has a GraphCollectionId, delete it from Graph first
            if (!string.IsNullOrEmpty(collection.GraphCollectionId))
            {
                try
                {
                    await _graphSyncService.DeleteCollectionFromOptimizelyGraphAsync(collection.GraphCollectionId);
                }
                catch (Exception)
                {
                    // Log error but continue with local deletion
                    // In production, you might want to handle this differently
                }
            }

            // Delete all related pinned results from local database
            await _pinnedResultRepository.DeleteByCollectionIdAsync(id);

            // Finally delete the collection from local database
            return await _collectionRepository.DeleteAsync(id);
        }

        public async Task<bool> CollectionExistsAsync(Guid id)
        {
            return await _collectionRepository.ExistsAsync(id);
        }

        public async Task<PinnedResultsCollection?> UpdateGraphCollectionIdAsync(Guid id, string? graphCollectionId)
        {
            return await _collectionRepository.UpdateGraphCollectionIdAsync(id, graphCollectionId);
        }

        public async Task<IEnumerable<PinnedResultsCollection>> SyncCollectionsFromGraphAsync()
        {
            try
            {
                // Fetch collections from Optimizely Graph
                var graphCollections = await _graphSyncService.SyncCollectionsFromOptimizelyGraphAsync();
                var syncedCollections = new List<PinnedResultsCollection>();

                foreach (var graphCollection in graphCollections)
                {
                    if (string.IsNullOrEmpty(graphCollection.Id) || string.IsNullOrEmpty(graphCollection.Title))
                        continue;

                    // Convert timestamps from Unix milliseconds to DateTime
                    //var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(graphCollection.CreatedAt).UtcDateTime;
                    
                    // Create or update local collection
                    var localCollection = new PinnedResultsCollection
                    {
                        Id = Guid.NewGuid(), // Will be ignored if existing collection is found
                        GraphCollectionId = graphCollection.Id,
                        Title = graphCollection.Title,
                        IsActive = graphCollection.IsActive,
                        CreatedAt = graphCollection.CreatedAt,
                        CreatedBy = "Optimizely Graph Sync"
                    };

                    var syncedCollection = await _collectionRepository.UpsertAsync(localCollection);
                    syncedCollections.Add(syncedCollection);
                }

                return syncedCollections;
            }
            catch (Exception)
            {
                // If Graph sync fails, return current local collections
                return await _collectionRepository.GetAllAsync();
            }
        }
    }
}