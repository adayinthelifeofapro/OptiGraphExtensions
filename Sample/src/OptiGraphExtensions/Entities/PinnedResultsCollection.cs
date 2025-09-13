using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OptiGraphExtensions.Entities
{
    [Table("tbl_OptiGraphExtensions_PinnedResultsCollections")]
    public class PinnedResultsCollection
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public bool IsActive { get; set; }

        public string? GraphCollectionId { get; set; } // Optimizely Graph collection ID

        public DateTime? CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        // Navigation property for related pinned results
        [JsonIgnore]
        public virtual ICollection<PinnedResult> PinnedResults { get; set; } = new List<PinnedResult>();
    }
}