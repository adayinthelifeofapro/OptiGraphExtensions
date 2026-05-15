using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.Synonyms.Models
{
    public class BulkSynonymPreviewRow
    {
        public int LineNumber { get; set; }
        public string? Synonym { get; set; }
        public string? Language { get; set; }
        public SynonymSlot? Slot { get; set; }
        public string? RawSlot { get; set; }
        public BulkRowStatus Status { get; set; }
        public string? Message { get; set; }
    }
}
