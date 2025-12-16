using OptiGraphExtensions.Features.StopWords.Models;

namespace OptiGraphExtensions.Features.StopWords.Services.Abstractions
{
    public interface IStopWordValidationService
    {
        StopWordValidationResult ValidateStopWord(StopWordModel model);
    }

    public class StopWordValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
