namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Represents a mapping from an external API field to a schema property.
    /// </summary>
    public class FieldMapping
    {
        /// <summary>
        /// Path in the external API response (supports dot notation, e.g., "data.name").
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Target property name in the schema.
        /// </summary>
        public string TargetProperty { get; set; } = string.Empty;

        /// <summary>
        /// Optional transformation to apply to the value.
        /// </summary>
        public FieldTransformation Transformation { get; set; } = FieldTransformation.None;

        /// <summary>
        /// Optional default value if source field is null or missing.
        /// </summary>
        public string? DefaultValue { get; set; }
    }

    /// <summary>
    /// Type transformations that can be applied during import.
    /// </summary>
    public enum FieldTransformation
    {
        /// <summary>
        /// No transformation, use value as-is.
        /// </summary>
        None = 0,

        /// <summary>
        /// Convert to string.
        /// </summary>
        ToString = 1,

        /// <summary>
        /// Convert to integer.
        /// </summary>
        ToInt = 2,

        /// <summary>
        /// Convert to floating-point number.
        /// </summary>
        ToFloat = 3,

        /// <summary>
        /// Convert to boolean.
        /// </summary>
        ToBoolean = 4,

        /// <summary>
        /// Convert to date (yyyy-MM-dd).
        /// </summary>
        ToDate = 5,

        /// <summary>
        /// Convert to datetime.
        /// </summary>
        ToDateTime = 6
    }
}
