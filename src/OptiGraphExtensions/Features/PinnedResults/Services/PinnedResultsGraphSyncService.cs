using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services;

public class PinnedResultsGraphSyncService : IPinnedResultsGraphSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IOptiGraphConfigurationService _configurationService;
    private readonly IGraphConfigurationValidator _graphConfigurationValidator;
    private readonly IPinnedResultsCollectionService _collectionService;
    private readonly IPinnedResultService _pinnedResultService;
    private readonly JsonSerializerOptions _jsonOptions;

    public PinnedResultsGraphSyncService(
        HttpClient httpClient,
        IOptiGraphConfigurationService configurationService,
        IGraphConfigurationValidator graphConfigurationValidator,
        IPinnedResultsCollectionService collectionService,
        IPinnedResultService pinnedResultService)
    {
        _httpClient = httpClient;
        _configurationService = configurationService;
        _graphConfigurationValidator = graphConfigurationValidator;
        _collectionService = collectionService;
        _pinnedResultService = pinnedResultService;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<bool> SyncCollectionToOptimizelyGraphAsync(PinnedResultsCollection collection)
    {
        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections";

        var graphRequest = CreateGraphCollectionRequest(collection);

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, graphApiUrl, authenticationHeader);
        request.Content = JsonContent.Create(graphRequest);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Graph sync failed: {response.StatusCode} - {errorContent}");
        }

        await UpdateCollectionWithGraphId(collection, response);
        return true;
    }

    public async Task<bool> SyncPinnedResultsToOptimizelyGraphAsync(Guid collectionId)
    {
        var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
        if (collection == null)
            throw new InvalidOperationException("Collection not found.");

        // If collection doesn't have a GraphCollectionId, sync it first
        if (string.IsNullOrEmpty(collection.GraphCollectionId))
        {
            await SyncCollectionToOptimizelyGraphAsync(collection);
            // Refresh collection to get the updated GraphCollectionId
            collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            if (collection == null || string.IsNullOrEmpty(collection.GraphCollectionId))
                throw new InvalidOperationException("Failed to sync collection to Optimizely Graph.");
        }

        var pinnedResults = await GetPinnedResultsForSync(collectionId);

        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections/{collection.GraphCollectionId}/items";

        // Send pinned results in parallel batches for improved performance
        var activePinnedResults = pinnedResults.Where(pr => pr.IsActive).ToList();
        const int batchSize = 5; // Limit concurrent requests to avoid overwhelming the API

        var batches = activePinnedResults
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            var tasks = batch.Select(pinnedResult => SyncSinglePinnedResultAsync(pinnedResult, graphApiUrl, authenticationHeader));
            await Task.WhenAll(tasks);
        }

        return true;
    }

    private async Task SyncSinglePinnedResultAsync(PinnedResult pinnedResult, string graphApiUrl, string authenticationHeader)
    {
        var graphItem = CreateGraphPinnedResultItem(pinnedResult);
        var jsonString = JsonSerializer.Serialize(graphItem, _jsonOptions);

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, graphApiUrl, authenticationHeader);
        request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error syncing pinned result to Optimizely Graph: {response.StatusCode} - {errorContent}");
        }
    }

    public async Task<bool> DeletePinnedResultFromOptimizelyGraphAsync(string graphCollectionId, string graphId)
    {
        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections/{graphCollectionId}/items/{graphId}";

        using var request = CreateAuthenticatedRequest(HttpMethod.Delete, graphApiUrl, authenticationHeader);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error deleting pinned result from Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        return true;
    }

    public async Task<bool> DeleteAllItemsFromCollectionAsync(string graphCollectionId)
    {
        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections/{graphCollectionId}/items";

        using var request = CreateAuthenticatedRequest(HttpMethod.Delete, graphApiUrl, authenticationHeader);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return true; //never mind, already removed, contine as removed. 

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error deleting all items from collection in Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        return true;
    }

    public async Task<bool> DeleteCollectionFromOptimizelyGraphAsync(string graphCollectionId)
    {
        // Optimizely Graph requires all items to be removed before deleting a collection
        await DeleteAllItemsFromCollectionAsync(graphCollectionId);

        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections/{graphCollectionId}";

        using var request = CreateAuthenticatedRequest(HttpMethod.Delete, graphApiUrl, authenticationHeader);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error deleting collection from Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        return true;
    }

    public async Task<IList<GraphCollectionResponse>> SyncCollectionsFromOptimizelyGraphAsync()
    {
        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections";

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, graphApiUrl, authenticationHeader);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Failed to fetch collections from Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        return DeserializeGraphCollectionsResponse(responseContent);
    }

    public async Task<string> SyncSinglePinnedResultToOptimizelyGraphAsync(PinnedResult pinnedResult, string graphCollectionId)
    {
        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections/{graphCollectionId}/items";

        var graphItem = CreateGraphPinnedResultItem(pinnedResult);
        var jsonString = JsonSerializer.Serialize(graphItem, _jsonOptions);

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, graphApiUrl, authenticationHeader);
        request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Error syncing pinned result to Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        // Parse the response to get the Graph ID
        var responseContent = await response.Content.ReadAsStringAsync();
        var graphResponse = JsonSerializer.Deserialize<GraphPinnedResultResponse>(responseContent, _jsonOptions);
        
        return graphResponse?.Id ?? string.Empty;
    }

    public async Task<bool> SyncPinnedResultsFromOptimizelyGraphAsync(Guid collectionId)
    {
        var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
        if (collection == null || string.IsNullOrEmpty(collection.GraphCollectionId))
            throw new InvalidOperationException("Collection has no Graph collection ID. Please sync the collection to Graph first.");

        var (gatewayUrl, hmacKey, hmacSecret) = GetAndValidateGraphConfiguration();

        var authenticationHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
        var graphApiUrl = $"{gatewayUrl}/api/pinned/collections/{collection.GraphCollectionId}/items";

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, graphApiUrl, authenticationHeader);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new GraphSyncException($"Failed to fetch pinned results from Optimizely Graph: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var graphPinnedResults = DeserializeGraphPinnedResultsResponse(responseContent);

        // Preserve existing TargetName values before deletion (Graph doesn't store them)
        var existingResults = await _pinnedResultService.GetAllPinnedResultsAsync(collectionId);
        var targetNameLookup = existingResults
            .Where(r => !string.IsNullOrEmpty(r.TargetKey) && !string.IsNullOrEmpty(r.TargetName))
            .ToDictionary(r => r.TargetKey!, r => r.TargetName);

        // Clear existing pinned results for this collection
        await _pinnedResultService.DeletePinnedResultsByCollectionIdAsync(collectionId);

        // Create new pinned results from Graph data
        foreach (var graphItem in graphPinnedResults)
        {
            // Look up preserved TargetName by TargetKey
            var targetName = graphItem.TargetKey != null && targetNameLookup.TryGetValue(graphItem.TargetKey, out var name)
                ? name
                : null;

            await _pinnedResultService.CreatePinnedResultAsync(
                collectionId,
                graphItem.Phrases ?? string.Empty,
                graphItem.TargetKey ?? string.Empty,
                targetName,
                graphItem.Language ?? "en",
                graphItem.Priority,
                graphItem.IsActive,
                null, // CreatedBy
                graphItem.Id // GraphId
            );
        }

        return true;
    }

    private (string gatewayUrl, string hmacKey, string hmacSecret) GetAndValidateGraphConfiguration()
    {
        var gatewayUrl = _configurationService.GetGatewayUrl();
        var hmacKey = _configurationService.GetAppKey();
        var hmacSecret = _configurationService.GetSecret();

        _graphConfigurationValidator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret);

        return (gatewayUrl, hmacKey, hmacSecret);
    }


    private async Task<IList<PinnedResult>> GetPinnedResultsForSync(Guid collectionId)
    {
        var pinnedResults = await _pinnedResultService.GetAllPinnedResultsAsync(collectionId);
        if (!pinnedResults.Any())
            throw new InvalidOperationException("No pinned results found to sync to Optimizely Graph.");
        
        return pinnedResults.ToList();
    }

    private static object CreateGraphCollectionRequest(PinnedResultsCollection collection)
    {
        return new
        {
            key = collection.Id.ToString(),
            title = collection.Title,
            isActive = collection.IsActive
        };
    }

    private static object CreateGraphPinnedResultItem(PinnedResult pinnedResult)
    {
        return new
        {
            phrases = pinnedResult.Phrases?.Trim() ?? string.Empty,
            targetKey = pinnedResult.TargetKey,
            language = pinnedResult.Language,
            priority = pinnedResult.Priority,
            isActive = pinnedResult.IsActive
        };
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string authenticationHeader)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeader);
        return request;
    }

    private async Task UpdateCollectionWithGraphId(PinnedResultsCollection collection, HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        try
        {
            var graphCollection = JsonSerializer.Deserialize<GraphCollectionResponse>(responseContent, _jsonOptions);
            if (!string.IsNullOrEmpty(graphCollection?.Id))
            {
                await _collectionService.UpdateGraphCollectionIdAsync(collection.Id, graphCollection.Id);
            }
        }
        catch (JsonException)
        {
            throw new GraphSyncException($"Failed to parse Graph collection response: {responseContent}");
        }
    }

    private IList<GraphCollectionResponse> DeserializeGraphCollectionsResponse(string responseContent)
    {
        try
        {
            var graphCollections = JsonSerializer.Deserialize<List<GraphCollectionResponse>>(responseContent, _jsonOptions);
            return graphCollections?.OrderBy(c => c.Title).ToList() ?? new List<GraphCollectionResponse>();
        }
        catch (JsonException)
        {
            throw new GraphSyncException($"Failed to parse Graph collections response: {responseContent[..Math.Min(500, responseContent.Length)]}");
        }
    }

    private IList<GraphPinnedResultResponse> DeserializeGraphPinnedResultsResponse(string responseContent)
    {
        try
        {
            var graphPinnedResults = JsonSerializer.Deserialize<List<GraphPinnedResultResponse>>(responseContent, _jsonOptions);
            return graphPinnedResults ?? new List<GraphPinnedResultResponse>();
        }
        catch (JsonException)
        {
            throw new GraphSyncException($"Failed to parse Graph pinned results response: {responseContent[..Math.Min(500, responseContent.Length)]}");
        }
    }
}