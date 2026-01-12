using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for building and parsing NdJSON (newline-delimited JSON) payloads
    /// for Optimizely Graph data synchronization.
    /// </summary>
    public interface INdJsonBuilderService
    {
        /// <summary>
        /// Builds an NdJSON payload from a collection of data items.
        /// Each item produces two lines: an action line and a data line.
        /// </summary>
        /// <param name="items">The items to convert to NdJSON.</param>
        /// <returns>NdJSON formatted string.</returns>
        string BuildNdJson(IEnumerable<CustomDataItemModel> items);

        /// <summary>
        /// Parses an NdJSON payload into data items.
        /// </summary>
        /// <param name="ndJson">The NdJSON string to parse.</param>
        /// <returns>Collection of parsed data items.</returns>
        IEnumerable<CustomDataItemModel> ParseNdJson(string ndJson);

        /// <summary>
        /// Builds an action line for an index operation.
        /// </summary>
        /// <param name="itemId">The item ID.</param>
        /// <param name="languageRouting">Optional language routing value.</param>
        /// <returns>JSON action line string.</returns>
        string BuildIndexActionLine(string itemId, string? languageRouting = null);

        /// <summary>
        /// Builds an action line for a delete operation.
        /// </summary>
        /// <param name="itemId">The item ID to delete.</param>
        /// <returns>JSON action line string.</returns>
        string BuildDeleteActionLine(string itemId);

        /// <summary>
        /// Validates NdJSON format.
        /// </summary>
        /// <param name="ndJson">The NdJSON string to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool IsValidNdJson(string ndJson);

        /// <summary>
        /// Gets the number of items in an NdJSON payload.
        /// </summary>
        /// <param name="ndJson">The NdJSON string.</param>
        /// <returns>Number of items.</returns>
        int GetItemCount(string ndJson);
    }
}
