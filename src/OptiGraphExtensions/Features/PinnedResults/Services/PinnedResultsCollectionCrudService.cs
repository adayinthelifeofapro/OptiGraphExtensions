using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Validation;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services;

public class PinnedResultsCollectionCrudService : IPinnedResultsCollectionCrudService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOptiGraphConfigurationService _configurationService;
    private readonly IValidationService<CreatePinnedResultsCollectionRequest> _createValidationService;
    private readonly IValidationService<UpdatePinnedResultsCollectionRequest> _updateValidationService;
    private readonly JsonSerializerOptions _jsonOptions;

    public PinnedResultsCollectionCrudService(
        HttpClient httpClient, 
        IAuthenticationService authenticationService,
        IOptiGraphConfigurationService configurationService,
        IValidationService<CreatePinnedResultsCollectionRequest> createValidationService,
        IValidationService<UpdatePinnedResultsCollectionRequest> updateValidationService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
        _createValidationService = createValidationService;
        _updateValidationService = updateValidationService;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<IList<PinnedResultsCollection>> GetCollectionsAsync()
    {
        EnsureUserAuthenticated();

        var baseUrl = _configurationService.GetBaseUrl();
        var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Error loading collections: {response.StatusCode} - {response.ReasonPhrase}");

        var responseContent = await response.Content.ReadAsStringAsync();
        return await DeserializeCollectionsResponse(responseContent);
    }

    public async Task<PinnedResultsCollection> CreateCollectionAsync(CreatePinnedResultsCollectionRequest request)
    {
        var validationResult = _createValidationService.Validate(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ErrorMessages);

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
        var validationResult = _updateValidationService.Validate(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ErrorMessages);

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
        EnsureUserAuthenticated();

        var baseUrl = _configurationService.GetBaseUrl();
        var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results-collections/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Error retrieving collection: {response.StatusCode} - {response.ReasonPhrase}");

        var responseContent = await response.Content.ReadAsStringAsync();
        return await DeserializeCollectionResponse(responseContent);
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

    private void EnsureUserAuthenticated()
    {
        if (!_authenticationService.IsUserAuthenticated())
            throw new UnauthorizedAccessException("User is not authenticated");
    }

    private Task<IList<PinnedResultsCollection>> DeserializeCollectionsResponse(string responseContent)
    {
        try
        {
            var collections = JsonSerializer.Deserialize<List<PinnedResultsCollection>>(responseContent, _jsonOptions);
            return Task.FromResult<IList<PinnedResultsCollection>>(collections?.OrderBy(c => c.Title).ToList() ?? new List<PinnedResultsCollection>());
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"API returned invalid JSON. Response: {responseContent[..Math.Min(500, responseContent.Length)]}");
        }
    }

    private Task<PinnedResultsCollection?> DeserializeCollectionResponse(string responseContent)
    {
        try
        {
            var collection = JsonSerializer.Deserialize<PinnedResultsCollection>(responseContent, _jsonOptions);
            return Task.FromResult(collection);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Failed to parse collection data");
        }
    }
}