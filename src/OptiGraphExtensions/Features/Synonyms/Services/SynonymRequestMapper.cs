using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Synonyms.Models;

namespace OptiGraphExtensions.Features.Synonyms.Services;

public class SynonymRequestMapper : IRequestMapper<SynonymModel, CreateSynonymRequest, UpdateSynonymRequest>
{
    public CreateSynonymRequest MapToCreateRequest(SynonymModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new CreateSynonymRequest
        {
            Synonym = model.Synonym?.Trim() ?? string.Empty,
            Language = model.Language?.Trim() ?? string.Empty
        };
    }

    public UpdateSynonymRequest MapToUpdateRequest(SynonymModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new UpdateSynonymRequest
        {
            Synonym = model.Synonym?.Trim() ?? string.Empty,
            Language = model.Language?.Trim() ?? string.Empty
        };
    }
}