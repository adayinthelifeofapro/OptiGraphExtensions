using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.PinnedResults.Models;

namespace OptiGraphExtensions.Features.PinnedResults.Services;

public class PinnedResultsCollectionRequestMapper : IRequestMapper<PinnedResultsCollectionModel, CreatePinnedResultsCollectionRequest, UpdatePinnedResultsCollectionRequest>
{
    public CreatePinnedResultsCollectionRequest MapToCreateRequest(PinnedResultsCollectionModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new CreatePinnedResultsCollectionRequest
        {
            Title = model.Title?.Trim() ?? string.Empty,
            IsActive = model.IsActive
        };
    }

    public UpdatePinnedResultsCollectionRequest MapToUpdateRequest(PinnedResultsCollectionModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new UpdatePinnedResultsCollectionRequest
        {
            Title = model.Title?.Trim() ?? string.Empty,
            IsActive = model.IsActive
        };
    }
}