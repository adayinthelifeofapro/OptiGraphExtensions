using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptiGraphExtensions.Entities
{
    /// <summary>
    /// Represents a saved configuration for importing data from external APIs.
    /// </summary>
    [Table("tbl_OptiGraphExtensions_ImportConfigurations")]
    public class ImportConfiguration
    {
        /// <summary>
        /// Unique identifier for this configuration.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// User-friendly name for this import configuration.
        /// </summary>
        [Required]
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
        [Required]
        [StringLength(4)]
        public string TargetSourceId { get; set; } = string.Empty;

        /// <summary>
        /// The content type to import data as.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string TargetContentType { get; set; } = string.Empty;

        /// <summary>
        /// The external API endpoint URL.
        /// </summary>
        [Required]
        [StringLength(2048)]
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method for the API request (GET, POST, etc.).
        /// </summary>
        [Required]
        [StringLength(10)]
        public string HttpMethod { get; set; } = "GET";

        /// <summary>
        /// Authentication type: None, ApiKey, Basic, Bearer
        /// </summary>
        [Required]
        public AuthenticationType AuthType { get; set; } = AuthenticationType.None;

        /// <summary>
        /// For ApiKey: the header name (e.g., "X-API-Key").
        /// For Basic: the username.
        /// For Bearer: not used.
        /// </summary>
        [StringLength(255)]
        public string? AuthKeyOrUsername { get; set; }

        /// <summary>
        /// For ApiKey: the API key value.
        /// For Basic: the password.
        /// For Bearer: the token.
        /// </summary>
        [StringLength(2048)]
        public string? AuthValueOrPassword { get; set; }

        /// <summary>
        /// JSON-serialized field mapping configuration.
        /// Maps external field paths to CustomDataItemModel properties.
        /// </summary>
        public string? FieldMappingsJson { get; set; }

        /// <summary>
        /// The external field path to use as the item ID (for deduplication).
        /// </summary>
        [Required]
        [StringLength(255)]
        public string IdFieldMapping { get; set; } = string.Empty;

        /// <summary>
        /// Language routing value for imported items.
        /// </summary>
        [StringLength(10)]
        public string? LanguageRouting { get; set; }

        /// <summary>
        /// JSON-serialized custom HTTP headers to include.
        /// </summary>
        public string? CustomHeadersJson { get; set; }

        /// <summary>
        /// Whether this configuration is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last time this import was executed.
        /// </summary>
        public DateTime? LastImportAt { get; set; }

        /// <summary>
        /// Number of items imported in last execution.
        /// </summary>
        public int? LastImportCount { get; set; }

        /// <summary>
        /// When this configuration was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Who created this configuration.
        /// </summary>
        [StringLength(255)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// When this configuration was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Who last updated this configuration.
        /// </summary>
        [StringLength(255)]
        public string? UpdatedBy { get; set; }
    }
}
