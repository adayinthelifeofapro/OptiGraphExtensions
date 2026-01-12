using Microsoft.EntityFrameworkCore;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.PinnedResults.Repositories
{
    public class PinnedResultsCollectionRepository : IPinnedResultsCollectionRepository
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public PinnedResultsCollectionRepository(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IEnumerable<PinnedResultsCollection>> GetAllAsync()
        {
            return await _dataContext.PinnedResultsCollections.AsNoTracking().ToListAsync();
        }

        public async Task<PinnedResultsCollection?> GetByIdAsync(Guid id)
        {
            return await _dataContext.PinnedResultsCollections.FindAsync(id);
        }

        public async Task<PinnedResultsCollection> CreateAsync(PinnedResultsCollection collection)
        {
            _dataContext.PinnedResultsCollections.Add(collection);
            await _dataContext.SaveChangesAsync();
            return collection;
        }

        public async Task<PinnedResultsCollection> UpdateAsync(PinnedResultsCollection collection)
        {
            _dataContext.PinnedResultsCollections.Update(collection);
            await _dataContext.SaveChangesAsync();
            return collection;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var collection = await _dataContext.PinnedResultsCollections.FindAsync(id);
            if (collection == null)
            {
                return false;
            }

            _dataContext.PinnedResultsCollections.Remove(collection);
            await _dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dataContext.PinnedResultsCollections.AnyAsync(e => e.Id == id);
        }

        public async Task<PinnedResultsCollection?> UpdateGraphCollectionIdAsync(Guid id, string? graphCollectionId)
        {
            var collection = await _dataContext.PinnedResultsCollections.FindAsync(id);
            if (collection == null)
            {
                return null;
            }

            collection.GraphCollectionId = graphCollectionId;
            await _dataContext.SaveChangesAsync();
            return collection;
        }

        public async Task<PinnedResultsCollection?> GetByGraphCollectionIdAsync(string graphCollectionId)
        {
            return await _dataContext.PinnedResultsCollections
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.GraphCollectionId == graphCollectionId);
        }

        public async Task<PinnedResultsCollection> UpsertAsync(PinnedResultsCollection collection)
        {
            var existingCollection = await _dataContext.PinnedResultsCollections
                .FirstOrDefaultAsync(c => c.GraphCollectionId == collection.GraphCollectionId);

            if (existingCollection != null)
            {
                // Update existing collection
                existingCollection.Title = collection.Title;
                existingCollection.IsActive = collection.IsActive;
                // Don't update CreatedAt/CreatedBy for existing records
                // No need to call Update() as the entity is already tracked
                await _dataContext.SaveChangesAsync();
                return existingCollection;
            }
            else
            {
                // Create new collection
                _dataContext.PinnedResultsCollections.Add(collection);
                await _dataContext.SaveChangesAsync();
                return collection;
            }
        }
    }
}