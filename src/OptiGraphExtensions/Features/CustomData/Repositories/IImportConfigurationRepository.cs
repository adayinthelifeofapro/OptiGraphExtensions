using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.CustomData.Repositories
{
    /// <summary>
    /// Repository interface for managing import configurations.
    /// </summary>
    public interface IImportConfigurationRepository
    {
        /// <summary>
        /// Gets all import configurations.
        /// </summary>
        Task<IEnumerable<ImportConfiguration>> GetAllAsync();

        /// <summary>
        /// Gets import configurations for a specific source ID.
        /// </summary>
        Task<IEnumerable<ImportConfiguration>> GetBySourceIdAsync(string sourceId);

        /// <summary>
        /// Gets an import configuration by its ID.
        /// </summary>
        Task<ImportConfiguration?> GetByIdAsync(Guid id);

        /// <summary>
        /// Creates a new import configuration.
        /// </summary>
        Task<ImportConfiguration> CreateAsync(ImportConfiguration config);

        /// <summary>
        /// Updates an existing import configuration.
        /// </summary>
        Task<ImportConfiguration> UpdateAsync(ImportConfiguration config);

        /// <summary>
        /// Deletes an import configuration.
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Deletes all import configurations for a specific source ID.
        /// Also deletes associated execution history records.
        /// </summary>
        /// <param name="sourceId">The source ID to delete configurations for.</param>
        /// <returns>The number of configurations deleted.</returns>
        Task<int> DeleteBySourceIdAsync(string sourceId);

        /// <summary>
        /// Checks if an import configuration exists.
        /// </summary>
        Task<bool> ExistsAsync(Guid id);
    }
}
