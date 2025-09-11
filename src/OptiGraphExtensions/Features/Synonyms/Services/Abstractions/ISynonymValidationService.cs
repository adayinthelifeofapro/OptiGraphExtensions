using OptiGraphExtensions.Features.Synonyms.Models;

namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions
{
    public interface ISynonymValidationService
    {
        ValidationResult ValidateSynonym(SynonymModel model);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}