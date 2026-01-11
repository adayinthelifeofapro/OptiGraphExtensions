using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptiGraphExtensions.Entities
{
    /// <summary>
    /// Represents a saved query configuration for the Query Library.
    /// Supports both visual query builder and raw GraphQL queries.
    /// </summary>
    [Table("tbl_OptiGraphExtensions_SavedQueries")]
    public class SavedQuery
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// The type of query: Visual (0) or Raw (1).
        /// </summary>
        public QueryType QueryType { get; set; } = QueryType.Visual;

        // === Visual Query Fields ===

        /// <summary>
        /// The content type to query (for Visual queries).
        /// </summary>
        [StringLength(255)]
        public string? ContentType { get; set; }

        /// <summary>
        /// JSON-serialized List of selected field paths (for Visual queries).
        /// Example: ["Name", "ContentLink.GuidValue", "Changed"]
        /// </summary>
        public string? SelectedFieldsJson { get; set; }

        /// <summary>
        /// JSON-serialized List of QueryFilter objects (for Visual queries).
        /// </summary>
        public string? FiltersJson { get; set; }

        /// <summary>
        /// Language filter for the query.
        /// </summary>
        [StringLength(10)]
        public string? Language { get; set; }

        /// <summary>
        /// Field to sort results by (for Visual queries).
        /// </summary>
        [StringLength(100)]
        public string? SortField { get; set; }

        /// <summary>
        /// Whether to sort in descending order.
        /// </summary>
        public bool SortDescending { get; set; }

        // === Raw Query Fields ===

        /// <summary>
        /// The raw GraphQL query string (for Raw queries).
        /// </summary>
        public string? RawGraphQuery { get; set; }

        /// <summary>
        /// JSON-serialized Dictionary of query variables (for Raw queries).
        /// Example: { "limit": 100, "locale": ["en"] }
        /// </summary>
        public string? QueryVariablesJson { get; set; }

        // === Common Fields ===

        /// <summary>
        /// Number of items per page for pagination.
        /// </summary>
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// Whether the query is active and visible to users.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // === Audit Fields ===

        public DateTime CreatedAt { get; set; }

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [StringLength(255)]
        public string? UpdatedBy { get; set; }
    }
}
