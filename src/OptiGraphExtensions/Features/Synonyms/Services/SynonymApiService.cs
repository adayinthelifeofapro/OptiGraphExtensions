using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class SynonymApiService : ISynonymApiService
    {
        private readonly ISynonymCrudService _synonymCrudService;
        private readonly ISynonymGraphSyncService _graphSyncService;

        public SynonymApiService(
            ISynonymCrudService synonymCrudService,
            ISynonymGraphSyncService graphSyncService)
        {
            _synonymCrudService = synonymCrudService;
            _graphSyncService = graphSyncService;
        }

        public async Task<IList<Synonym>> GetSynonymsAsync()
        {
            return await _synonymCrudService.GetSynonymsAsync();
        }

        public async Task<bool> CreateSynonymAsync(CreateSynonymRequest request)
        {
            return await _synonymCrudService.CreateSynonymAsync(request);
        }

        public async Task<bool> UpdateSynonymAsync(Guid id, UpdateSynonymRequest request)
        {
            return await _synonymCrudService.UpdateSynonymAsync(id, request);
        }

        public async Task<bool> DeleteSynonymAsync(Guid id)
        {
            return await _synonymCrudService.DeleteSynonymAsync(id);
        }

        public async Task<bool> SyncSynonymsToOptimizelyGraphAsync()
        {
            return await _graphSyncService.SyncSynonymsToOptimizelyGraphAsync();
        }

        public async Task<bool> SyncSynonymsForLanguageAsync(string language)
        {
            return await _graphSyncService.SyncSynonymsForLanguageAsync(language);
        }
    }
}