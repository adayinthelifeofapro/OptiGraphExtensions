using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services
{
    public class PinnedResultsValidationService : IPinnedResultsValidationService
    {
        public ValidationResult ValidateCollection(PinnedResultsCollectionModel model)
        {
            if (model == null)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Collection model is required" 
                };
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Collection title is required" 
                };
            }

            if (model.Title.Length > 255)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Collection title must be less than 255 characters" 
                };
            }

            return new ValidationResult { IsValid = true };
        }

        public ValidationResult ValidatePinnedResult(PinnedResultModel model)
        {
            if (model == null)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Pinned result model is required" 
                };
            }

            if (model.CollectionId == Guid.Empty)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Collection ID is required" 
                };
            }

            if (string.IsNullOrWhiteSpace(model.Phrases))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Phrases are required" 
                };
            }

            if (string.IsNullOrWhiteSpace(model.TargetKey))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Target Key is required" 
                };
            }

            if (string.IsNullOrWhiteSpace(model.Language))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Language is required" 
                };
            }

            if (model.Phrases.Length > 1000)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Phrases must be less than 1000 characters" 
                };
            }

            if (model.TargetKey.Length > 100)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Target Key must be less than 100 characters" 
                };
            }

            if (model.Language.Length > 10)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Language must be less than 10 characters" 
                };
            }

            return new ValidationResult { IsValid = true };
        }
    }
}