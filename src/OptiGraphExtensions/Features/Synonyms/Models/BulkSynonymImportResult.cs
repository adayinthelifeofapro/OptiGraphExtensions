namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class BulkSynonymImportResult
    {
        public int InsertedCount { get; set; }
        public int SkippedDuplicateCount { get; set; }
        public int InvalidSkippedCount { get; set; }
        public bool GraphSyncAttempted { get; set; }
        public bool GraphSyncSucceeded { get; set; }
        public string? GraphSyncErrorMessage { get; set; }
        public IReadOnlyCollection<string> AffectedLanguages { get; set; } = Array.Empty<string>();
    }
}
