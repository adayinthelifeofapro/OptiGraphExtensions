using OptiGraphExtensions.Features.StopWords.Models;
using OptiGraphExtensions.Features.StopWords.Services.Abstractions;

namespace OptiGraphExtensions.Features.StopWords.Services
{
    public class StopWordValidationService : IStopWordValidationService
    {
        public StopWordValidationResult ValidateStopWord(StopWordModel model)
        {
            if (model == null)
            {
                return new StopWordValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Stop word model is required."
                };
            }

            if (string.IsNullOrWhiteSpace(model.Word))
            {
                return new StopWordValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Stop word is required."
                };
            }

            if (model.Word.Length > 100)
            {
                return new StopWordValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Stop word must be less than 100 characters."
                };
            }

            // Stop words should be single words without spaces (typically)
            if (model.Word.Contains(' '))
            {
                return new StopWordValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Stop word should be a single word without spaces."
                };
            }

            if (string.IsNullOrWhiteSpace(model.Language))
            {
                return new StopWordValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Language is required."
                };
            }

            return new StopWordValidationResult
            {
                IsValid = true
            };
        }
    }
}
