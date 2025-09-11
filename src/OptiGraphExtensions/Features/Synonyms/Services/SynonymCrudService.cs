using System.Net.Http.Json;
using System.Text.Json;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Validation;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services;

public class SynonymCrudService : ISynonymCrudService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOptiGraphConfigurationService _configurationService;
    private readonly IValidationService<CreateSynonymRequest> _createValidationService;
    private readonly IValidationService<UpdateSynonymRequest> _updateValidationService;
    private readonly JsonSerializerOptions _jsonOptions;

    public SynonymCrudService(
        HttpClient httpClient,
        IAuthenticationService authenticationService,
        IOptiGraphConfigurationService configurationService,
        IValidationService<CreateSynonymRequest> createValidationService,
        IValidationService<UpdateSynonymRequest> updateValidationService)
    {
        _httpClient = httpClient;
        _authenticationService = authenticationService;
        _configurationService = configurationService;
        _createValidationService = createValidationService;
        _updateValidationService = updateValidationService;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<IList<Synonym>> GetSynonymsAsync()
    {
        EnsureUserAuthenticated();

        var baseUrl = _configurationService.GetBaseUrl();
        var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/synonyms");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Error loading synonyms: {response.StatusCode} - {response.ReasonPhrase}");

        var responseContent = await response.Content.ReadAsStringAsync();
        return await DeserializeSynonymsResponse(responseContent);
    }

    public async Task<bool> CreateSynonymAsync(CreateSynonymRequest request)
    {
        var validationResult = _createValidationService.Validate(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ErrorMessages);

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
        var validationResult = _updateValidationService.Validate(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ErrorMessages);

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

    private void EnsureUserAuthenticated()
    {
        if (!_authenticationService.IsUserAuthenticated())
            throw new UnauthorizedAccessException("User is not authenticated");
    }

    private Task<IList<Synonym>> DeserializeSynonymsResponse(string responseContent)
    {
        try
        {
            var synonyms = JsonSerializer.Deserialize<List<Synonym>>(responseContent, _jsonOptions);
            return Task.FromResult<IList<Synonym>>(synonyms?.OrderBy(s => s.SynonymItem).ToList() ?? new List<Synonym>());
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"API returned invalid JSON. Response: {responseContent[..Math.Min(500, responseContent.Length)]}");
        }
    }
}