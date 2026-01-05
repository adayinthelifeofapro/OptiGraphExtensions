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
    private readonly ISynonymService _synonymService;

    public SynonymGraphSyncService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        IOptiGraphConfigurationService configurationService,
        IGraphConfigurationValidator graphConfigurationValidator,
        ISynonymService synonymService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
        _graphConfigurationValidator = graphConfigurationValidator;
        _synonymService = synonymService;
    }

    public async Task<bool> SyncSynonymsToOptimizelyGraphAsync()
    {
        EnsureUserAuthenticated();

        var synonyms = await GetSynonymsForSync();
        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();
        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

        // Group synonyms by language and slot, then sync each group separately
        var synonymsByLanguageAndSlot = synonyms
            .GroupBy(s => new { Language = s.Language ?? string.Empty, s.Slot })
            .ToList();

        var errors = new List<string>();

        foreach (var group in synonymsByLanguageAndSlot)
        {
            var language = group.Key.Language;
            var slot = group.Key.Slot;
            var groupSynonyms = group.ToList();

            try
            {
                await SyncSynonymsForLanguageAndSlotAsync(gatewayUrl, authenticationHeader, language, slot, groupSynonyms);
            }
            catch (GraphSyncException ex)
            {
                errors.Add($"Language '{language}', Slot '{slot}': {ex.Message}");
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

        var allSynonyms = await _synonymService.GetAllSynonymsAsync();
        var synonymsForLanguage = allSynonyms.Where(s => s.Language == language).ToList();

        if (!synonymsForLanguage.Any())
        {
            throw new InvalidOperationException($"No synonyms found for language '{language}' to sync to Optimizely Graph");
        }

        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();
        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();

        // Group by slot and sync each slot separately for this language
        var synonymsBySlot = synonymsForLanguage
            .GroupBy(s => s.Slot)
            .ToList();

        var errors = new List<string>();

        foreach (var slotGroup in synonymsBySlot)
        {
            var slot = slotGroup.Key;
            var slotSynonyms = slotGroup.ToList();

            try
            {
                await SyncSynonymsForLanguageAndSlotAsync(gatewayUrl, authenticationHeader, language, slot, slotSynonyms);
            }
            catch (GraphSyncException ex)
            {
                errors.Add($"Slot '{slot}': {ex.Message}");
            }
        }

        if (errors.Any())
        {
            throw new GraphSyncException($"Errors occurred while syncing synonyms for language '{language}': {string.Join("; ", errors)}");
        }

        return true;
    }

    private async Task SyncSynonymsForLanguageAndSlotAsync(string gatewayUrl, string authenticationHeader, string language, SynonymSlot slot, IList<Synonym> synonyms)
    {
        var graphApiUrl = BuildGraphApiUrl(gatewayUrl, language, slot);
        var synonymsContent = BuildSynonymsContent(synonyms);

        using var request = CreateAuthenticatedRequest(HttpMethod.Put, graphApiUrl, authenticationHeader);
        request.Content = new StringContent(synonymsContent, Encoding.UTF8, "text/plain");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error syncing to Optimizely Graph for language '{language}', slot '{slot}': {response.StatusCode} - {errorContent}");
        }
    }

    private static string BuildGraphApiUrl(string gatewayUrl, string language, SynonymSlot slot)
    {
        var baseUrl = $"{gatewayUrl}/resources/synonyms";
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(language))
        {
            queryParams.Add($"language_routing={Uri.EscapeDataString(language)}");
        }

        queryParams.Add($"synonym_slot={slot.ToString().ToUpperInvariant()}");

        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }

    private void EnsureUserAuthenticated()
    {
        if (!_authenticationService.IsUserAuthenticated())
            throw new UnauthorizedAccessException("User is not authenticated");
    }

    private async Task<IList<Synonym>> GetSynonymsForSync()
    {
        var synonyms = await _synonymService.GetAllSynonymsAsync();
        if (!synonyms.Any())
            throw new InvalidOperationException("No synonyms found to sync to Optimizely Graph");

        return synonyms.ToList();
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