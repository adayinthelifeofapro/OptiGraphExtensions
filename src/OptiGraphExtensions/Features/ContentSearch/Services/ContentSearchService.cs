using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.ContentSearch.Models;
using OptiGraphExtensions.Features.ContentSearch.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.ContentSearch.Services;

/// <summary>
/// Service for searching content in Optimizely Graph via GraphQL
/// </summary>
public class ContentSearchService : IContentSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IOptiGraphConfigurationService _configurationService;
    private readonly IGraphConfigurationValidator _graphConfigurationValidator;
    private readonly JsonSerializerOptions _jsonOptions;

    public ContentSearchService(
        HttpClient httpClient,
        IOptiGraphConfigurationService configurationService,
        IGraphConfigurationValidator graphConfigurationValidator)
    {
        _httpClient = httpClient;
        _configurationService = configurationService;
        _graphConfigurationValidator = graphConfigurationValidator;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IList<ContentSearchResult>> SearchContentAsync(
        string searchText,
        string? contentType = null,
        string? language = null,
        int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
            return new List<ContentSearchResult>();

        var (gatewayUrl, appKey, secret) = GetAndValidateGraphConfiguration();

        var graphQuery = BuildSearchQuery(contentType);
        var variables = BuildVariables(searchText, contentType, language, limit);

        var request = new ContentSearchGraphRequest
        {
            Query = graphQuery,
            Variables = variables
        };

        var response = await ExecuteGraphQueryAsync(gatewayUrl, appKey, secret, request);

        return MapToSearchResults(response);
    }

    public async Task<IList<string>> GetAvailableContentTypesAsync()
    {
        var (gatewayUrl, appKey, secret) = GetAndValidateGraphConfiguration();

        var query = @"
            query GetContentTypes {
                Content(limit: 0) {
                    facets {
                        ContentType(limit: 100) {
                            name
                            count
                        }
                    }
                }
            }";

        var request = new ContentSearchGraphRequest { Query = query };

        var response = await ExecuteContentTypeFacetsQueryAsync(gatewayUrl, appKey, secret, request);

        return response;
    }

    private string BuildSearchQuery(string? contentType)
    {
        // Build the where clause combining fulltext search and optional content type filter
        // Optimizely Graph uses _fulltext for text search
        var contentTypeFilter = string.IsNullOrEmpty(contentType)
            ? ""
            : $", ContentType: {{ eq: \"{contentType}\" }}";

        return $@"
            query SearchContent($searchText: String!, $limit: Int!, $locale: [Locales]) {{
                Content(
                    where: {{
                        _fulltext: {{ match: $searchText }}
                        {contentTypeFilter}
                    }}
                    limit: $limit
                    locale: $locale
                    orderBy: {{ _ranking: SEMANTIC }}
                ) {{
                    items {{
                        _score
                        ContentLink {{
                            GuidValue
                        }}
                        Name
                        RelativePath
                        ContentType
                        Language {{
                            Name
                        }}
                    }}
                    total
                }}
            }}";
    }

    private Dictionary<string, object> BuildVariables(
        string searchText,
        string? contentType,
        string? language,
        int limit)
    {
        var variables = new Dictionary<string, object>
        {
            ["searchText"] = searchText,
            ["limit"] = Math.Min(limit, 10)
        };

        if (!string.IsNullOrEmpty(language))
        {
            // Optimizely Graph locale format uses lowercase (e.g., "en", "sv")
            variables["locale"] = new[] { language.ToLowerInvariant() };
        }

        return variables;
    }

    private async Task<ContentSearchGraphResponse?> ExecuteGraphQueryAsync(
        string gatewayUrl,
        string appKey,
        string secret,
        ContentSearchGraphRequest request)
    {
        var authHeader = $"{appKey}:{secret}".Base64Encode();
        var graphqlEndpoint = $"{gatewayUrl}/content/v2";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, graphqlEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Graph search failed: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var graphResponse = JsonSerializer.Deserialize<ContentSearchGraphResponse>(responseContent, _jsonOptions);

        // Check for GraphQL errors in the response
        if (graphResponse?.Errors?.Any() == true)
        {
            var errorMessages = string.Join(", ", graphResponse.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"GraphQL errors: {errorMessages}");
        }

        return graphResponse;
    }

    private async Task<IList<string>> ExecuteContentTypeFacetsQueryAsync(
        string gatewayUrl,
        string appKey,
        string secret,
        ContentSearchGraphRequest request)
    {
        var authHeader = $"{appKey}:{secret}".Base64Encode();
        var graphqlEndpoint = $"{gatewayUrl}/content/v2";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, graphqlEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            // Return empty list on error for content types - not critical
            return new List<string>();
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var facetsResponse = JsonSerializer.Deserialize<ContentTypeFacetsResponse>(responseContent, _jsonOptions);

        var contentTypes = facetsResponse?.Data?.Content?.Facets?.ContentType?
            .Where(ct => !string.IsNullOrEmpty(ct.Name))
            .Select(ct => ct.Name!)
            .OrderBy(name => name)
            .ToList();

        return contentTypes ?? new List<string>();
    }

    private IList<ContentSearchResult> MapToSearchResults(ContentSearchGraphResponse? response)
    {
        if (response?.Data?.Content?.Items == null)
            return new List<ContentSearchResult>();

        return response.Data.Content.Items
            .Where(item => !string.IsNullOrEmpty(item.ContentLink?.GuidValue))
            .Select(item => new ContentSearchResult
            {
                GuidValue = item.ContentLink!.GuidValue!,
                Name = item.Name ?? "Untitled",
                Url = item.RelativePath ?? string.Empty,
                ContentType = item.ContentType?.Count >= 2 
                    ? item.ContentType[item.ContentType.Count - 2] 
                    : (item.ContentType?.FirstOrDefault() ?? "Unknown"),
                Language = item.Language?.Name ?? string.Empty
            })
            .ToList();
    }

    private (string gatewayUrl, string appKey, string secret) GetAndValidateGraphConfiguration()
    {
        var gatewayUrl = _configurationService.GetGatewayUrl();
        var appKey = _configurationService.GetAppKey();
        var secret = _configurationService.GetSecret();

        _graphConfigurationValidator.ValidateConfiguration(gatewayUrl, appKey, secret);

        return (gatewayUrl, appKey, secret);
    }
}
