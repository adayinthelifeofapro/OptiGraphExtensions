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
    /// Service for managing custom data source schemas in Optimizely Graph.
    /// </summary>
    public class CustomDataSchemaService : ICustomDataSchemaService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly IGraphConfigurationValidator _graphConfigurationValidator;
        private readonly ISchemaParserService _schemaParserService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public CustomDataSchemaService(
            HttpClient httpClient,
            IOptiGraphConfigurationService configurationService,
            IGraphConfigurationValidator graphConfigurationValidator,
            ISchemaParserService schemaParserService)
        {
            _httpClient = httpClient;
            _configurationService = configurationService;
            _graphConfigurationValidator = graphConfigurationValidator;
            _schemaParserService = schemaParserService;
        }

        public async Task<IEnumerable<CustomDataSourceModel>> GetAllSourcesAsync()
        {
            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildSourcesApiUrl(gatewayUrl);

            using var request = CreateAuthenticatedRequest(HttpMethod.Get, apiUrl, authHeader);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new GraphSyncException(
                        $"Error fetching sources from Optimizely Graph: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return Enumerable.Empty<CustomDataSourceModel>();
                }

                // Parse the response - API may return array directly or object with "sources" property
                var sourceInfoList = ParseSourcesResponse(responseContent);
                if (sourceInfoList == null || !sourceInfoList.Any())
                {
                    return Enumerable.Empty<CustomDataSourceModel>();
                }

                // Filter out the "default" source (CMS content) and fetch details for custom sources
                var customSources = new List<CustomDataSourceModel>();
                foreach (var sourceInfo in sourceInfoList.Where(s =>
                    !string.IsNullOrEmpty(s.Id) &&
                    s.Id != "default" &&
                    s.Id.Length <= 4))  // Custom sources have max 4 char IDs
                {
                    try
                    {
                        var sourceDetails = await GetSourceByIdAsync(sourceInfo.Id!);
                        if (sourceDetails != null)
                        {
                            customSources.Add(sourceDetails);
                        }
                    }
                    catch
                    {
                        // If we can't get details, create a basic model from the list info
                        customSources.Add(new CustomDataSourceModel
                        {
                            SourceId = sourceInfo.Id!,
                            Label = sourceInfo.Label,
                            Languages = sourceInfo.Languages ?? new List<string>()
                        });
                    }
                }

                return customSources;
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error fetching sources: {ex.Message}", ex);
            }
        }

        private List<GraphSourceInfo>? ParseSourcesResponse(string responseContent)
        {
            // Try parsing as array first (API returns array directly)
            try
            {
                var sources = JsonSerializer.Deserialize<List<GraphSourceInfo>>(responseContent, JsonOptions);
                if (sources != null)
                {
                    return sources;
                }
            }
            catch (JsonException)
            {
                // Not an array, try object format
            }

            // Try parsing as object with "sources" property
            try
            {
                var sourcesResponse = JsonSerializer.Deserialize<GraphSourcesResponse>(responseContent, JsonOptions);
                return sourcesResponse?.Sources;
            }
            catch (JsonException)
            {
                // Neither format worked
            }

            return null;
        }

        public async Task<CustomDataSourceModel?> GetSourceByIdAsync(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source ID is required", nameof(sourceId));
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildTypesApiUrl(gatewayUrl, sourceId);

            using var request = CreateAuthenticatedRequest(HttpMethod.Get, apiUrl, authHeader);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new GraphSyncException(
                        $"Error fetching schema from Optimizely Graph: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return null;
                }

                var schemaResponse = JsonSerializer.Deserialize<GraphSchemaResponse>(responseContent, JsonOptions);
                if (schemaResponse == null)
                {
                    return null;
                }

                var source = _schemaParserService.ResponseToModel(sourceId, schemaResponse);
                source.LastSyncedAt = DateTime.UtcNow;
                return source;
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error fetching schema: {ex.Message}", ex);
            }
        }

        public async Task<CustomDataSourceModel> CreateSchemaAsync(CreateSchemaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SourceId))
            {
                throw new ArgumentException("Source ID is required", nameof(request));
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildTypesApiUrl(gatewayUrl, request.SourceId);
            var jsonContent = _schemaParserService.ModelToApiJson(request);

            // Full sync uses PUT - WARNING: This deletes all existing data!
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Put, apiUrl, authHeader);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new GraphSyncException(
                        $"Error creating schema in Optimizely Graph: {response.StatusCode} - {errorContent}");
                }

                // Return the created schema
                return new CustomDataSourceModel
                {
                    SourceId = request.SourceId,
                    Label = request.Label,
                    Languages = request.Languages,
                    PropertyTypes = request.PropertyTypes,
                    ContentTypes = request.ContentTypes,
                    LastSyncedAt = DateTime.UtcNow
                };
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error creating schema: {ex.Message}", ex);
            }
        }

        public async Task<CustomDataSourceModel> UpdateSchemaPartialAsync(UpdateSchemaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SourceId))
            {
                throw new ArgumentException("Source ID is required", nameof(request));
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildTypesApiUrl(gatewayUrl, request.SourceId);
            var jsonContent = _schemaParserService.ModelToApiJson(request);

            // Partial sync uses POST - preserves existing data
            using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, apiUrl, authHeader);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new GraphSyncException(
                        $"Error updating schema in Optimizely Graph: {response.StatusCode} - {errorContent}");
                }

                return new CustomDataSourceModel
                {
                    SourceId = request.SourceId,
                    Label = request.Label,
                    Languages = request.Languages,
                    PropertyTypes = request.PropertyTypes,
                    ContentTypes = request.ContentTypes,
                    LastSyncedAt = DateTime.UtcNow
                };
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error updating schema: {ex.Message}", ex);
            }
        }

        public async Task<CustomDataSourceModel> UpdateSchemaFullAsync(CreateSchemaRequest request)
        {
            // Full sync is the same as create - uses PUT
            return await CreateSchemaAsync(request);
        }

        public async Task<bool> DeleteSourceAsync(string sourceId, List<string>? languages = null)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source ID is required", nameof(sourceId));
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();

            try
            {
                // Use the correct endpoint: DELETE /api/content/v3/sources?id={sourceId}
                // This deletes both content types and data for the source
                var deleteSourceUrl = BuildDeleteSourceApiUrl(gatewayUrl, sourceId);

                using var deleteRequest = CreateAuthenticatedRequest(HttpMethod.Delete, deleteSourceUrl, authHeader);
                var deleteResponse = await _httpClient.SendAsync(deleteRequest);

                if (!deleteResponse.IsSuccessStatusCode &&
                    deleteResponse.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                    throw new GraphSyncException(
                        $"Error deleting source from Optimizely Graph: {deleteResponse.StatusCode} - {errorContent}");
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new GraphSyncException($"Network error deleting source: {ex.Message}", ex);
            }
        }

        public async Task<bool> SourceExistsAsync(string sourceId)
        {
            var source = await GetSourceByIdAsync(sourceId);
            return source != null;
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

        private static string BuildSourcesApiUrl(string gatewayUrl)
        {
            gatewayUrl = gatewayUrl.TrimEnd('/');
            return $"{gatewayUrl}/api/content/v3/sources";
        }

        private static string BuildDeleteSourceApiUrl(string gatewayUrl, string sourceId)
        {
            gatewayUrl = gatewayUrl.TrimEnd('/');
            return $"{gatewayUrl}/api/content/v3/sources?id={Uri.EscapeDataString(sourceId)}";
        }

        private static string BuildTypesApiUrl(string gatewayUrl, string sourceId)
        {
            gatewayUrl = gatewayUrl.TrimEnd('/');
            return $"{gatewayUrl}/api/content/v3/types?id={Uri.EscapeDataString(sourceId)}";
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
