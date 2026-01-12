using System.ComponentModel.DataAnnotations;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Represents a data item to be synced to a custom data source in Optimizely Graph.
    /// </summary>
    public class CustomDataItemModel
    {
        /// <summary>
        /// The unique identifier for this item within the source.
        /// </summary>
        [Required(ErrorMessage = "Item ID is required")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The content type this item belongs to.
        /// </summary>
        [Required(ErrorMessage = "Content type is required")]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// The language routing for this item (e.g., "en", "sv").
        /// </summary>
        public string? LanguageRouting { get; set; }

        /// <summary>
        /// The property values for this item.
        /// Keys are property names, values are the property values.
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new();

        /// <summary>
        /// Display name for the item (derived from a Name property if available).
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// When this item was last modified.
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets a property value by name.
        /// </summary>
        public T? GetProperty<T>(string name)
        {
            if (Properties.TryGetValue(name, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Sets a property value by name.
        /// </summary>
        public void SetProperty(string name, object? value)
        {
            Properties[name] = value;
        }
    }
}
