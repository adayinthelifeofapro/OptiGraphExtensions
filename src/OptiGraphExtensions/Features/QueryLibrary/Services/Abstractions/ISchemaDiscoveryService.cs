using OptiGraphExtensions.Features.QueryLibrary.Models;

namespace OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions
{
    /// <summary>
    /// Service for discovering the Optimizely Graph schema (content types and fields).
    /// </summary>
    public interface ISchemaDiscoveryService
    {
        /// <summary>
        /// Gets all available content types from Optimizely Graph.
        /// </summary>
        Task<IList<string>> GetContentTypesAsync();

        /// <summary>
        /// Gets all queryable fields for a specific content type.
        /// </summary>
        Task<IList<SchemaField>> GetFieldsForContentTypeAsync(string contentType);

        /// <summary>
        /// Clears the schema cache (used after Graph schema changes).
        /// </summary>
        Task ClearCacheAsync();
    }
}
