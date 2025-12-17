namespace OptiGraphExtensions.Features.ContentSearch.Models;

/// <summary>
/// Represents a content item search result from Optimizely Graph
/// </summary>
public class ContentSearchResult
{
    /// <summary>
    /// The ContentLink.GuidValue - used as TargetKey for pinned results
    /// </summary>
    public string GuidValue { get; set; } = string.Empty;

    /// <summary>
    /// Content name for display
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Relative URL/Path for display
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Content type name for filtering/display
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Language of the content
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Display string combining Name and URL
    /// </summary>
    public string DisplayText => string.IsNullOrEmpty(Url)
        ? Name
        : $"{Name} ({Url})";
}
