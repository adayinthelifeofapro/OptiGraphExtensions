using Microsoft.EntityFrameworkCore;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.QueryLibrary.Repositories
{
    public class SavedQueryRepository : ISavedQueryRepository
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public SavedQueryRepository(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public async Task<IEnumerable<SavedQuery>> GetAllAsync()
        {
            return await _dataContext.SavedQueries
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SavedQuery>> GetActiveAsync()
        {
            return await _dataContext.SavedQueries
                .Where(q => q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task<SavedQuery?> GetByIdAsync(Guid id)
        {
            return await _dataContext.SavedQueries.FindAsync(id);
        }

        public async Task<SavedQuery> CreateAsync(SavedQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);
            _dataContext.SavedQueries.Add(query);
            await _dataContext.SaveChangesAsync();
            return query;
        }

        public async Task<SavedQuery> UpdateAsync(SavedQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);
            _dataContext.SavedQueries.Update(query);
            await _dataContext.SaveChangesAsync();
            return query;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var query = await _dataContext.SavedQueries.FindAsync(id);
            if (query == null)
            {
                return false;
            }

            _dataContext.SavedQueries.Remove(query);
            await _dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dataContext.SavedQueries.AnyAsync(q => q.Id == id);
        }

        public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
        {
            var query = _dataContext.SavedQueries.Where(q => q.Name == name);

            if (excludeId.HasValue)
            {
                query = query.Where(q => q.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
