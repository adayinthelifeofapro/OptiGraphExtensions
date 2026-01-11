using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.RequestLogs.Models;
using OptiGraphExtensions.Features.RequestLogs.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.RequestLogs.Services;

public class RequestLogService : IRequestLogService
{
    private readonly HttpClient _httpClient;
    private readonly IOptiGraphConfigurationService _configurationService;
    private readonly IGraphConfigurationValidator _graphConfigurationValidator;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RequestLogService(
        HttpClient httpClient,
        IOptiGraphConfigurationService configurationService,
        IGraphConfigurationValidator graphConfigurationValidator)
    {
        _httpClient = httpClient;
        _configurationService = configurationService;
        _graphConfigurationValidator = graphConfigurationValidator;
    }

    public async Task<IEnumerable<RequestLogModel>> GetRequestLogsAsync(RequestLogQueryParameters? queryParameters = null)
    {
        var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
        var apiUrl = BuildRequestLogsApiUrl(gatewayUrl, queryParameters);

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, apiUrl, authHeader);
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error fetching request logs from Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        var logs = await response.Content.ReadFromJsonAsync<List<RequestLogModel>>(JsonOptions);
        return logs ?? Enumerable.Empty<RequestLogModel>();
    }

    private (string gatewayUrl, string authHeader) GetAuthenticatedConfig()
    {
        var gatewayUrl = _configurationService.GetGatewayUrl();
        var appKey = _configurationService.GetAppKey();
        var secret = _configurationService.GetSecret();

        _graphConfigurationValidator.ValidateConfiguration(gatewayUrl, appKey, secret);

        var authHeader = (appKey + ":" + secret).Base64Encode();
        return (gatewayUrl, authHeader);
    }

    private static string BuildRequestLogsApiUrl(string gatewayUrl, RequestLogQueryParameters? queryParameters)
    {
        gatewayUrl = gatewayUrl.TrimEnd('/');
        var baseUrl = $"{gatewayUrl}/api/logs/request";

        if (queryParameters == null || !queryParameters.HasParameters)
        {
            return baseUrl;
        }

        var queryParts = new List<string>();

        if (queryParameters.Page.HasValue)
        {
            queryParts.Add($"page={queryParameters.Page.Value}");
        }

        if (!string.IsNullOrEmpty(queryParameters.RequestId))
        {
            queryParts.Add($"requestId={Uri.EscapeDataString(queryParameters.RequestId)}");
        }

        if (!string.IsNullOrEmpty(queryParameters.Host))
        {
            queryParts.Add($"host={Uri.EscapeDataString(queryParameters.Host)}");
        }

        if (!string.IsNullOrEmpty(queryParameters.Path))
        {
            queryParts.Add($"path={Uri.EscapeDataString(queryParameters.Path)}");
        }

        if (queryParameters.Success.HasValue)
        {
            queryParts.Add($"success={queryParameters.Success.Value.ToString().ToLowerInvariant()}");
        }

        return queryParts.Count > 0 ? $"{baseUrl}?{string.Join("&", queryParts)}" : baseUrl;
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string authHeader)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        return request;
    }
}
