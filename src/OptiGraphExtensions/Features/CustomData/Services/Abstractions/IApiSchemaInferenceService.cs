namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for inferring content type schemas from external API responses.
    /// </summary>
    public interface IApiSchemaInferenceService
    {
        /// <summary>
        /// Fetches JSON from an external API and infers a content type schema.
        /// </summary>
        /// <param name="apiUrl">The URL of the external API to fetch.</param>
        /// <param name="contentTypeName">The name to use for the inferred content type.</param>
        /// <param name="jsonPath">Optional JSON path to extract a specific part of the response (e.g., "data.items" or "results").</param>
        /// <param name="headers">Optional headers to include in the API request.</param>
        /// <returns>The inferred content type schema.</returns>
        Task<ApiSchemaInferenceResult> InferSchemaFromApiAsync(
            string apiUrl,
            string contentTypeName,
            string? jsonPath = null,
            Dictionary<string, string>? headers = null);
    }

    /// <summary>
    /// Result of schema inference from an external API.
    /// </summary>
    public class ApiSchemaInferenceResult
    {
        /// <summary>
        /// Whether the inference was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The inferred content type schema, if successful.
        /// </summary>
        public Models.ContentTypeSchemaModel? ContentType { get; set; }

        /// <summary>
        /// Error message if inference failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Sample data from the API response for preview.
        /// </summary>
        public List<Dictionary<string, object?>>? SampleData { get; set; }
    }
}
