using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.StopWords.Services.Abstractions
{
    public interface IStopWordService
    {
        Task<IEnumerable<StopWord>> GetAllStopWordsAsync();
        Task<IEnumerable<StopWord>> GetStopWordsByLanguageAsync(string language);
        Task<StopWord?> GetStopWordByIdAsync(Guid id);
        Task<StopWord> CreateStopWordAsync(string word, string language, string? createdBy = null);
        Task<StopWord?> UpdateStopWordAsync(Guid id, string word, string language);
        Task<bool> DeleteStopWordAsync(Guid id);
        Task<bool> StopWordExistsAsync(Guid id);
    }
}
