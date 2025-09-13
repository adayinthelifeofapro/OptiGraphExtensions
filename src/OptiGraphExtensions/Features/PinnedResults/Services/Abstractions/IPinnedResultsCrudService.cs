using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;

public interface IPinnedResultsCrudService
{
    Task<IList<PinnedResult>> GetPinnedResultsAsync(Guid collectionId);
    Task<bool> CreatePinnedResultAsync(CreatePinnedResultRequest request);
    Task<bool> UpdatePinnedResultAsync(Guid id, UpdatePinnedResultRequest request);
    Task<bool> DeletePinnedResultAsync(Guid id);
    Task<bool> DeletePinnedResultsByCollectionIdAsync(Guid collectionId);
}