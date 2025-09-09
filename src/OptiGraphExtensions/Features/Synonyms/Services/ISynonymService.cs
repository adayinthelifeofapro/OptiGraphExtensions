using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public interface ISynonymService
    {
        Task<IEnumerable<Synonym>> GetAllSynonymsAsync();
        Task<Synonym?> GetSynonymByIdAsync(Guid id);
        Task<Synonym> CreateSynonymAsync(string synonymText, string? createdBy = null);
        Task<Synonym?> UpdateSynonymAsync(Guid id, string synonymText);
        Task<bool> DeleteSynonymAsync(Guid id);
        Task<bool> SynonymExistsAsync(Guid id);
    }
}