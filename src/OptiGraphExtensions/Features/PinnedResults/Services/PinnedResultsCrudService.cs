using System.Net.Http.Json;
using System.Text.Json;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Validation;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.PinnedResults.Models;
using OptiGraphExtensions.Features.PinnedResults.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.PinnedResults.Services;

public class PinnedResultsCrudService : IPinnedResultsCrudService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOptiGraphConfigurationService _configurationService;
    private readonly IValidationService<CreatePinnedResultRequest> _createValidationService;
    private readonly IValidationService<UpdatePinnedResultRequest> _updateValidationService;
    private readonly JsonSerializerOptions _jsonOptions;

    public PinnedResultsCrudService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        IOptiGraphConfigurationService configurationService,
        IValidationService<CreatePinnedResultRequest> createValidationService,
        IValidationService<UpdatePinnedResultRequest> updateValidationService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
        _createValidationService = createValidationService;
        _updateValidationService = updateValidationService;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<IList<PinnedResult>> GetPinnedResultsAsync(Guid collectionId)
    {
        EnsureUserAuthenticated();

        var baseUrl = _configurationService.GetBaseUrl();
        var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results?collectionId={collectionId}");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Error loading pinned results: {response.StatusCode} - {response.ReasonPhrase}");

        var responseContent = await response.Content.ReadAsStringAsync();
        return await DeserializePinnedResultsResponse(responseContent);
    }

    public async Task<bool> CreatePinnedResultAsync(CreatePinnedResultRequest request)
    {
        var validationResult = _createValidationService.Validate(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ErrorMessages);

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
        var validationResult = _updateValidationService.Validate(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ErrorMessages);

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

    public async Task<bool> DeletePinnedResultsByCollectionIdAsync(Guid collectionId)
    {
        var baseUrl = _configurationService.GetBaseUrl();
        var response = await _httpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/pinned-results/collection/{collectionId}");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Error deleting pinned results for collection: {response.ReasonPhrase}");

        return true;
    }

    private void EnsureUserAuthenticated()
    {
        if (!_authenticationService.IsUserAuthenticated())
            throw new UnauthorizedAccessException("User is not authenticated");
    }

    private Task<IList<PinnedResult>> DeserializePinnedResultsResponse(string responseContent)
    {
        try
        {
            var pinnedResults = JsonSerializer.Deserialize<List<PinnedResult>>(responseContent, _jsonOptions);
            return Task.FromResult<IList<PinnedResult>>(pinnedResults?.OrderBy(pr => pr.Priority).ToList() ?? new List<PinnedResult>());
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"API returned invalid JSON. Response: {responseContent[..Math.Min(500, responseContent.Length)]}");
        }
    }
}