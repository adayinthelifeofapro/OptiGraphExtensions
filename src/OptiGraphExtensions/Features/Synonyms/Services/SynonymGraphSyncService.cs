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
        var graphApiUrl = $"{gatewayUrl}/resources/synonyms";
        var synonymsContent = BuildSynonymsContent(synonyms);

        using var request = CreateAuthenticatedRequest(HttpMethod.Put, graphApiUrl, authenticationHeader);
        request.Content = new StringContent(synonymsContent, Encoding.UTF8, "text/plain");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error syncing to Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        return true;
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