using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Represents a custom data source in Optimizely Graph.
    /// </summary>
    public class CustomDataSourceModel
    {
        /// <summary>
        /// The unique identifier for the source.
        /// Must be 1-4 lowercase characters (a-z) and/or numbers (0-9).
        /// </summary>
        [Required(ErrorMessage = "Source ID is required")]
        [RegularExpression(@"^[a-z0-9]{1,4}$",
            ErrorMessage = "Source ID must be 1-4 lowercase letters and/or numbers")]
        [StringLength(4, MinimumLength = 1,
            ErrorMessage = "Source ID must be between 1 and 4 characters")]
        public string SourceId { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable label for the data source.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// The languages supported by this data source.
        /// </summary>
        [Required(ErrorMessage = "At least one language is required")]
        [MinLength(1, ErrorMessage = "At least one language is required")]
        public List<string> Languages { get; set; } = new();

        /// <summary>
        /// The content types defined in this data source.
        /// </summary>
        public List<ContentTypeSchemaModel> ContentTypes { get; set; } = new();

        /// <summary>
        /// Global property types available to all content types.
        /// </summary>
        public List<PropertyTypeModel> PropertyTypes { get; set; } = new();

        /// <summary>
        /// Whether this source has data synced to Graph.
        /// </summary>
        public bool HasData { get; set; }

        /// <summary>
        /// When the schema was last synced to Graph.
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// The count of content types in this source.
        /// </summary>
        public int ContentTypeCount => ContentTypes?.Count ?? 0;

        /// <summary>
        /// Languages formatted as a comma-separated string for display.
        /// </summary>
        public string LanguagesDisplay => Languages != null && Languages.Any()
            ? string.Join(", ", Languages)
            : "-";
    }
}
