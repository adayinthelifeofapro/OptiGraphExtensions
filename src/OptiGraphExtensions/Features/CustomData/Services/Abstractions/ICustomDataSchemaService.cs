using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for managing custom data source schemas in Optimizely Graph.
    /// </summary>
    public interface ICustomDataSchemaService
    {
        /// <summary>
        /// Gets all custom data sources from Optimizely Graph.
        /// </summary>
        Task<IEnumerable<CustomDataSourceModel>> GetAllSourcesAsync();

        /// <summary>
        /// Gets a specific custom data source by its ID.
        /// </summary>
        /// <param name="sourceId">The source ID (1-4 characters).</param>
        Task<CustomDataSourceModel?> GetSourceByIdAsync(string sourceId);

        /// <summary>
        /// Creates a new custom data source schema using full sync (PUT).
        /// WARNING: This will delete all existing data in the source.
        /// </summary>
        /// <param name="request">The schema creation request.</param>
        Task<CustomDataSourceModel> CreateSchemaAsync(CreateSchemaRequest request);

        /// <summary>
        /// Updates an existing schema using partial sync (POST).
        /// Requires the source to have been created with full sync first.
        /// </summary>
        /// <param name="request">The schema update request.</param>
        Task<CustomDataSourceModel> UpdateSchemaPartialAsync(UpdateSchemaRequest request);

        /// <summary>
        /// Updates an existing schema using full sync (PUT).
        /// WARNING: This will delete all existing data in the source.
        /// </summary>
        /// <param name="request">The schema update request.</param>
        Task<CustomDataSourceModel> UpdateSchemaFullAsync(CreateSchemaRequest request);

        /// <summary>
        /// Deletes a custom data source from Optimizely Graph.
        /// </summary>
        /// <param name="sourceId">The source ID to delete.</param>
        /// <param name="languages">Optional languages to delete. If null, all languages are deleted.</param>
        Task<bool> DeleteSourceAsync(string sourceId, List<string>? languages = null);

        /// <summary>
        /// Checks if a source exists in Optimizely Graph.
        /// </summary>
        /// <param name="sourceId">The source ID to check.</param>
        Task<bool> SourceExistsAsync(string sourceId);
    }
}
