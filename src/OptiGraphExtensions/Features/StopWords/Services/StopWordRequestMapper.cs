using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.StopWords.Models;

namespace OptiGraphExtensions.Features.StopWords.Services
{
    public class StopWordRequestMapper : IRequestMapper<StopWordModel, CreateStopWordRequest, UpdateStopWordRequest>
    {
        public CreateStopWordRequest MapToCreateRequest(StopWordModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new CreateStopWordRequest
            {
                Word = model.Word?.Trim().ToLowerInvariant() ?? string.Empty,
                Language = model.Language?.Trim() ?? string.Empty
            };
        }

        public UpdateStopWordRequest MapToUpdateRequest(StopWordModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return new UpdateStopWordRequest
            {
                Word = model.Word?.Trim().ToLowerInvariant() ?? string.Empty,
                Language = model.Language?.Trim() ?? string.Empty
            };
        }
    }
}
