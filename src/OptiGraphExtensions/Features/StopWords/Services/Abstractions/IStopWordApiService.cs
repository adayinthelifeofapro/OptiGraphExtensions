using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.StopWords.Models;

namespace OptiGraphExtensions.Features.StopWords.Services.Abstractions
{
    public interface IStopWordApiService
    {
        Task<IList<StopWord>> GetStopWordsAsync();
        Task<bool> CreateStopWordAsync(CreateStopWordRequest request);
        Task<bool> UpdateStopWordAsync(Guid id, UpdateStopWordRequest request);
        Task<bool> DeleteStopWordAsync(Guid id);
        Task<bool> SyncStopWordsToOptimizelyGraphAsync();
        Task<bool> SyncStopWordsForLanguageAsync(string language);
        Task<bool> DeleteAllStopWordsFromGraphAsync(string language);
    }
}
