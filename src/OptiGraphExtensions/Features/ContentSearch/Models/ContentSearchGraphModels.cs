using System.Text.Json.Serialization;

namespace OptiGraphExtensions.Features.ContentSearch.Models;

/// <summary>
/// GraphQL request for content search
/// </summary>
public class ContentSearchGraphRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("variables")]
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// GraphQL response wrapper for content search
/// </summary>
public class ContentSearchGraphResponse
{
    [JsonPropertyName("data")]
    public ContentSearchData? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQLError>? Errors { get; set; }
}

/// <summary>
/// GraphQL error response
/// </summary>
public class GraphQLError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Data wrapper in GraphQL response
/// </summary>
public class ContentSearchData
{
    [JsonPropertyName("Content")]
    public ContentSearchResultSet? Content { get; set; }
}

/// <summary>
/// Content search result set with items and total
/// </summary>
public class ContentSearchResultSet
{
    [JsonPropertyName("items")]
    public List<ContentSearchItem>? Items { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }
}

/// <summary>
/// Individual content item from Graph search
/// </summary>
public class ContentSearchItem
{
    [JsonPropertyName("_score")]
    public decimal? Score { get; set; }

    [JsonPropertyName("ContentLink")]
    public ContentLinkInfo? ContentLink { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("RelativePath")]
    public string? RelativePath { get; set; }

    [JsonPropertyName("ContentType")]
    public List<string>? ContentType { get; set; }

    [JsonPropertyName("Language")]
    public GraphLanguageInfo? Language { get; set; }
}

/// <summary>
/// ContentLink information containing GuidValue
/// </summary>
public class ContentLinkInfo
{
    [JsonPropertyName("GuidValue")]
    public string? GuidValue { get; set; }
}

/// <summary>
/// Language information from Graph response
/// </summary>
public class GraphLanguageInfo
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }
}

/// <summary>
/// GraphQL response for content type facets
/// </summary>
public class ContentTypeFacetsResponse
{
    [JsonPropertyName("data")]
    public ContentTypeFacetsData? Data { get; set; }
}

/// <summary>
/// Data wrapper for content type facets
/// </summary>
public class ContentTypeFacetsData
{
    [JsonPropertyName("Content")]
    public ContentTypeFacetsContent? Content { get; set; }
}

/// <summary>
/// Content wrapper containing facets
/// </summary>
public class ContentTypeFacetsContent
{
    [JsonPropertyName("facets")]
    public ContentTypeFacets? Facets { get; set; }
}

/// <summary>
/// Facets containing content type information
/// </summary>
public class ContentTypeFacets
{
    [JsonPropertyName("ContentType")]
    public List<ContentTypeFacetItem>? ContentType { get; set; }
}

/// <summary>
/// Individual content type facet item
/// </summary>
public class ContentTypeFacetItem
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }
}
