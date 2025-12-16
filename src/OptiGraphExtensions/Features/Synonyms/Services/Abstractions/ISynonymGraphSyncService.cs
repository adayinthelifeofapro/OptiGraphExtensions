namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

public interface ISynonymGraphSyncService
{
    Task<bool> SyncSynonymsToOptimizelyGraphAsync();
    Task<bool> SyncSynonymsForLanguageAsync(string language);
}