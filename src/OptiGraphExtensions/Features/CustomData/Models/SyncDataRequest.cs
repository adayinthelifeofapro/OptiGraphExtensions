using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Request DTO for syncing data items to a custom data source.
    /// </summary>
    public class SyncDataRequest
    {
        /// <summary>
        /// The source ID to sync data to.
        /// </summary>
        [Required(ErrorMessage = "Source ID is required")]
        [RegularExpression(@"^[a-z0-9]{1,4}$",
            ErrorMessage = "Source ID must be 1-4 lowercase letters and/or numbers")]
        public string SourceId { get; set; } = string.Empty;

        /// <summary>
        /// The data items to sync.
        /// </summary>
        [Required(ErrorMessage = "At least one item is required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<CustomDataItemModel> Items { get; set; } = new();

        /// <summary>
        /// Optional job ID for tracking in Optimizely Graph sync logs.
        /// This value is sent as the og-job-id header.
        /// </summary>
        public string? JobId { get; set; }
    }

    /// <summary>
    /// Request DTO for deleting data from a custom data source.
    /// </summary>
    public class DeleteDataRequest
    {
        /// <summary>
        /// The source ID to delete data from.
        /// </summary>
        [Required(ErrorMessage = "Source ID is required")]
        public string SourceId { get; set; } = string.Empty;

        /// <summary>
        /// Optional list of specific item IDs to delete.
        /// If empty, all items matching the languages filter will be deleted.
        /// </summary>
        public List<string>? ItemIds { get; set; }

        /// <summary>
        /// Optional list of languages to delete data for.
        /// </summary>
        public List<string>? Languages { get; set; }
    }
}
