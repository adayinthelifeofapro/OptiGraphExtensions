using OptiGraphExtensions.Features.Synonyms.Models;

namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions
{
    public interface ISynonymBulkImportService
    {
        Task<BulkSynonymPreviewResult> BuildPreviewAsync(string csvContent);
        Task<BulkSynonymImportResult> ImportAsync(BulkSynonymPreviewResult preview, string? createdBy);
    }
}
