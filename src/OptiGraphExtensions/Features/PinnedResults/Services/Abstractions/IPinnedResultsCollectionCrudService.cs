using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

public interface IPinnedResultsCollectionCrudService
{
    Task<IList<PinnedResultsCollection>> GetCollectionsAsync();
    Task<PinnedResultsCollection> CreateCollectionAsync(CreatePinnedResultsCollectionRequest request);
    Task<bool> UpdateCollectionAsync(Guid id, UpdatePinnedResultsCollectionRequest request);
    Task<bool> DeleteCollectionAsync(Guid id);
    Task<PinnedResultsCollection?> GetCollectionByIdAsync(Guid id);
    Task<bool> UpdateCollectionGraphIdAsync(Guid collectionId, string graphCollectionId);
}