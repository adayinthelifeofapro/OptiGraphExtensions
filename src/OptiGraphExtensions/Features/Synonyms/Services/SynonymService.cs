using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Repositories;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class SynonymService : ISynonymService
    {
        private readonly ISynonymRepository _synonymRepository;

        public SynonymService(ISynonymRepository synonymRepository)
        {
            _synonymRepository = synonymRepository;
        }

        public async Task<IEnumerable<Synonym>> GetAllSynonymsAsync()
        {
            return await _synonymRepository.GetAllAsync();
        }

        public async Task<Synonym?> GetSynonymByIdAsync(Guid id)
        {
            return await _synonymRepository.GetByIdAsync(id);
        }

        public async Task<Synonym> CreateSynonymAsync(string synonymText, string? createdBy = null)
        {
            var synonym = new Synonym
            {
                Id = Guid.NewGuid(),
                SynonymItem = synonymText,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            return await _synonymRepository.CreateAsync(synonym);
        }

        public async Task<Synonym?> UpdateSynonymAsync(Guid id, string synonymText)
        {
            var synonym = await _synonymRepository.GetByIdAsync(id);
            if (synonym == null)
            {
                return null;
            }

            synonym.SynonymItem = synonymText;
            return await _synonymRepository.UpdateAsync(synonym);
        }

        public async Task<bool> DeleteSynonymAsync(Guid id)
        {
            return await _synonymRepository.DeleteAsync(id);
        }

        public async Task<bool> SynonymExistsAsync(Guid id)
        {
            return await _synonymRepository.ExistsAsync(id);
        }
    }
}