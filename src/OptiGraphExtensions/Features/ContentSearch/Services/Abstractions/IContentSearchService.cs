using OptiGraphExtensions.Features.ContentSearch.Models;

namespace OptiGraphExtensions.Features.ContentSearch.Services.Abstractions;

/// <summary>
/// Service for searching content in Optimizely Graph
/// </summary>
public interface IContentSearchService
{
    /// <summary>
    /// Searches for content items in Optimizely Graph
    /// </summary>
    /// <param name="searchText">The search text to query (minimum 2 characters)</param>
    /// <param name="contentType">Optional content type filter</param>
    /// <param name="language">Optional language filter</param>
    /// <param name="limit">Maximum number of results (default: 10, max: 10)</param>
    /// <returns>List of content search results</returns>
    Task<IList<ContentSearchResult>> SearchContentAsync(
        string searchText,
        string? contentType = null,
        string? language = null,
        int limit = 10);

    /// <summary>
    /// Gets available content types from Optimizely Graph for filtering
    /// </summary>
    /// <returns>List of available content type names</returns>
    Task<IList<string>> GetAvailableContentTypesAsync();
}
