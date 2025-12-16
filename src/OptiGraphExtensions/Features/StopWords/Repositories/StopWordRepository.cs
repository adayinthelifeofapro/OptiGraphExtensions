using Microsoft.EntityFrameworkCore;

using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.StopWords.Repositories
{
    public class StopWordRepository : IStopWordRepository
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;

        public StopWordRepository(IOptiGraphExtensionsDataContext dataContext)
        {
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        }

        public async Task<IEnumerable<StopWord>> GetAllAsync()
        {
            return await _dataContext.StopWords.ToListAsync();
        }

        public async Task<IEnumerable<StopWord>> GetByLanguageAsync(string language)
        {
            return await _dataContext.StopWords
                .Where(s => s.Language == language)
                .ToListAsync();
        }

        public async Task<StopWord?> GetByIdAsync(Guid id)
        {
            return await _dataContext.StopWords.FindAsync(id);
        }

        public async Task<StopWord> CreateAsync(StopWord stopWord)
        {
            ArgumentNullException.ThrowIfNull(stopWord);
            _dataContext.StopWords.Add(stopWord);
            await _dataContext.SaveChangesAsync();
            return stopWord;
        }

        public async Task<StopWord> UpdateAsync(StopWord stopWord)
        {
            ArgumentNullException.ThrowIfNull(stopWord);
            _dataContext.StopWords.Update(stopWord);
            await _dataContext.SaveChangesAsync();
            return stopWord;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var stopWord = await _dataContext.StopWords.FindAsync(id);
            if (stopWord == null)
            {
                return false;
            }

            _dataContext.StopWords.Remove(stopWord);
            await _dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dataContext.StopWords.AnyAsync(e => e.Id == id);
        }
    }
}
