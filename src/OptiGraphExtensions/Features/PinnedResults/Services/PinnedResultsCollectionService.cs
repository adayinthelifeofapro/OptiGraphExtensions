using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Repositories;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services
{
    public class PinnedResultsCollectionService : IPinnedResultsCollectionService
    {
        private readonly IPinnedResultsCollectionRepository _collectionRepository;
        private readonly IPinnedResultRepository _pinnedResultRepository;

        public PinnedResultsCollectionService(
            IPinnedResultsCollectionRepository collectionRepository, 
            IPinnedResultRepository pinnedResultRepository)
        {
            _collectionRepository = collectionRepository;
            _pinnedResultRepository = pinnedResultRepository;
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
            // First delete all related pinned results
            await _pinnedResultRepository.DeleteByCollectionIdAsync(id);
            
            // Then delete the collection
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
    }
}