using Microsoft.EntityFrameworkCore;

using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Synonyms.Repositories
{
    public class SynonymRepository : ISynonymRepository
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public SynonymRepository(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public async Task<IEnumerable<Synonym>> GetAllAsync()
        {
            return await _dataContext.Synonyms.ToListAsync();
        }

        public async Task<Synonym?> GetByIdAsync(Guid id)
        {
            return await _dataContext.Synonyms.FindAsync(id);
        }

        public async Task<Synonym> CreateAsync(Synonym synonym)
        {
            ArgumentNullException.ThrowIfNull(synonym);
            _dataContext.Synonyms.Add(synonym);
            await _dataContext.SaveChangesAsync();
            return synonym;
        }

        public async Task<Synonym> UpdateAsync(Synonym synonym)
        {
            ArgumentNullException.ThrowIfNull(synonym);
            _dataContext.Synonyms.Update(synonym);
            await _dataContext.SaveChangesAsync();
            return synonym;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var synonym = await _dataContext.Synonyms.FindAsync(id);
            if (synonym == null)
            {
                return false;
            }

            _dataContext.Synonyms.Remove(synonym);
            await _dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dataContext.Synonyms.AnyAsync(e => e.Id == id);
        }
    }
}