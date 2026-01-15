using System.ComponentModel.DataAnnotations;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// DTO model for import configuration UI operations.
    /// </summary>
    public class ImportConfigurationModel
    {
        /// <summary>
        /// Unique identifier (null for new configurations).
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// User-friendly name for this import configuration.
        /// </summary>
        [Required(ErrorMessage = "Configuration name is required")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of what this import does.
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// The custom data source ID to import data into.
        /// </summary>
        [Required(ErrorMessage = "Target source ID is required")]
        public string TargetSourceId { get; set; } = string.Empty;

        /// <summary>
        /// The content type to import data as.
        /// </summary>
        [Required(ErrorMessage = "Target content type is required")]
        public string TargetContentType { get; set; } = string.Empty;

        /// <summary>
        /// The external API endpoint URL.
        /// </summary>
        [Required(ErrorMessage = "API URL is required")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method for the API request.
        /// </summary>
        public string HttpMethod { get; set; } = "GET";

        /// <summary>
        /// Authentication type.
        /// </summary>
        public AuthenticationType AuthType { get; set; } = AuthenticationType.None;

        /// <summary>
        /// For ApiKey: the header name (e.g., "X-API-Key").
        /// For Basic: the username.
        /// </summary>
        public string? AuthKeyOrUsername { get; set; }

        /// <summary>
        /// For ApiKey: the API key value.
        /// For Basic: the password.
        /// For Bearer: the token.
        /// </summary>
        public string? AuthValueOrPassword { get; set; }

        /// <summary>
        /// Field mappings from external data to schema properties.
        /// </summary>
        public List<FieldMapping> FieldMappings { get; set; } = new();

        /// <summary>
        /// The external field path to use as the item ID (required for deduplication).
        /// </summary>
        [Required(ErrorMessage = "ID field mapping is required for deduplication")]
        public string IdFieldMapping { get; set; } = string.Empty;

        /// <summary>
        /// Language routing value for imported items.
        /// </summary>
        public string? LanguageRouting { get; set; }

        /// <summary>
        /// Optional JSON path to navigate to the data array within the response.
        /// Examples: "data", "results", "items", "data.records"
        /// </summary>
        [StringLength(255)]
        public string? JsonPath { get; set; }

        /// <summary>
        /// Custom HTTP headers to include in the request.
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();

        /// <summary>
        /// Whether this configuration is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last time this import was executed (read-only).
        /// </summary>
        public DateTime? LastImportAt { get; set; }

        /// <summary>
        /// Number of items imported in last execution (read-only).
        /// </summary>
        public int? LastImportCount { get; set; }

        /// <summary>
        /// Available HTTP methods for the dropdown.
        /// </summary>
        public static IEnumerable<string> AvailableHttpMethods => new[] { "GET", "POST" };
    }
}
