using System.Net.Http.Headers;
using System.Text;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.StopWords.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.StopWords.Services
{
    public class StopWordGraphSyncService : IStopWordGraphSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthenticationService _authenticationService;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly IGraphConfigurationValidator _graphConfigurationValidator;
        private readonly IStopWordCrudService _stopWordCrudService;

        public StopWordGraphSyncService(
            HttpClient httpClient,
            IAuthenticationService authenticationService,
            IOptiGraphConfigurationService configurationService,
            IGraphConfigurationValidator graphConfigurationValidator,
            IStopWordCrudService stopWordCrudService)
        {
            _httpClient = httpClient;
            _authenticationService = authenticationService;
            _configurationService = configurationService;
            _graphConfigurationValidator = graphConfigurationValidator;
            _stopWordCrudService = stopWordCrudService;
        }

        public async Task<bool> SyncStopWordsToOptimizelyGraphAsync()
        {
            EnsureUserAuthenticated();

            var stopWords = await GetStopWordsForSync();
            var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();
            var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

            // Group stop words by language, then sync each group separately
            var stopWordsByLanguage = stopWords
                .GroupBy(s => s.Language ?? string.Empty)
                .ToList();

            var errors = new List<string>();

            foreach (var group in stopWordsByLanguage)
            {
                var language = group.Key;
                var groupStopWords = group.ToList();

                try
                {
                    await SyncStopWordsForLanguageToGraphAsync(gatewayUrl, authenticationHeader, language, groupStopWords);
                }
                catch (GraphSyncException ex)
                {
                    errors.Add($"Language '{language}': {ex.Message}");
                }
            }

            if (errors.Any())
            {
                throw new GraphSyncException($"Errors occurred while syncing stop words: {string.Join("; ", errors)}");
            }

            return true;
        }

        public async Task<bool> SyncStopWordsForLanguageAsync(string language)
        {
            EnsureUserAuthenticated();

            var allStopWords = await _stopWordCrudService.GetStopWordsAsync();
            var stopWordsForLanguage = allStopWords.Where(s => s.Language == language).ToList();

            if (!stopWordsForLanguage.Any())
            {
                throw new InvalidOperationException($"No stop words found for language '{language}' to sync to Optimizely Graph");
            }

            var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();
            var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

            await SyncStopWordsForLanguageToGraphAsync(gatewayUrl, authenticationHeader, language, stopWordsForLanguage);

            return true;
        }

        public async Task<bool> DeleteAllStopWordsFromGraphAsync(string language)
        {
            EnsureUserAuthenticated();

            var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();
            var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

            var graphApiUrl = BuildGraphApiUrl(gatewayUrl, language);

            using var request = CreateAuthenticatedRequest(HttpMethod.Delete, graphApiUrl, authenticationHeader);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new GraphSyncException($"Error deleting stop words from Optimizely Graph for language '{language}': {response.StatusCode} - {errorContent}");
            }

            return true;
        }

        private async Task SyncStopWordsForLanguageToGraphAsync(string gatewayUrl, string authenticationHeader, string language, IList<StopWord> stopWords)
        {
            var graphApiUrl = BuildGraphApiUrl(gatewayUrl, language);
            var stopWordsContent = BuildStopWordsContent(stopWords);

            using var request = CreateAuthenticatedRequest(HttpMethod.Put, graphApiUrl, authenticationHeader);
            request.Content = new StringContent(stopWordsContent, Encoding.UTF8, "text/plain");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new GraphSyncException($"Error syncing stop words to Optimizely Graph for language '{language}': {response.StatusCode} - {errorContent}");
            }
        }

        private static string BuildGraphApiUrl(string gatewayUrl, string language)
        {
            var baseUrl = $"{gatewayUrl}/resources/stopwords";

            if (!string.IsNullOrWhiteSpace(language))
            {
                return $"{baseUrl}?language_routing={Uri.EscapeDataString(language)}";
            }

            return baseUrl;
        }

        private void EnsureUserAuthenticated()
        {
            if (!_authenticationService.IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");
        }

        private async Task<IList<StopWord>> GetStopWordsForSync()
        {
            var stopWords = await _stopWordCrudService.GetStopWordsAsync();
            if (!stopWords.Any())
                throw new InvalidOperationException("No stop words found to sync to Optimizely Graph");

            return stopWords;
        }

        private (string gatewayUrl, string hmacKey, string hmacSecret) GetAndValidateGraphConfiguration()
        {
            var gatewayUrl = _configurationService.GetGatewayUrl();
            var hmacKey = _configurationService.GetAppKey();
            var hmacSecret = _configurationService.GetSecret();

            _graphConfigurationValidator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret);

            return (gatewayUrl, hmacKey, hmacSecret);
        }

        private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string authenticationHeader)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeader);
            return request;
        }

        private static string BuildStopWordsContent(IList<StopWord> stopWords)
        {
            var stopWordList = new StringBuilder();
            foreach (var stopWord in stopWords)
            {
                stopWordList.AppendLine(stopWord.Word);
            }
            return stopWordList.ToString();
        }
    }
}
