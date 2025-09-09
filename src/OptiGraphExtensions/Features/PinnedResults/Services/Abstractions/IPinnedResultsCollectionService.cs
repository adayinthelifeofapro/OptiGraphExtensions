using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions
{
    public interface IPinnedResultsCollectionService
    {
        Task<IEnumerable<PinnedResultsCollection>> GetAllCollectionsAsync();
        Task<PinnedResultsCollection?> GetCollectionByIdAsync(Guid id);
        Task<PinnedResultsCollection> CreateCollectionAsync(string? title, bool isActive, string? createdBy = null);
        Task<PinnedResultsCollection?> UpdateCollectionAsync(Guid id, string? title, bool isActive);
        Task<bool> DeleteCollectionAsync(Guid id);
        Task<bool> CollectionExistsAsync(Guid id);
        Task<PinnedResultsCollection?> UpdateGraphCollectionIdAsync(Guid id, string? graphCollectionId);
    }
}