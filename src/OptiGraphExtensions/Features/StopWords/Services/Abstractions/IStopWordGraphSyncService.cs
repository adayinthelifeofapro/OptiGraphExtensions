namespace OptiGraphExtensions.Features.StopWords.Services.Abstractions
{
    public interface IStopWordGraphSyncService
    {
        Task<bool> SyncStopWordsToOptimizelyGraphAsync();
        Task<bool> SyncStopWordsForLanguageAsync(string language);
        Task<bool> DeleteAllStopWordsFromGraphAsync(string language);
    }
}
