using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services.Abstractions
{
    public interface IPinnedResultsValidationService
    {
        ValidationResult ValidateCollection(PinnedResultsCollectionModel model);
        ValidationResult ValidatePinnedResult(PinnedResultModel model);
    }
}