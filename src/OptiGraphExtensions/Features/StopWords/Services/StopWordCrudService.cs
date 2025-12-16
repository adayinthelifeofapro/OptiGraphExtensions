using System.Net.Http.Json;
using System.Text.Json;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Common.Validation;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.StopWords.Models;
using OptiGraphExtensions.Features.StopWords.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.StopWords.Services
{
    public class StopWordCrudService : IStopWordCrudService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthenticationService _authenticationService;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly IValidationService<CreateStopWordRequest> _createValidationService;
        private readonly IValidationService<UpdateStopWordRequest> _updateValidationService;
        private readonly JsonSerializerOptions _jsonOptions;

        public StopWordCrudService(
            HttpClient httpClient,
            IAuthenticationService authenticationService,
            IOptiGraphConfigurationService configurationService,
            IValidationService<CreateStopWordRequest> createValidationService,
            IValidationService<UpdateStopWordRequest> updateValidationService)
        {
            _httpClient = httpClient;
            _authenticationService = authenticationService;
            _configurationService = configurationService;
            _createValidationService = createValidationService;
            _updateValidationService = updateValidationService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IList<StopWord>> GetStopWordsAsync()
        {
            EnsureUserAuthenticated();

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.GetAsync($"{baseUrl}/api/optimizely-graphextensions/stopwords");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error loading stop words: {response.StatusCode} - {response.ReasonPhrase}");

            var responseContent = await response.Content.ReadAsStringAsync();
            return await DeserializeStopWordsResponse(responseContent);
        }

        public async Task<bool> CreateStopWordAsync(CreateStopWordRequest request)
        {
            var validationResult = _createValidationService.Validate(request);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.ErrorMessages);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/stopwords", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("You are not authorized to create stop words");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error creating stop word: {response.StatusCode} - {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> UpdateStopWordAsync(Guid id, UpdateStopWordRequest request)
        {
            var validationResult = _updateValidationService.Validate(request);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.ErrorMessages);

            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/optimizely-graphextensions/stopwords/{id}", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Stop word not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error updating stop word: {response.ReasonPhrase}");

            return true;
        }

        public async Task<bool> DeleteStopWordAsync(Guid id)
        {
            var baseUrl = _configurationService.GetBaseUrl();
            var response = await _httpClient.DeleteAsync($"{baseUrl}/api/optimizely-graphextensions/stopwords/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Stop word not found");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error deleting stop word: {response.ReasonPhrase}");

            return true;
        }

        private void EnsureUserAuthenticated()
        {
            if (!_authenticationService.IsUserAuthenticated())
                throw new UnauthorizedAccessException("User is not authenticated");
        }

        private Task<IList<StopWord>> DeserializeStopWordsResponse(string responseContent)
        {
            try
            {
                var stopWords = JsonSerializer.Deserialize<List<StopWord>>(responseContent, _jsonOptions);
                return Task.FromResult<IList<StopWord>>(stopWords?.OrderBy(s => s.Word).ToList() ?? new List<StopWord>());
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"API returned invalid JSON. Response: {responseContent[..Math.Min(500, responseContent.Length)]}");
            }
        }
    }
}
