using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Request DTO for creating or updating a custom data source schema.
    /// </summary>
    public class CreateSchemaRequest
    {
        /// <summary>
        /// The unique identifier for the source (1-4 lowercase alphanumeric characters).
        /// </summary>
        [Required(ErrorMessage = "Source ID is required")]
        [RegularExpression(@"^[a-z0-9]{1,4}$",
            ErrorMessage = "Source ID must be 1-4 lowercase letters and/or numbers")]
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
        /// Global property types available to all content types.
        /// </summary>
        public List<PropertyTypeModel> PropertyTypes { get; set; } = new();

        /// <summary>
        /// The content types defined in this data source.
        /// </summary>
        [Required(ErrorMessage = "At least one content type is required")]
        [MinLength(1, ErrorMessage = "At least one content type is required")]
        public List<ContentTypeSchemaModel> ContentTypes { get; set; } = new();
    }

    /// <summary>
    /// Request DTO for updating an existing schema (partial sync).
    /// </summary>
    public class UpdateSchemaRequest : CreateSchemaRequest
    {
        /// <summary>
        /// Whether this is a partial update (POST) or full sync (PUT).
        /// Full sync deletes all existing data in the source.
        /// </summary>
        public bool IsPartialUpdate { get; set; } = true;
    }
}
