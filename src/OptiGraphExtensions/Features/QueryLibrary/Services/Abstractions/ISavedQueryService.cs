using OptiGraphExtensions.Features.QueryLibrary.Models;

namespace OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions
{
    /// <summary>
    /// Service for managing saved queries.
    /// </summary>
    public interface ISavedQueryService
    {
        /// <summary>
        /// Gets all saved queries.
        /// </summary>
        Task<IEnumerable<SavedQueryModel>> GetAllQueriesAsync();

        /// <summary>
        /// Gets all active saved queries.
        /// </summary>
        Task<IEnumerable<SavedQueryModel>> GetActiveQueriesAsync();

        /// <summary>
        /// Gets a saved query by ID.
        /// </summary>
        Task<SavedQueryModel?> GetQueryByIdAsync(Guid id);

        /// <summary>
        /// Creates a new saved query.
        /// </summary>
        Task<SavedQueryModel> CreateQueryAsync(SavedQueryModel model, string? createdBy = null);

        /// <summary>
        /// Updates an existing saved query.
        /// </summary>
        Task<SavedQueryModel?> UpdateQueryAsync(Guid id, SavedQueryModel model, string? updatedBy = null);

        /// <summary>
        /// Deletes a saved query.
        /// </summary>
        Task<bool> DeleteQueryAsync(Guid id);

        /// <summary>
        /// Checks if a query with the given ID exists.
        /// </summary>
        Task<bool> QueryExistsAsync(Guid id);

        /// <summary>
        /// Converts a saved query to a query execution request.
        /// </summary>
        QueryExecutionRequest ToExecutionRequest(SavedQueryModel model);
    }
}
