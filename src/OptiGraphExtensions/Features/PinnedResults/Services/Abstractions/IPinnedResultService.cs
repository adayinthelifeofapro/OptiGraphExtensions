using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions
{
    public interface IPinnedResultService
    {
        Task<IEnumerable<PinnedResult>> GetAllPinnedResultsAsync(Guid? collectionId = null);
        Task<PinnedResult?> GetPinnedResultByIdAsync(Guid id);
        Task<PinnedResult> CreatePinnedResultAsync(Guid collectionId, string? phrases, string? targetKey, string? language, int priority, bool isActive, string? createdBy = null);
        Task<PinnedResult?> UpdatePinnedResultAsync(Guid id, string? phrases, string? targetKey, string? language, int priority, bool isActive);
        Task<bool> DeletePinnedResultAsync(Guid id);
        Task<bool> PinnedResultExistsAsync(Guid id);
        Task<bool> CollectionExistsAsync(Guid collectionId);
        Task<IEnumerable<PinnedResult>> GetPinnedResultsByCollectionIdAsync(Guid collectionId);
    }
}