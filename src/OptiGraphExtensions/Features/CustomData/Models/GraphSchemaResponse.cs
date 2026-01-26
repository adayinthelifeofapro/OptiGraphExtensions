using System.Text.Json.Serialization;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Response model for deserializing schema data from Optimizely Graph API.
    /// The API returns contentTypes and propertyTypes as objects (dictionaries) with names as keys.
    /// </summary>
    public class GraphSchemaResponse
    {
        /// <summary>
        /// Property types defined in the schema.
        /// Format: { "TypeName": { "properties": {...} } }
        /// </summary>
        [JsonPropertyName("propertyTypes")]
        public Dictionary<string, GraphPropertyTypeDefinition>? PropertyTypes { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("languages")]
        public List<string>? Languages { get; set; }

        /// <summary>
        /// Content types defined in the schema.
        /// Format: { "TypeName": { "contentType": [], "properties": {...} } }
        /// </summary>
        [JsonPropertyName("contentTypes")]
        public Dictionary<string, GraphContentTypeDefinition>? ContentTypes { get; set; }
    }

    /// <summary>
    /// Property type definition from Graph API.
    /// </summary>
    public class GraphPropertyTypeDefinition
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, GraphPropertyDefinition>? Properties { get; set; }
    }

    /// <summary>
    /// Property definition within a content type.
    /// </summary>
    public class GraphPropertyDefinition
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("searchable")]
        public bool? Searchable { get; set; }

        [JsonPropertyName("index")]
        public string? Index { get; set; }
    }

    /// <summary>
    /// Content type definition from Graph API.
    /// </summary>
    public class GraphContentTypeDefinition
    {
        /// <summary>
        /// Base types for inheritance. The API uses "contentType" array for this.
        /// </summary>
        [JsonPropertyName("contentType")]
        public List<string>? BaseTypes { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        /// <summary>
        /// Properties defined for this content type.
        /// Format: { "PropertyName": { "type": "String", "searchable": true } }
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, GraphPropertyDefinition>? Properties { get; set; }
    }

    /// <summary>
    /// Response model for a property type from Graph API (legacy list format).
    /// </summary>
    public class GraphPropertyTypeResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("searchable")]
        public bool? Searchable { get; set; }

        [JsonPropertyName("index")]
        public string? Index { get; set; }
    }

    /// <summary>
    /// Response model for a content type from Graph API (legacy list format).
    /// </summary>
    public class GraphContentTypeResponse
    {
        /// <summary>
        /// The content type name. API uses "contentType" in request/response.
        /// </summary>
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        /// <summary>
        /// Alias for backward compatibility - maps to ContentType.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets the effective name (prefers ContentType, falls back to Name).
        /// </summary>
        public string? EffectiveName => ContentType ?? Name;

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("base")]
        public string? Base { get; set; }

        [JsonPropertyName("properties")]
        public List<GraphPropertyTypeResponse>? Properties { get; set; }
    }

    /// <summary>
    /// Response model for the v3/sources endpoint that lists all content sources.
    /// </summary>
    public class GraphSourcesResponse
    {
        [JsonPropertyName("sources")]
        public List<GraphSourceInfo>? Sources { get; set; }
    }

    /// <summary>
    /// Information about a single content source from the v3/sources endpoint.
    /// </summary>
    public class GraphSourceInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("languages")]
        public List<string>? Languages { get; set; }
    }

    /// <summary>
    /// Response model for API errors from Optimizely Graph.
    /// </summary>
    public class GraphApiErrorResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; }
    }

    /// <summary>
    /// Response model for sync operation results.
    /// </summary>
    public class GraphSyncResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }
    }
}
