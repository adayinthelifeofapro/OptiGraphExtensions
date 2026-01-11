using System.ComponentModel.DataAnnotations;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.QueryLibrary.Models
{
    /// <summary>
    /// DTO model for saved queries used in API and UI.
    /// </summary>
    public class SavedQueryModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name must be less than 255 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description must be less than 1000 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// The type of query (Visual or Raw).
        /// </summary>
        public QueryType QueryType { get; set; } = QueryType.Visual;

        // === Visual Query Fields ===

        /// <summary>
        /// The content type to query (required for Visual queries).
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// List of field paths to select.
        /// </summary>
        public List<string> SelectedFields { get; set; } = new();

        /// <summary>
        /// List of filter conditions.
        /// </summary>
        public List<QueryFilter> Filters { get; set; } = new();

        /// <summary>
        /// Language filter.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Field to sort results by.
        /// </summary>
        public string? SortField { get; set; }

        /// <summary>
        /// Whether to sort in descending order.
        /// </summary>
        public bool SortDescending { get; set; }

        // === Raw Query Fields ===

        /// <summary>
        /// The raw GraphQL query string (required for Raw queries).
        /// </summary>
        public string? RawGraphQuery { get; set; }

        /// <summary>
        /// Query variables as JSON string.
        /// </summary>
        public string? QueryVariablesJson { get; set; }

        // === Common Fields ===

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// Whether the query is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // === Audit Fields (read-only) ===

        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
