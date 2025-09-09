using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults.Repositories
{
    public interface IPinnedResultsCollectionRepository
    {
        Task<IEnumerable<PinnedResultsCollection>> GetAllAsync();
        Task<PinnedResultsCollection?> GetByIdAsync(Guid id);
        Task<PinnedResultsCollection> CreateAsync(PinnedResultsCollection collection);
        Task<PinnedResultsCollection> UpdateAsync(PinnedResultsCollection collection);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<PinnedResultsCollection?> UpdateGraphCollectionIdAsync(Guid id, string? graphCollectionId);
    }
}