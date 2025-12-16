using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.StopWords.Repositories
{
    public interface IStopWordRepository
    {
        Task<IEnumerable<StopWord>> GetAllAsync();
        Task<IEnumerable<StopWord>> GetByLanguageAsync(string language);
        Task<StopWord?> GetByIdAsync(Guid id);
        Task<StopWord> CreateAsync(StopWord stopWord);
        Task<StopWord> UpdateAsync(StopWord stopWord);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
