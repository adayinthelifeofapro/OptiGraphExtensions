using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for managing custom data items in Optimizely Graph.
    /// </summary>
    public interface ICustomDataService
    {
        /// <summary>
        /// Gets all data items for a custom data source via GraphQL query.
        /// </summary>
        /// <param name="sourceId">The source ID to get items from.</param>
        /// <param name="contentType">The content type to query.</param>
        /// <param name="properties">The property names to retrieve.</param>
        /// <param name="language">Optional language filter.</param>
        /// <param name="limit">Maximum items to retrieve (default 100).</param>
        Task<IEnumerable<CustomDataItemModel>> GetAllItemsAsync(
            string sourceId,
            string contentType,
            IEnumerable<string> properties,
            string? language = null,
            int limit = 100);

        /// <summary>
        /// Gets a specific data item by its ID.
        /// </summary>
        /// <param name="sourceId">The source ID.</param>
        /// <param name="itemId">The item ID.</param>
        Task<CustomDataItemModel?> GetItemByIdAsync(string sourceId, string itemId);

        /// <summary>
        /// Syncs data items to Optimizely Graph using NdJSON format.
        /// </summary>
        /// <param name="request">The sync request containing items to sync.</param>
        /// <returns>The API response content for debugging purposes.</returns>
        Task<string> SyncDataAsync(SyncDataRequest request);

        /// <summary>
        /// Syncs a single data item to Optimizely Graph.
        /// </summary>
        /// <param name="sourceId">The source ID.</param>
        /// <param name="item">The item to sync.</param>
        /// <param name="jobId">Optional job ID for tracking.</param>
        /// <returns>The API response content for debugging purposes.</returns>
        Task<string> SyncSingleItemAsync(string sourceId, CustomDataItemModel item, string? jobId = null);

        /// <summary>
        /// Deletes data from a custom data source.
        /// </summary>
        /// <param name="sourceId">The source ID.</param>
        /// <param name="itemIds">Optional specific item IDs to delete.</param>
        /// <param name="languages">Optional languages to delete.</param>
        Task<bool> DeleteDataAsync(string sourceId, List<string>? itemIds = null, List<string>? languages = null);

        /// <summary>
        /// Deletes all data from a custom data source.
        /// </summary>
        /// <param name="sourceId">The source ID.</param>
        Task<bool> ClearAllDataAsync(string sourceId);
    }
}
