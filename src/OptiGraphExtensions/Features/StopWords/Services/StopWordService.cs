using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.StopWords.Repositories;
using OptiGraphExtensions.Features.StopWords.Services.Abstractions;

namespace OptiGraphExtensions.Features.StopWords.Services
{
    public class StopWordService : IStopWordService
    {
        private readonly IStopWordRepository _stopWordRepository;

        public StopWordService(IStopWordRepository stopWordRepository)
        {
            _stopWordRepository = stopWordRepository;
        }

        public async Task<IEnumerable<StopWord>> GetAllStopWordsAsync()
        {
            return await _stopWordRepository.GetAllAsync();
        }

        public async Task<IEnumerable<StopWord>> GetStopWordsByLanguageAsync(string language)
        {
            return await _stopWordRepository.GetByLanguageAsync(language);
        }

        public async Task<StopWord?> GetStopWordByIdAsync(Guid id)
        {
            return await _stopWordRepository.GetByIdAsync(id);
        }

        public async Task<StopWord> CreateStopWordAsync(string word, string language, string? createdBy = null)
        {
            var stopWord = new StopWord
            {
                Id = Guid.NewGuid(),
                Word = word,
                Language = language,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            return await _stopWordRepository.CreateAsync(stopWord);
        }

        public async Task<StopWord?> UpdateStopWordAsync(Guid id, string word, string language)
        {
            var stopWord = await _stopWordRepository.GetByIdAsync(id);
            if (stopWord == null)
            {
                return null;
            }

            stopWord.Word = word;
            stopWord.Language = language;
            return await _stopWordRepository.UpdateAsync(stopWord);
        }

        public async Task<bool> DeleteStopWordAsync(Guid id)
        {
            return await _stopWordRepository.DeleteAsync(id);
        }

        public async Task<bool> StopWordExistsAsync(Guid id)
        {
            return await _stopWordRepository.ExistsAsync(id);
        }
    }
}
