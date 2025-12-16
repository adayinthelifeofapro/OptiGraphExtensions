using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions
{
    public interface ISynonymService
    {
        Task<IEnumerable<Synonym>> GetAllSynonymsAsync();
        Task<IEnumerable<Synonym>> GetSynonymsByLanguageAsync(string language);
        Task<Synonym?> GetSynonymByIdAsync(Guid id);
        Task<Synonym> CreateSynonymAsync(string synonymText, string language, SynonymSlot slot, string? createdBy = null);
        Task<Synonym?> UpdateSynonymAsync(Guid id, string synonymText, string language, SynonymSlot slot);
        Task<bool> DeleteSynonymAsync(Guid id);
        Task<bool> SynonymExistsAsync(Guid id);
    }
}