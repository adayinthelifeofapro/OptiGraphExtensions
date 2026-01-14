using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Service for managing custom data items in Optimizely Graph.
    /// </summary>
    public class CustomDataService : ICustomDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly IGraphConfigurationValidator _graphConfigurationValidator;
        private readonly INdJsonBuilderService _ndJsonBuilderService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public CustomDataService(
            HttpClient httpClient,
            IOptiGraphConfigurationService configurationService,
            IGraphConfigurationValidator graphConfigurationValidator,
            INdJsonBuilderService ndJsonBuilderService)
        {
            _httpClient = httpClient;
            _configurationService = configurationService;
            _graphConfigurationValidator = graphConfigurationValidator;
            _ndJsonBuilderService = ndJsonBuilderService;
        }

        public async Task<IEnumerable<CustomDataItemModel>> GetAllItemsAsync(
            string sourceId,
            string contentType,
            IEnumerable<string> properties,
            string? language = null,
            int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return Enumerable.Empty<CustomDataItemModel>();
            }

            var propertyList = properties.ToList();
            if (!propertyList.Any())
            {
                return Enumerable.Empty<CustomDataItemModel>();
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var graphqlEndpoint = $"{gatewayUrl.TrimEnd('/')}/content/v2";

            // Build GraphQL query with variables - custom data uses source-prefixed locale type
            var query = BuildGraphQLQuery(sourceId, contentType, propertyList);
            var variables = BuildQueryVariables(limit, language);

            var requestBody = new
            {
                query = query,
                variables = variables
            };

            var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, graphqlEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                // Log the query for debugging
                Console.WriteLine($"GraphQL Request:\n{requestJson}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log the response for debugging
                Console.WriteLine($"GraphQL Response ({response.StatusCode}):\n{responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new GraphSyncException(
                        $"Error querying data from Optimizely Graph: {response.StatusCode} - {responseContent}");
                }

                return ParseGraphQLResponse(responseContent, contentType, propertyList);
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error querying data: {ex.Message}", ex);
            }
        }

        public async Task<(IEnumerable<CustomDataItemModel> Items, string DebugInfo)> GetAllItemsWithDebugAsync(
            string sourceId,
            string contentType,
            IEnumerable<string> properties,
            string? language = null,
            int limit = 100)
        {
            var debugInfo = new StringBuilder();
            const int pageSize = 100; // Graph API maximum per request

            if (string.IsNullOrWhiteSpace(contentType))
            {
                return (Enumerable.Empty<CustomDataItemModel>(), "Content type is empty");
            }

            var propertyList = properties.ToList();
            if (!propertyList.Any())
            {
                return (Enumerable.Empty<CustomDataItemModel>(), "No properties specified");
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var graphqlEndpoint = $"{gatewayUrl.TrimEnd('/')}/content/v2";

            var allItems = new List<CustomDataItemModel>();
            string? cursor = null;
            int totalItems = 0;
            int pageNumber = 1;

            debugInfo.AppendLine($"Endpoint: {graphqlEndpoint}");

            try
            {
                do
                {
                    // Build query - use cursor parameter after first page
                    var useCursor = cursor != null;
                    var query = BuildGraphQLQuery(sourceId, contentType, propertyList, useCursor);
                    var variables = BuildQueryVariables(pageSize, language, cursor);

                    var requestBody = new
                    {
                        query = query,
                        variables = variables
                    };

                    var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);

                    if (pageNumber == 1)
                    {
                        debugInfo.AppendLine($"Query:\n{query}");
                    }
                    debugInfo.AppendLine($"\nPage {pageNumber} - Variables: {JsonSerializer.Serialize(variables, JsonOptions)}");

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, graphqlEndpoint);
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                    httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    debugInfo.AppendLine($"Page {pageNumber} Response Status: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        debugInfo.AppendLine($"Response Body:\n{responseContent}");
                        return (allItems, debugInfo.ToString());
                    }

                    var (items, nextCursor, total) = ParseGraphQLResponseWithPagination(responseContent, contentType, propertyList);
                    var itemsList = items.ToList();

                    allItems.AddRange(itemsList);
                    cursor = nextCursor;
                    totalItems = total;

                    debugInfo.AppendLine($"Page {pageNumber}: Retrieved {itemsList.Count} items (Total in Graph: {total})");

                    pageNumber++;

                    // Stop if we've retrieved all items or reached the requested limit
                    if (allItems.Count >= totalItems || allItems.Count >= limit || itemsList.Count == 0 || string.IsNullOrEmpty(cursor))
                    {
                        break;
                    }

                } while (true);

                debugInfo.AppendLine($"\nTotal retrieved: {allItems.Count} of {totalItems} items in {pageNumber - 1} page(s)");

                // Apply limit if specified
                if (limit > 0 && allItems.Count > limit)
                {
                    allItems = allItems.Take(limit).ToList();
                    debugInfo.AppendLine($"Applied limit: returning {limit} items");
                }

                return (allItems, debugInfo.ToString());
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"\nException: {ex.Message}");
                return (allItems, debugInfo.ToString());
            }
        }

        private static string BuildGraphQLQuery(string sourceId, string contentType, List<string> properties, bool useCursor = false)
        {
            // Build the properties selection
            var propsSelection = string.Join("\n                        ", properties);

            // Custom data sources use source-prefixed locale type (e.g., "test_Locales" instead of "Locales")
            // Custom data types don't have _locale field like CMS content
            var localeType = $"{sourceId}_Locales";

            // Add cursor parameter if pagination is needed
            var cursorParam = useCursor ? ", $cursor: String" : "";
            var cursorArg = useCursor ? ", cursor: $cursor" : "";

            return $@"
            query GetCustomData($limit: Int!, $locale: [{localeType}]{cursorParam}) {{
                {contentType}(limit: $limit, locale: $locale{cursorArg}) {{
                    items {{
                        _id
                        {propsSelection}
                    }}
                    cursor
                    total
                }}
            }}";
        }

        private static Dictionary<string, object> BuildQueryVariables(int limit, string? language, string? cursor = null)
        {
            var variables = new Dictionary<string, object>
            {
                ["limit"] = limit
            };

            if (!string.IsNullOrEmpty(language))
            {
                // Optimizely Graph locale format uses lowercase (e.g., "en", "sv")
                variables["locale"] = new[] { language.ToLowerInvariant() };
            }

            if (!string.IsNullOrEmpty(cursor))
            {
                variables["cursor"] = cursor;
            }

            return variables;
        }

        private static (IEnumerable<CustomDataItemModel> Items, string? Cursor, int Total) ParseGraphQLResponseWithPagination(
            string responseContent,
            string contentType,
            List<string> properties)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                // Check for errors
                if (root.TryGetProperty("errors", out var errorsElement) &&
                    errorsElement.ValueKind == JsonValueKind.Array &&
                    errorsElement.GetArrayLength() > 0)
                {
                    // Extract error messages for debugging
                    var errorMessages = errorsElement.EnumerateArray()
                        .Select(e => e.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error")
                        .Where(m => !string.IsNullOrEmpty(m));
                    var combinedErrors = string.Join("; ", errorMessages);
                    throw new GraphSyncException($"GraphQL query error: {combinedErrors}");
                }

                // Navigate to data -> contentType -> items
                if (!root.TryGetProperty("data", out var dataElement))
                {
                    throw new GraphSyncException($"GraphQL response missing 'data' element. Response: {responseContent}");
                }

                if (!dataElement.TryGetProperty(contentType, out var contentTypeElement))
                {
                    // The content type doesn't exist in the response - list available types
                    var availableTypes = dataElement.EnumerateObject().Select(p => p.Name).ToList();
                    throw new GraphSyncException($"Content type '{contentType}' not found in GraphQL response. Available types: [{string.Join(", ", availableTypes)}]. Full response: {responseContent}");
                }

                // Get cursor and total for pagination
                string? cursor = null;
                int total = 0;

                if (contentTypeElement.TryGetProperty("cursor", out var cursorElement) &&
                    cursorElement.ValueKind == JsonValueKind.String)
                {
                    cursor = cursorElement.GetString();
                }

                if (contentTypeElement.TryGetProperty("total", out var totalElement) &&
                    totalElement.ValueKind == JsonValueKind.Number)
                {
                    total = totalElement.GetInt32();
                }

                if (!contentTypeElement.TryGetProperty("items", out var itemsElement) ||
                    itemsElement.ValueKind != JsonValueKind.Array)
                {
                    throw new GraphSyncException($"No 'items' array found for content type '{contentType}'. Response: {responseContent}");
                }

                var results = new List<CustomDataItemModel>();

                foreach (var item in itemsElement.EnumerateArray())
                {
                    var dataItem = new CustomDataItemModel
                    {
                        ContentType = contentType,
                        Properties = new Dictionary<string, object?>()
                    };

                    // Extract _id directly (for custom data)
                    if (item.TryGetProperty("_id", out var idElement))
                    {
                        dataItem.Id = idElement.GetString() ?? Guid.NewGuid().ToString();
                    }
                    else
                    {
                        dataItem.Id = Guid.NewGuid().ToString();
                    }

                    // Try to get language from _locale if available
                    if (item.TryGetProperty("_locale", out var localeElement))
                    {
                        dataItem.LanguageRouting = localeElement.GetString();
                    }

                    // Extract each property
                    foreach (var propName in properties)
                    {
                        if (item.TryGetProperty(propName, out var propValue))
                        {
                            dataItem.Properties[propName] = ConvertJsonElement(propValue);
                        }
                    }

                    results.Add(dataItem);
                }

                return (results, cursor, total);
            }
            catch (JsonException)
            {
                return (Enumerable.Empty<CustomDataItemModel>(), null, 0);
            }
        }

        private static IEnumerable<CustomDataItemModel> ParseGraphQLResponse(
            string responseContent,
            string contentType,
            List<string> properties)
        {
            var (items, _, _) = ParseGraphQLResponseWithPagination(responseContent, contentType, properties);
            return items;
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(ConvertJsonElement)
                    .ToList(),
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                _ => element.GetRawText()
            };
        }

        public async Task<CustomDataItemModel?> GetItemByIdAsync(string sourceId, string itemId)
        {
            // Note: Similar to GetAllItemsAsync, direct item retrieval isn't available via REST.
            // Would require GraphQL query with filter on _id.
            return null;
        }

        public async Task<string> SyncDataAsync(SyncDataRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SourceId))
            {
                throw new ArgumentException("Source ID is required", nameof(request));
            }

            if (request.Items == null || !request.Items.Any())
            {
                throw new ArgumentException("At least one item is required", nameof(request));
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildDataApiUrl(gatewayUrl, request.SourceId);

            // Build NdJSON payload
            var ndJsonContent = _ndJsonBuilderService.BuildNdJson(request.Items);

            // Data sync uses POST method per Optimizely Graph documentation
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, apiUrl, authHeader);
            httpRequest.Content = new StringContent(ndJsonContent, Encoding.UTF8, "text/plain");

            // Add optional job ID header for tracking
            if (!string.IsNullOrWhiteSpace(request.JobId))
            {
                httpRequest.Headers.TryAddWithoutValidation("og-job-id", request.JobId);
            }

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Build debug info
                var debugInfo = $"API URL: {apiUrl}\n";
                debugInfo += $"Status: {response.StatusCode}\n";
                debugInfo += $"Response: {(string.IsNullOrEmpty(responseContent) ? "(empty)" : responseContent)}";

                if (!response.IsSuccessStatusCode)
                {
                    throw new GraphSyncException(
                        $"Error syncing data to Optimizely Graph: {response.StatusCode} - {responseContent}");
                }

                // Check if the response indicates any issues even with success status
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                            errors.ValueKind == JsonValueKind.Array &&
                            errors.GetArrayLength() > 0)
                        {
                            throw new GraphSyncException($"Sync returned errors: {responseContent}");
                        }
                    }
                    catch (JsonException)
                    {
                        // Not JSON, that's fine
                    }
                }

                return debugInfo;
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error syncing data: {ex.Message}", ex);
            }
        }

        public async Task<string> SyncSingleItemAsync(string sourceId, CustomDataItemModel item, string? jobId = null)
        {
            var request = new SyncDataRequest
            {
                SourceId = sourceId,
                Items = new List<CustomDataItemModel> { item },
                JobId = jobId
            };

            return await SyncDataAsync(request);
        }

        public async Task<bool> DeleteDataAsync(string sourceId, List<string>? itemIds = null, List<string>? languages = null)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source ID is required", nameof(sourceId));
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildDataApiUrl(gatewayUrl, sourceId);

            // Add language parameters if specified
            if (languages != null && languages.Any())
            {
                var langParams = string.Join("&", languages.Select(l => $"languages={Uri.EscapeDataString(l)}"));
                apiUrl += $"&{langParams}";
            }

            // If specific item IDs are provided, use NdJSON delete actions
            if (itemIds != null && itemIds.Any())
            {
                return await DeleteSpecificItemsAsync(sourceId, itemIds);
            }

            // Otherwise, delete all data (or by language filter)
            using var request = CreateAuthenticatedRequest(HttpMethod.Delete, apiUrl, authHeader);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new GraphSyncException(
                        $"Error deleting data from Optimizely Graph: {response.StatusCode} - {errorContent}");
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error deleting data: {ex.Message}", ex);
            }
        }

        public async Task<bool> ClearAllDataAsync(string sourceId)
        {
            return await DeleteDataAsync(sourceId);
        }

        private async Task<bool> DeleteSpecificItemsAsync(string sourceId, List<string> itemIds)
        {
            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildDataApiUrl(gatewayUrl, sourceId);

            // Build NdJSON with delete actions
            var sb = new StringBuilder();
            foreach (var itemId in itemIds)
            {
                sb.AppendLine(_ndJsonBuilderService.BuildDeleteActionLine(itemId));
            }

            // Delete actions via NdJSON also use POST method
            using var request = CreateAuthenticatedRequest(HttpMethod.Post, apiUrl, authHeader);
            request.Content = new StringContent(sb.ToString(), Encoding.UTF8, "text/plain");

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new GraphSyncException(
                        $"Error deleting items from Optimizely Graph: {response.StatusCode} - {errorContent}");
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error deleting items: {ex.Message}", ex);
            }
        }

        private (string gatewayUrl, string authHeader) GetAuthenticatedConfig()
        {
            var gatewayUrl = _configurationService.GetGatewayUrl();
            var hmacKey = _configurationService.GetAppKey();
            var hmacSecret = _configurationService.GetSecret();

            _graphConfigurationValidator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret);

            var authHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
            return (gatewayUrl, authHeader);
        }

        private static string BuildDataApiUrl(string gatewayUrl, string sourceId)
        {
            gatewayUrl = gatewayUrl.TrimEnd('/');
            return $"{gatewayUrl}/api/content/v2/data?id={Uri.EscapeDataString(sourceId)}";
        }

        private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string authHeader)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            return request;
        }
    }
}
