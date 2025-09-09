using Microsoft.AspNetCore.Http;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class SynonymApiService : ISynonymApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly JsonSerializerOptions _jsonOptions;

        public SynonymApiService(
            HttpClient httpClient, 
            IHttpContextAccessor httpContextAccessor, 
            IOptiGraphConfigurationService configurationService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configurationService = configurationService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IList<Synonym>> GetSynonymsAsync()
        {
            if (!IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error loading synonyms: {response.StatusCode} - {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            
            try
            {
                var synonyms = JsonSerializer.Deserialize<List<Synonym>>(responseContent, _jsonOptions);
                return synonyms?.OrderBy(s => s.SynonymItem).ToList() ?? new List<Synonym>();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"API returned invalid JSON. Response: {responseContent[..Math.Min(500, responseContent.Length)]}");
            }
        }

        public async Task<bool> CreateSynonymAsync(CreateSynonymRequest request)
        {
            ValidateRequest(request?.Synonym);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("You are not authorized to create synonyms");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error creating synonym: {response.StatusCode} - {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> UpdateSynonymAsync(Guid id, UpdateSynonymRequest request)
        {
            ValidateRequest(request?.Synonym);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms/{id}", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Synonym not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error updating synonym: {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> DeleteSynonymAsync(Guid id)
        {
            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Synonym not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error deleting synonym: {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> SyncSynonymsToOptimizelyGraphAsync()
        {
            if (!IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");

            var synonyms = await GetSynonymsAsync();

            if (!synonyms.Any())
                throw new InvalidOperationException("No synonyms found to sync to Optimizely Graph");

            var graphGatewayUrl = _configurationService.GetGatewayUrl();
            var hmacKey = _configurationService.GetAppKey();
            var hmacSecret = _configurationService.GetSecret();

            ValidateGraphConfiguration(graphGatewayUrl, hmacKey, hmacSecret);

            var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
            var graphApiUrl = $"{graphGatewayUrl}/resources/synonyms";
            var synonymsContent = BuildSynonymsContent(synonyms);

            using var request = new HttpRequestMessage(HttpMethod.Put, graphApiUrl);
            request.Content = new StringContent(synonymsContent, Encoding.UTF8, "text/plain");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeader);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error syncing to Optimizely Graph: {response.StatusCode} - {errorContent}");
            }

            return true;
        }

        private bool IsUserAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }

        private static void ValidateRequest(string? synonym)
        {
            if (string.IsNullOrWhiteSpace(synonym))
                throw new ArgumentException("Synonym text is required");
        }

        private static void ValidateGraphConfiguration(string gatewayUrl, string hmacKey, string hmacSecret)
        {
            if (string.IsNullOrEmpty(gatewayUrl))
                throw new InvalidOperationException("Optimizely Graph Gateway URL not configured");

            if (string.IsNullOrEmpty(hmacKey) || string.IsNullOrEmpty(hmacSecret))
                throw new InvalidOperationException("Optimizely Graph HMAC credentials not configured");
        }

        private static string BuildSynonymsContent(IList<Synonym> synonyms)
        {
            var synonymList = new StringBuilder();
            foreach (var synonym in synonyms)
            {
                synonymList.AppendLine(synonym.SynonymItem);
            }
            return synonymList.ToString();
        }
    }
}