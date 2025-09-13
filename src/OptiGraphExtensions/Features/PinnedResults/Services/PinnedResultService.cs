using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Repositories;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services
{
    public class PinnedResultService : IPinnedResultService
    {
        private readonly IPinnedResultRepository _pinnedResultRepository;
        private readonly IPinnedResultsCollectionRepository _collectionRepository;
        private readonly IPinnedResultsGraphSyncService _graphSyncService;

        public PinnedResultService(
            IPinnedResultRepository pinnedResultRepository,
            IPinnedResultsCollectionRepository collectionRepository,
            IPinnedResultsGraphSyncService graphSyncService)
        {
            _pinnedResultRepository = pinnedResultRepository;
            _collectionRepository = collectionRepository;
            _graphSyncService = graphSyncService;
        }

        public async Task<IEnumerable<PinnedResult>> GetAllPinnedResultsAsync(Guid? collectionId = null)
        {
            return await _pinnedResultRepository.GetAllAsync(collectionId);
        }

        public async Task<PinnedResult?> GetPinnedResultByIdAsync(Guid id)
        {
            return await _pinnedResultRepository.GetByIdAsync(id);
        }

        public async Task<PinnedResult> CreatePinnedResultAsync(Guid collectionId, string? phrases, string? targetKey, string? language, int priority, bool isActive, string? createdBy = null, string? graphId = null)
        {
            var pinnedResult = new PinnedResult
            {
                Id = Guid.NewGuid(),
                CollectionId = collectionId,
                Phrases = phrases,
                TargetKey = targetKey,
                Language = language,
                Priority = priority,
                IsActive = isActive,
                GraphId = graphId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            // Create the local record first
            var createdPinnedResult = await _pinnedResultRepository.CreateAsync(pinnedResult);

            // If no GraphId was provided and the item is active, sync to Graph
            if (string.IsNullOrEmpty(graphId) && isActive)
            {
                var collection = await _collectionRepository.GetByIdAsync(collectionId);
                if (collection != null)
                {
                    // If collection doesn't have a GraphCollectionId, sync it first
                    if (string.IsNullOrEmpty(collection.GraphCollectionId))
                    {
                        try
                        {
                            await _graphSyncService.SyncCollectionToOptimizelyGraphAsync(collection);
                            // Refresh collection to get the updated GraphCollectionId
                            collection = await _collectionRepository.GetByIdAsync(collectionId);
                        }
                        catch
                        {
                            // If collection sync fails, return the local result without Graph sync
                            return createdPinnedResult;
                        }
                    }

                    // Sync to Graph and update local record with Graph ID
                    if (collection != null && !string.IsNullOrEmpty(collection.GraphCollectionId))
                    {
                        try
                        {
                            var returnedGraphId = await _graphSyncService.SyncSinglePinnedResultToOptimizelyGraphAsync(createdPinnedResult, collection.GraphCollectionId);
                            
                            if (!string.IsNullOrEmpty(returnedGraphId))
                            {
                                // Update the local record with the Graph ID
                                createdPinnedResult.GraphId = returnedGraphId;
                                await _pinnedResultRepository.UpdateAsync(createdPinnedResult);
                            }
                        }
                        catch
                        {
                            // If Graph sync fails, return the local result without Graph ID
                        }
                    }
                }
            }

            return createdPinnedResult;
        }

        public async Task<PinnedResult?> UpdatePinnedResultAsync(Guid id, string? phrases, string? targetKey, string? language, int priority, bool isActive)
        {
            var pinnedResult = await _pinnedResultRepository.GetByIdAsync(id);
            if (pinnedResult == null)
            {
                return null;
            }

            pinnedResult.Phrases = phrases;
            pinnedResult.TargetKey = targetKey;
            pinnedResult.Language = language;
            pinnedResult.Priority = priority;
            pinnedResult.IsActive = isActive;

            return await _pinnedResultRepository.UpdateAsync(pinnedResult);
        }

        public async Task<bool> DeletePinnedResultAsync(Guid id)
        {
            // Get the pinned result to check if it has a Graph ID
            var pinnedResult = await _pinnedResultRepository.GetByIdAsync(id);
            if (pinnedResult == null)
                return false;

            // If the pinned result has a Graph ID, delete it from Graph first
            if (!string.IsNullOrEmpty(pinnedResult.GraphId))
            {
                // Get the collection to get the Graph Collection ID
                var collection = await _collectionRepository.GetByIdAsync(pinnedResult.CollectionId);
                if (collection != null && !string.IsNullOrEmpty(collection.GraphCollectionId))
                {
                    try
                    {
                        await _graphSyncService.DeletePinnedResultFromOptimizelyGraphAsync(collection.GraphCollectionId, pinnedResult.GraphId);
                    }
                    catch
                    {
                        // Continue with local deletion even if Graph deletion fails
                        // This prevents orphaned records in the local database
                    }
                }
            }

            // Delete locally
            return await _pinnedResultRepository.DeleteAsync(id);
        }

        public async Task<bool> DeletePinnedResultsByCollectionIdAsync(Guid collectionId)
        {
            // Get all pinned results for this collection
            var pinnedResults = await _pinnedResultRepository.GetByCollectionIdAsync(collectionId);
            
            // Get the collection to get the Graph Collection ID
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            
            // Delete each item from Graph if it has a Graph ID
            if (collection != null && !string.IsNullOrEmpty(collection.GraphCollectionId))
            {
                foreach (var pinnedResult in pinnedResults.Where(pr => !string.IsNullOrEmpty(pr.GraphId)))
                {
                    try
                    {
                        await _graphSyncService.DeletePinnedResultFromOptimizelyGraphAsync(collection.GraphCollectionId, pinnedResult.GraphId!);
                    }
                    catch
                    {
                        // Continue with other deletions even if one fails
                    }
                }
            }

            // Delete locally
            return await _pinnedResultRepository.DeleteByCollectionIdAsync(collectionId);
        }

        public async Task<bool> PinnedResultExistsAsync(Guid id)
        {
            return await _pinnedResultRepository.ExistsAsync(id);
        }

        public async Task<bool> CollectionExistsAsync(Guid collectionId)
        {
            return await _pinnedResultRepository.CollectionExistsAsync(collectionId);
        }

        public async Task<IEnumerable<PinnedResult>> GetPinnedResultsByCollectionIdAsync(Guid collectionId)
        {
            return await _pinnedResultRepository.GetByCollectionIdAsync(collectionId);
        }
    }
}