using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Repositories;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services
{
    public class PinnedResultService : IPinnedResultService
    {
        private readonly IPinnedResultRepository _pinnedResultRepository;

        public PinnedResultService(IPinnedResultRepository pinnedResultRepository)
        {
            _pinnedResultRepository = pinnedResultRepository;
        }

        public async Task<IEnumerable<PinnedResult>> GetAllPinnedResultsAsync(Guid? collectionId = null)
        {
            return await _pinnedResultRepository.GetAllAsync(collectionId);
        }

        public async Task<PinnedResult?> GetPinnedResultByIdAsync(Guid id)
        {
            return await _pinnedResultRepository.GetByIdAsync(id);
        }

        public async Task<PinnedResult> CreatePinnedResultAsync(Guid collectionId, string? phrases, string? targetKey, string? language, int priority, bool isActive, string? createdBy = null)
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
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            return await _pinnedResultRepository.CreateAsync(pinnedResult);
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
            return await _pinnedResultRepository.DeleteAsync(id);
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