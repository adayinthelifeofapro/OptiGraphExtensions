using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.PinnedResults.Models;

namespace OptiGraphExtensions.Features.PinnedResults.Services;

public class PinnedResultRequestMapper : IRequestMapper<PinnedResultModel, CreatePinnedResultRequest, UpdatePinnedResultRequest>
{
    public CreatePinnedResultRequest MapToCreateRequest(PinnedResultModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new CreatePinnedResultRequest
        {
            CollectionId = model.CollectionId,
            Phrases = model.Phrases?.Trim() ?? string.Empty,
            TargetKey = model.TargetKey?.Trim() ?? string.Empty,
            TargetName = model.TargetName?.Trim(),
            Language = model.Language?.Trim() ?? string.Empty,
            Priority = model.Priority,
            IsActive = model.IsActive
        };
    }

    public UpdatePinnedResultRequest MapToUpdateRequest(PinnedResultModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new UpdatePinnedResultRequest
        {
            Phrases = model.Phrases?.Trim() ?? string.Empty,
            TargetKey = model.TargetKey?.Trim() ?? string.Empty,
            TargetName = model.TargetName?.Trim(),
            Language = model.Language?.Trim() ?? string.Empty,
            Priority = model.Priority,
            IsActive = model.IsActive
        };
    }
}