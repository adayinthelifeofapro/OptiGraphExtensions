using System.Net.Http.Headers;
using System.Text;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services;

public class SynonymGraphSyncService : ISynonymGraphSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOptiGraphConfigurationService _configurationService;
    private readonly IGraphConfigurationValidator _graphConfigurationValidator;
    private readonly ISynonymCrudService _synonymCrudService;

    public SynonymGraphSyncService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        IOptiGraphConfigurationService configurationService,
        IGraphConfigurationValidator graphConfigurationValidator,
        ISynonymCrudService synonymCrudService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
        _graphConfigurationValidator = graphConfigurationValidator;
        _synonymCrudService = synonymCrudService;
    }

    public async Task<bool> SyncSynonymsToOptimizelyGraphAsync()
    {
        EnsureUserAuthenticated();

        var synonyms = await GetSynonymsForSync();
        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();
        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

        // Group synonyms by language and sync each language separately
        var synonymsByLanguage = synonyms
            .GroupBy(s => s.Language ?? string.Empty)
            .ToList();

        var errors = new List<string>();

        foreach (var languageGroup in synonymsByLanguage)
        {
            var language = languageGroup.Key;
            var languageSynonyms = languageGroup.ToList();

            try
            {
                await SyncSynonymsForLanguageAsync(gatewayUrl, authenticationHeader, language, languageSynonyms);
            }
            catch (GraphSyncException ex)
            {
                errors.Add($"Language '{language}': {ex.Message}");
            }
        }

        if (errors.Any())
        {
            throw new GraphSyncException($"Errors occurred while syncing synonyms: {string.Join("; ", errors)}");
        }

        return true;
    }

    public async Task<bool> SyncSynonymsForLanguageAsync(string language)
    {
        EnsureUserAuthenticated();

        var allSynonyms = await _synonymCrudService.GetSynonymsAsync();
        var synonymsForLanguage = allSynonyms.Where(s => s.Language == language).ToList();

        if (!synonymsForLanguage.Any())
        {
            throw new InvalidOperationException($"No synonyms found for language '{language}' to sync to Optimizely Graph");
        }

        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();
        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

        await SyncSynonymsForLanguageAsync(gatewayUrl, authenticationHeader, language, synonymsForLanguage);

        return true;
    }

    private async Task SyncSynonymsForLanguageAsync(string gatewayUrl, string authenticationHeader, string language, IList<Synonym> synonyms)
    {
        var graphApiUrl = BuildGraphApiUrl(gatewayUrl, language);
        var synonymsContent = BuildSynonymsContent(synonyms);

        using var request = CreateAuthenticatedRequest(HttpMethod.Put, graphApiUrl, authenticationHeader);
        request.Content = new StringContent(synonymsContent, Encoding.UTF8, "text/plain");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error syncing to Optimizely Graph for language '{language}': {response.StatusCode} - {errorContent}");
        }
    }

    private static string BuildGraphApiUrl(string gatewayUrl, string language)
    {
        var baseUrl = $"{gatewayUrl}/resources/synonyms";

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

    private async Task<IList<Synonym>> GetSynonymsForSync()
    {
        var synonyms = await _synonymCrudService.GetSynonymsAsync();
        if (!synonyms.Any())
            throw new InvalidOperationException("No synonyms found to sync to Optimizely Graph");

        return synonyms;
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