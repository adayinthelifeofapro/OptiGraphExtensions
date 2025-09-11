using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Models;

namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

public interface ISynonymCrudService
{
    Task<IList<Synonym>> GetSynonymsAsync();
    Task<bool> CreateSynonymAsync(CreateSynonymRequest request);
    Task<bool> UpdateSynonymAsync(Guid id, UpdateSynonymRequest request);
    Task<bool> DeleteSynonymAsync(Guid id);
}