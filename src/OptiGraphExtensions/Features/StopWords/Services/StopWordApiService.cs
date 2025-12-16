using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.StopWords.Models;
using OptiGraphExtensions.Features.StopWords.Services.Abstractions;

namespace OptiGraphExtensions.Features.StopWords.Services
{
    public class StopWordApiService : IStopWordApiService
    {
        private readonly IStopWordCrudService _stopWordCrudService;
        private readonly IStopWordGraphSyncService _graphSyncService;

        public StopWordApiService(
            IStopWordCrudService stopWordCrudService,
            IStopWordGraphSyncService graphSyncService)
        {
            _stopWordCrudService = stopWordCrudService;
            _graphSyncService = graphSyncService;
        }

        public async Task<IList<StopWord>> GetStopWordsAsync()
        {
            return await _stopWordCrudService.GetStopWordsAsync();
        }

        public async Task<bool> CreateStopWordAsync(CreateStopWordRequest request)
        {
            return await _stopWordCrudService.CreateStopWordAsync(request);
        }

        public async Task<bool> UpdateStopWordAsync(Guid id, UpdateStopWordRequest request)
        {
            return await _stopWordCrudService.UpdateStopWordAsync(id, request);
        }

        public async Task<bool> DeleteStopWordAsync(Guid id)
        {
            return await _stopWordCrudService.DeleteStopWordAsync(id);
        }

        public async Task<bool> SyncStopWordsToOptimizelyGraphAsync()
        {
            return await _graphSyncService.SyncStopWordsToOptimizelyGraphAsync();
        }

        public async Task<bool> SyncStopWordsForLanguageAsync(string language)
        {
            return await _graphSyncService.SyncStopWordsForLanguageAsync(language);
        }

        public async Task<bool> DeleteAllStopWordsFromGraphAsync(string language)
        {
            return await _graphSyncService.DeleteAllStopWordsFromGraphAsync(language);
        }
    }
}
