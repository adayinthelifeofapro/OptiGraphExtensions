using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Represents a content type definition within a custom data source schema.
    /// </summary>
    public class ContentTypeSchemaModel
    {
        /// <summary>
        /// The name of the content type. Must start with a letter.
        /// </summary>
        [Required(ErrorMessage = "Content type name is required")]
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_]*$",
            ErrorMessage = "Content type name must start with a letter and contain only letters, numbers, and underscores")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable label for the content type.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Optional base type that this content type inherits from.
        /// </summary>
        public string? BaseType { get; set; }

        /// <summary>
        /// The properties defined for this content type.
        /// </summary>
        public List<PropertyTypeModel> Properties { get; set; } = new();
    }
}
