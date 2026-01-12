using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Represents a property definition within a content type schema.
    /// </summary>
    public class PropertyTypeModel
    {
        /// <summary>
        /// The name of the property. Must be a valid identifier.
        /// </summary>
        [Required(ErrorMessage = "Property name is required")]
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_]*$",
            ErrorMessage = "Property name must start with a letter and contain only letters, numbers, and underscores")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The data type of the property.
        /// Valid types: String, Int, Float, Boolean, Date, DateTime, StringArray, IntArray, FloatArray
        /// </summary>
        [Required(ErrorMessage = "Property type is required")]
        public string Type { get; set; } = "String";

        /// <summary>
        /// Whether the property should be searchable in Graph queries.
        /// </summary>
        public bool IsSearchable { get; set; } = true;

        /// <summary>
        /// Whether the property is required when creating data items.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// The index type for the property. Options: default, keyword, analyzed.
        /// </summary>
        public string? IndexType { get; set; }

        /// <summary>
        /// Available property types for use in UI dropdowns.
        /// </summary>
        public static IEnumerable<string> AvailableTypes => new[]
        {
            "String",
            "Int",
            "Float",
            "Boolean",
            "Date",
            "DateTime",
            "StringArray",
            "IntArray",
            "FloatArray"
        };
    }
}
