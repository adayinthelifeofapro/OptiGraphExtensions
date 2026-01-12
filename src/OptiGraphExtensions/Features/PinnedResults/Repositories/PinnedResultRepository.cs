using Microsoft.EntityFrameworkCore;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults.Repositories
{
    public class PinnedResultRepository : IPinnedResultRepository
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public PinnedResultRepository(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IEnumerable<PinnedResult>> GetAllAsync(Guid? collectionId = null)
        {
            var query = _dataContext.PinnedResults.AsNoTracking();

            if (collectionId.HasValue)
            {
                query = query.Where(pr => pr.CollectionId == collectionId.Value);
            }

            return await query.OrderBy(pr => pr.Priority).ToListAsync();
        }

        public async Task<PinnedResult?> GetByIdAsync(Guid id)
        {
            return await _dataContext.PinnedResults.FindAsync(id);
        }

        public async Task<PinnedResult?> GetByIdWithCollectionAsync(Guid id)
        {
            return await _dataContext.PinnedResults
                .Include(pr => pr.Collection)
                .FirstOrDefaultAsync(pr => pr.Id == id);
        }

        public async Task<PinnedResult> CreateAsync(PinnedResult pinnedResult)
        {
            _dataContext.PinnedResults.Add(pinnedResult);
            await _dataContext.SaveChangesAsync();
            return pinnedResult;
        }

        public async Task<PinnedResult> UpdateAsync(PinnedResult pinnedResult)
        {
            _dataContext.PinnedResults.Update(pinnedResult);
            await _dataContext.SaveChangesAsync();
            return pinnedResult;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var pinnedResult = await _dataContext.PinnedResults.FindAsync(id);
            if (pinnedResult == null)
            {
                return false;
            }

            _dataContext.PinnedResults.Remove(pinnedResult);
            await _dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dataContext.PinnedResults.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> CollectionExistsAsync(Guid collectionId)
        {
            return await _dataContext.PinnedResultsCollections.AnyAsync(c => c.Id == collectionId);
        }

        public async Task<IEnumerable<PinnedResult>> GetByCollectionIdAsync(Guid collectionId)
        {
            return await _dataContext.PinnedResults
                .AsNoTracking()
                .Where(pr => pr.CollectionId == collectionId)
                .OrderBy(pr => pr.Priority)
                .ToListAsync();
        }

        public async Task<bool> DeleteByCollectionIdAsync(Guid collectionId)
        {
            var relatedPinnedResults = await _dataContext.PinnedResults
                .Where(pr => pr.CollectionId == collectionId)
                .ToListAsync();
            
            if (relatedPinnedResults.Any())
            {
                _dataContext.PinnedResults.RemoveRange(relatedPinnedResults);
                await _dataContext.SaveChangesAsync();
                return true;
            }

            return true;
        }
    }
}