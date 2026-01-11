namespace OptiGraphExtensions.Features.QueryLibrary.Models
{
    /// <summary>
    /// Represents a field from the Optimizely Graph schema for use in the visual query builder.
    /// </summary>
    public class SchemaField
    {
        /// <summary>
        /// The display name of the field.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The full path to the field (e.g., "ContentLink.GuidValue").
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The GraphQL type of the field (String, Int, DateTime, Boolean, Object, etc.).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Whether this field is a nested object type.
        /// </summary>
        public bool IsNested { get; set; }

        /// <summary>
        /// Whether this field is a list/array type.
        /// </summary>
        public bool IsList { get; set; }

        /// <summary>
        /// Child fields for nested object types.
        /// </summary>
        public List<SchemaField> Children { get; set; } = new();
    }
}
