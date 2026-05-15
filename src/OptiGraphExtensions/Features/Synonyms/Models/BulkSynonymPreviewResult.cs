namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class BulkSynonymPreviewResult
    {
        public IReadOnlyList<BulkSynonymPreviewRow> Rows { get; set; } = Array.Empty<BulkSynonymPreviewRow>();
        public IReadOnlyList<string> GlobalErrors { get; set; } = Array.Empty<string>();

        public int ValidCount => Rows.Count(r => r.Status == BulkRowStatus.Valid);
        public int DuplicateInFileCount => Rows.Count(r => r.Status == BulkRowStatus.DuplicateInFile);
        public int DuplicateInDatabaseCount => Rows.Count(r => r.Status == BulkRowStatus.DuplicateInDatabase);
        public int InvalidCount => Rows.Count(r => r.Status == BulkRowStatus.Invalid);
        public int TotalCount => Rows.Count;
        public bool HasAnythingToImport => ValidCount > 0;
    }
}
