using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.QueryLibrary.Repositories
{
    public interface ISavedQueryRepository
    {
        Task<IEnumerable<SavedQuery>> GetAllAsync();
        Task<IEnumerable<SavedQuery>> GetActiveAsync();
        Task<SavedQuery?> GetByIdAsync(Guid id);
        Task<SavedQuery> CreateAsync(SavedQuery query);
        Task<SavedQuery> UpdateAsync(SavedQuery query);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
    }
}
