using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class SynonymValidationService : ISynonymValidationService
    {
        public ValidationResult ValidateSynonym(SynonymModel model)
        {
            if (model == null)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Synonym model is required" 
                };
            }

            if (string.IsNullOrWhiteSpace(model.Synonym))
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Synonym text is required" 
                };
            }

            if (model.Synonym.Length > 255)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Synonym must be less than 255 characters" 
                };
            }

            return new ValidationResult { IsValid = true };
        }
    }
}