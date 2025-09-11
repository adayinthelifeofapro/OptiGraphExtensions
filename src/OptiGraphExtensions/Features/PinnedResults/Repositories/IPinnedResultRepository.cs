using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults.Repositories
{
    public interface IPinnedResultRepository
    {
        Task<IEnumerable<PinnedResult>> GetAllAsync(Guid? collectionId = null);
        Task<PinnedResult?> GetByIdAsync(Guid id);
        Task<PinnedResult> CreateAsync(PinnedResult pinnedResult);
        Task<PinnedResult> UpdateAsync(PinnedResult pinnedResult);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> CollectionExistsAsync(Guid collectionId);
        Task<IEnumerable<PinnedResult>> GetByCollectionIdAsync(Guid collectionId);
        Task<bool> DeleteByCollectionIdAsync(Guid collectionId);
    }
}