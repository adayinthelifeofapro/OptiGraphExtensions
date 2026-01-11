using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.QueryLibrary.Models;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.QueryLibrary.Services
{
    /// <summary>
    /// Service for executing queries against Optimizely Graph.
    /// </summary>
    public class QueryExecutionService : IQueryExecutionService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly IGraphConfigurationValidator _graphConfigurationValidator;
        private readonly IQueryBuilderService _queryBuilderService;
        private readonly IRawQueryService _rawQueryService;
        private readonly JsonSerializerOptions _jsonOptions;

        public QueryExecutionService(
            HttpClient httpClient,
            IOptiGraphConfigurationService configurationService,
            IGraphConfigurationValidator graphConfigurationValidator,
            IQueryBuilderService queryBuilderService,
            IRawQueryService rawQueryService)
        {
            _httpClient = httpClient;
            _configurationService = configurationService;
            _graphConfigurationValidator = graphConfigurationValidator;
            _queryBuilderService = queryBuilderService;
            _rawQueryService = rawQueryService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<QueryExecutionResult> ExecuteQueryAsync(QueryExecutionRequest request)
        {
            try
            {
                string query;
                Dictionary<string, object> variables;

                if (request.QueryType == QueryType.Raw)
                {
                    query = request.RawGraphQuery ?? throw new ArgumentException("RawGraphQuery is required");
                    variables = request.QueryVariables ?? new Dictionary<string, object>();

                    // Inject pagination support if needed
                    (query, variables) = _rawQueryService.InjectPaginationSupport(
                        query, variables, request.PageSize, request.Cursor);
                }
                else
                {
                    query = _queryBuilderService.BuildGraphQLQuery(request);
                    variables = _queryBuilderService.BuildVariables(request);
                }

                return await ExecuteRawQueryAsync(query, variables);
            }
            catch (Exception ex)
            {
                return new QueryExecutionResult
                {
                    ErrorMessage = $"Query execution failed: {ex.Message}"
                };
            }
        }

        public async IAsyncEnumerable<QueryExecutionResult> ExecuteQueryWithPaginationAsync(
            QueryExecutionRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string? cursor = null;
            var hasMore = true;

            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                request.Cursor = cursor;
                var result = await ExecuteQueryAsync(request);

                if (!result.IsSuccess)
                {
                    yield return result;
                    yield break;
                }

                yield return result;

                cursor = result.NextCursor;
                hasMore = result.HasMore && !string.IsNullOrEmpty(cursor);
            }
        }

        public async Task<int> GetQueryCountAsync(QueryExecutionRequest request)
        {
            // Execute query with limit 0 to just get count
            var countRequest = new QueryExecutionRequest
            {
                QueryType = request.QueryType,
                ContentType = request.ContentType,
                SelectedFields = new List<string> { "Name" }, // Minimal field selection
                Filters = request.Filters,
                Language = request.Language,
                RawGraphQuery = request.RawGraphQuery,
                QueryVariables = request.QueryVariables,
                PageSize = 1 // Minimal page size
            };

            var result = await ExecuteQueryAsync(countRequest);
            return result.TotalCount;
        }

        public async Task<QueryExecutionResult> ExecuteRawQueryAsync(
            string query,
            Dictionary<string, object>? variables = null)
        {
            try
            {
                var (gatewayUrl, appKey, secret) = GetAndValidateGraphConfiguration();

                var request = new
                {
                    query,
                    variables = variables ?? new Dictionary<string, object>()
                };

                var authHeader = $"{appKey}:{secret}".Base64Encode();
                var graphqlEndpoint = $"{gatewayUrl}/content/v2";

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, graphqlEndpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                httpRequest.Content = new StringContent(
                    JsonSerializer.Serialize(request, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new QueryExecutionResult
                    {
                        ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}"
                    };
                }

                return ParseGraphQLResponse(responseContent);
            }
            catch (Exception ex)
            {
                return new QueryExecutionResult
                {
                    ErrorMessage = $"Query execution failed: {ex.Message}"
                };
            }
        }

        private QueryExecutionResult ParseGraphQLResponse(string responseContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                // Check for GraphQL errors
                if (root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                {
                    var errorMessages = new List<string>();
                    foreach (var error in errors.EnumerateArray())
                    {
                        if (error.TryGetProperty("message", out var msg))
                        {
                            errorMessages.Add(msg.GetString() ?? "Unknown error");
                        }
                    }

                    return new QueryExecutionResult
                    {
                        ErrorMessage = string.Join("; ", errorMessages)
                    };
                }

                // Parse data
                if (!root.TryGetProperty("data", out var data))
                {
                    return new QueryExecutionResult
                    {
                        ErrorMessage = "No data in response"
                    };
                }

                // Find the first property in data (the content type query result)
                JsonElement? contentResult = null;
                foreach (var prop in data.EnumerateObject())
                {
                    contentResult = prop.Value;
                    break;
                }

                if (!contentResult.HasValue)
                {
                    return new QueryExecutionResult
                    {
                        ErrorMessage = "No content type result in response"
                    };
                }

                var result = new QueryExecutionResult();

                // Parse items
                if (contentResult.Value.TryGetProperty("items", out var items))
                {
                    result.Rows = ParseItems(items, out var columns);
                    result.Columns = columns;
                }

                // Parse pagination info
                if (contentResult.Value.TryGetProperty("cursor", out var cursor) &&
                    cursor.ValueKind == JsonValueKind.String)
                {
                    result.NextCursor = cursor.GetString();
                    result.HasMore = !string.IsNullOrEmpty(result.NextCursor);
                }

                if (contentResult.Value.TryGetProperty("total", out var total) &&
                    total.ValueKind == JsonValueKind.Number)
                {
                    result.TotalCount = total.GetInt32();
                }

                return result;
            }
            catch (JsonException ex)
            {
                return new QueryExecutionResult
                {
                    ErrorMessage = $"Failed to parse response: {ex.Message}"
                };
            }
        }

        private List<Dictionary<string, object?>> ParseItems(JsonElement items, out List<string> columns)
        {
            var rows = new List<Dictionary<string, object?>>();
            var columnSet = new HashSet<string>();

            foreach (var item in items.EnumerateArray())
            {
                var row = FlattenJsonElement(item, "");
                rows.Add(row);

                foreach (var key in row.Keys)
                {
                    columnSet.Add(key);
                }
            }

            // Sort columns for consistent ordering
            columns = columnSet.OrderBy(c => c).ToList();

            return rows;
        }

        private Dictionary<string, object?> FlattenJsonElement(JsonElement element, string prefix)
        {
            var result = new Dictionary<string, object?>();

            if (element.ValueKind != JsonValueKind.Object)
            {
                result[prefix] = GetJsonValue(element);
                return result;
            }

            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix)
                    ? property.Name
                    : $"{prefix}.{property.Name}";

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    // Recursively flatten nested objects
                    var nested = FlattenJsonElement(property.Value, key);
                    foreach (var (nestedKey, nestedValue) in nested)
                    {
                        result[nestedKey] = nestedValue;
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // Convert arrays to comma-separated strings
                    var values = new List<string>();
                    foreach (var arrayItem in property.Value.EnumerateArray())
                    {
                        var val = GetJsonValue(arrayItem);
                        if (val != null)
                        {
                            values.Add(val.ToString() ?? "");
                        }
                    }
                    result[key] = string.Join(", ", values);
                }
                else
                {
                    result[key] = GetJsonValue(property.Value);
                }
            }

            return result;
        }

        private object? GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => element.GetRawText()
            };
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
}
