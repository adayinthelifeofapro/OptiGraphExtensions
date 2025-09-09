using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Synonyms.Repositories
{
    public interface ISynonymRepository
    {
        Task<IEnumerable<Synonym>> GetAllAsync();
        Task<Synonym?> GetByIdAsync(Guid id);
        Task<Synonym> CreateAsync(Synonym synonym);
        Task<Synonym> UpdateAsync(Synonym synonym);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}