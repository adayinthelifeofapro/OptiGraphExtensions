using System.ComponentModel.DataAnnotations.Schema;

namespace OptiGraphExtensions.Entities
{
    [Table("tbl_OptiGraphExtensions_PinnedResults")]
    public class PinnedResult
    {
        public Guid Id { get; set; }

        public Guid CollectionId { get; set; }

        public string? Phrases { get; set; } // Comma-separated search phrases

        public string? TargetKey { get; set; } // Content GUID to pin

        public string? Language { get; set; } // Language code (e.g., "en", "sv")

        public int Priority { get; set; } // Numeric ordering value

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        // Navigation property for the parent collection
        [ForeignKey("CollectionId")]
        public virtual PinnedResultsCollection? Collection { get; set; }
    }
}