using Microsoft.AspNetCore.Http;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OptiGraphExtensions.Features.PinnedResults.Services
{
    public class PinnedResultsApiService : IPinnedResultsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly JsonSerializerOptions _jsonOptions;

        public PinnedResultsApiService(
            HttpClient httpClient, 
            IHttpContextAccessor httpContextAccessor, 
            IOptiGraphConfigurationService configurationService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configurationService = configurationService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IList<PinnedResultsCollection>> GetCollectionsAsync()
        {
            if (!IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error loading collections: {response.StatusCode} - {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            
            try
            {
                var collections = JsonSerializer.Deserialize<List<PinnedResultsCollection>>(responseContent, _jsonOptions);
                return collections?.OrderBy(c => c.Title).ToList() ?? new List<PinnedResultsCollection>();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"API returned invalid JSON. Response: {responseContent[..Math.Min(500, responseContent.Length)]}");
            }
        }

        public async Task<PinnedResultsCollection> CreateCollectionAsync(CreatePinnedResultsCollectionRequest request)
        {
            ValidateCollectionRequest(request?.Title);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("You are not authorized to create collections");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error creating collection: {response.StatusCode} - {response.ReasonPhrase}");

            var createdCollection = await response.Content.ReadFromJsonAsync<PinnedResultsCollection>();
            return createdCollection ?? throw new InvalidOperationException("Failed to deserialize created collection");
        }

        public async Task<bool> UpdateCollectionAsync(Guid id, UpdatePinnedResultsCollectionRequest request)
        {
            ValidateCollectionRequest(request?.Title);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections/{id}", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Collection not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error updating collection: {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> DeleteCollectionAsync(Guid id)
        {
            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Collection not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error deleting collection: {response.ReasonPhrase}");

            return true;
        }

        public async Task<PinnedResultsCollection?> GetCollectionByIdAsync(Guid id)
        {
            if (!IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error retrieving collection: {response.StatusCode} - {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            
            try
            {
                return JsonSerializer.Deserialize<PinnedResultsCollection>(responseContent, _jsonOptions);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Failed to parse collection data");
            }
        }

        public async Task<IList<PinnedResult>> GetPinnedResultsAsync(Guid collectionId)
        {
            if (!IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results?collectionId={collectionId}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error loading pinned results: {response.StatusCode} - {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            
            try
            {
                var pinnedResults = JsonSerializer.Deserialize<List<PinnedResult>>(responseContent, _jsonOptions);
                return pinnedResults?.OrderBy(pr => pr.Priority).ToList() ?? new List<PinnedResult>();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"API returned invalid JSON. Response: {responseContent[..Math.Min(500, responseContent.Length)]}");
            }
        }

        public async Task<bool> CreatePinnedResultAsync(CreatePinnedResultRequest request)
        {
            ValidatePinnedResultRequest(request);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("You are not authorized to create pinned results");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error creating pinned result: {response.StatusCode} - {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> UpdatePinnedResultAsync(Guid id, UpdatePinnedResultRequest request)
        {
            ValidateUpdatePinnedResultRequest(request);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results/{id}", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Pinned result not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error updating pinned result: {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> DeletePinnedResultAsync(Guid id)
        {
            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Pinned result not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error deleting pinned result: {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> SyncCollectionToOptimizelyGraphAsync(PinnedResultsCollection collection)
        {
            var graphGatewayUrl = _configurationService.GetGatewayUrl();
            var hmacKey = _configurationService.GetAppKey();
            var hmacSecret = _configurationService.GetSecret();

            ValidateGraphConfiguration(graphGatewayUrl, hmacKey, hmacSecret);

            var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
            var graphApiUrl = $"{graphGatewayUrl}/api/pinned/collections";

            var graphRequest = new
            {
                title = collection.Title,
                isActive = collection.IsActive
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, graphApiUrl);
            request.Content = JsonContent.Create(graphRequest);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeader);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Graph sync failed: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            try
            {
                var graphCollection = JsonSerializer.Deserialize<GraphCollectionResponse>(responseContent, _jsonOptions);
                if (!string.IsNullOrEmpty(graphCollection?.Id))
                {
                    await UpdateCollectionGraphIdAsync(collection.Id, graphCollection.Id);
                }
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"Failed to parse Graph collection response: {responseContent}");
            }

            return true;
        }

        public async Task<bool> SyncPinnedResultsToOptimizelyGraphAsync(Guid collectionId)
        {
            if (!IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");

            var collection = await GetCollectionByIdAsync(collectionId);
            if (collection == null || string.IsNullOrEmpty(collection.GraphCollectionId))
                throw new InvalidOperationException("Collection has no Graph collection ID. Please ensure the collection was properly synced to Graph when created.");

            var pinnedResults = await GetPinnedResultsAsync(collectionId);
            if (!pinnedResults.Any())
                throw new InvalidOperationException("No pinned results found to sync to Optimizely Graph.");

            var graphGatewayUrl = _configurationService.GetGatewayUrl();
            var hmacKey = _configurationService.GetAppKey();
            var hmacSecret = _configurationService.GetSecret();

            ValidateGraphConfiguration(graphGatewayUrl, hmacKey, hmacSecret);

            var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
            var graphApiUrl = $"{graphGatewayUrl}/api/pinned/collections/{collection.GraphCollectionId}/items";

            var graphItems = pinnedResults.Where(pr => pr.IsActive).Select(pr => new
            {
                phrases = pr.Phrases?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray() ?? Array.Empty<string>(),
                targetKey = pr.TargetKey,
                language = pr.Language,
                priority = pr.Priority,
                isActive = pr.IsActive
            }).ToArray();

            using var request = new HttpRequestMessage(HttpMethod.Put, graphApiUrl);
            request.Content = JsonContent.Create(graphItems);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeader);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error syncing to Optimizely Graph: {response.StatusCode} - {errorContent}");
            }

            return true;
        }

        public async Task<bool> UpdateCollectionGraphIdAsync(Guid collectionId, string graphCollectionId)
        {
            var baseUrl = _configurationService.GetBaseUrl();
            var updateRequest = new { GraphCollectionId = graphCollectionId };
            
            var response = await _httpClient.PatchAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections/{collectionId}/graph-id", 
                JsonContent.Create(updateRequest));

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to update Graph collection ID: {response.ReasonPhrase}");

            return true;
        }

        private bool IsUserAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }

        private static void ValidateCollectionRequest(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Collection title is required");
        }

        private static void ValidatePinnedResultRequest(CreatePinnedResultRequest? request)
        {
            if (request == null)
                throw new ArgumentException("Request is required");

            if (request.CollectionId == Guid.Empty)
                throw new ArgumentException("Collection ID is required");

            if (string.IsNullOrWhiteSpace(request.Phrases))
                throw new ArgumentException("Phrases are required");

            if (string.IsNullOrWhiteSpace(request.TargetKey))
                throw new ArgumentException("Target Key is required");

            if (string.IsNullOrWhiteSpace(request.Language))
                throw new ArgumentException("Language is required");
        }

        private static void ValidateUpdatePinnedResultRequest(UpdatePinnedResultRequest? request)
        {
            if (request == null)
                throw new ArgumentException("Request is required");

            if (string.IsNullOrWhiteSpace(request.Phrases))
                throw new ArgumentException("Phrases are required");

            if (string.IsNullOrWhiteSpace(request.TargetKey))
                throw new ArgumentException("Target Key is required");

            if (string.IsNullOrWhiteSpace(request.Language))
                throw new ArgumentException("Language is required");
        }

        private static void ValidateGraphConfiguration(string gatewayUrl, string hmacKey, string hmacSecret)
        {
            if (string.IsNullOrEmpty(gatewayUrl))
                throw new InvalidOperationException("Optimizely Graph Gateway URL not configured");

            if (string.IsNullOrEmpty(hmacKey) || string.IsNullOrEmpty(hmacSecret))
                throw new InvalidOperationException("Optimizely Graph HMAC credentials not configured");
        }
    }
}