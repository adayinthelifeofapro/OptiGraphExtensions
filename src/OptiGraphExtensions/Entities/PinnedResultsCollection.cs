using System.ComponentModel.DataAnnotations.Schema;

namespace OptiGraphExtensions.Entities
{
    [Table("tbl_OptiGraphExtensions_PinnedResultsCollections")]
    public class PinnedResultsCollection
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        // Navigation property for related pinned results
        public virtual ICollection<PinnedResult> PinnedResults { get; set; } = new List<PinnedResult>();
    }
}