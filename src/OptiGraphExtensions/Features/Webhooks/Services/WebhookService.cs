using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;
using OptiGraphExtensions.Features.Webhooks.Models;
using OptiGraphExtensions.Features.Webhooks.Services.Abstractions;

namespace OptiGraphExtensions.Features.Webhooks.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly IGraphConfigurationValidator _graphConfigurationValidator;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public WebhookService(
            HttpClient httpClient,
            IOptiGraphConfigurationService configurationService,
            IGraphConfigurationValidator graphConfigurationValidator)
        {
            _httpClient = httpClient;
            _configurationService = configurationService;
            _graphConfigurationValidator = graphConfigurationValidator;
        }

        public async Task<IEnumerable<WebhookModel>> GetAllWebhooksAsync()
        {
            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildWebhooksApiUrl(gatewayUrl);

            using var request = CreateAuthenticatedRequest(HttpMethod.Get, apiUrl, authHeader);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new GraphSyncException($"Error fetching webhooks from Optimizely Graph: {response.StatusCode} - {errorContent}");
            }

            var webhookResponses = await response.Content.ReadFromJsonAsync<List<WebhookResponse>>(JsonOptions);
            return webhookResponses?.Select(MapToModel) ?? Enumerable.Empty<WebhookModel>();
        }

        public async Task<WebhookModel> CreateWebhookAsync(CreateWebhookRequest createRequest)
        {
            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = BuildWebhooksApiUrl(gatewayUrl);

            var requestBody = BuildCreateRequestBody(createRequest);
            var jsonContent = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var request = CreateAuthenticatedRequest(HttpMethod.Post, apiUrl, authHeader);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new GraphSyncException($"Error creating webhook in Optimizely Graph: {response.StatusCode} - {errorContent}");
            }

            // Try to parse response, but handle empty responses gracefully
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    var webhookResponse = JsonSerializer.Deserialize<WebhookResponse>(responseContent, JsonOptions);
                    if (webhookResponse != null)
                    {
                        return MapToModel(webhookResponse);
                    }
                }
                catch (JsonException)
                {
                    // Response is not valid JSON, fall through to return model from request
                }
            }

            // API returned empty or non-JSON response, return model based on request
            return new WebhookModel
            {
                Url = createRequest.Url,
                Method = createRequest.Method,
                Disabled = createRequest.Disabled,
                Topics = createRequest.Topics,
                Filters = createRequest.Filters
            };
        }

        public async Task<WebhookModel> UpdateWebhookAsync(UpdateWebhookRequest updateRequest)
        {
            if (string.IsNullOrWhiteSpace(updateRequest.Id))
            {
                throw new ArgumentException("Webhook ID is required", nameof(updateRequest));
            }

            // The Optimizely Graph PUT endpoint doesn't reliably update topics/filters.
            // Workaround: Delete the existing webhook and create a new one with updated config.
            await DeleteWebhookAsync(updateRequest.Id);

            var createRequest = new CreateWebhookRequest
            {
                Url = updateRequest.Url,
                Method = updateRequest.Method,
                Disabled = updateRequest.Disabled,
                Topics = updateRequest.Topics,
                Filters = updateRequest.Filters
            };

            return await CreateWebhookAsync(createRequest);
        }

        public async Task<bool> DeleteWebhookAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Webhook ID is required", nameof(id));
            }

            var (gatewayUrl, authHeader) = GetAuthenticatedConfig();
            var apiUrl = $"{BuildWebhooksApiUrl(gatewayUrl)}/{Uri.EscapeDataString(id)}";

            using var request = CreateAuthenticatedRequest(HttpMethod.Delete, apiUrl, authHeader);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new GraphSyncException($"Error deleting webhook from Optimizely Graph: {response.StatusCode} - {errorContent}");
            }

            return true;
        }

        private (string gatewayUrl, string authHeader) GetAuthenticatedConfig()
        {
            var gatewayUrl = _configurationService.GetGatewayUrl();
            var hmacKey = _configurationService.GetAppKey();
            var hmacSecret = _configurationService.GetSecret();

            _graphConfigurationValidator.ValidateConfiguration(gatewayUrl, hmacKey, hmacSecret);

            var authHeader = (hmacKey + ":" + hmacSecret).Base64Encode();
            return (gatewayUrl, authHeader);
        }

        private static string BuildWebhooksApiUrl(string gatewayUrl)
        {
            // Remove trailing slash if present
            gatewayUrl = gatewayUrl.TrimEnd('/');
            return $"{gatewayUrl}/api/webhooks";
        }

        private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string authHeader)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            return request;
        }

        private static object BuildCreateRequestBody(CreateWebhookRequest createRequest)
        {
            return BuildWebhookRequestBody(
                createRequest.Disabled,
                createRequest.Url,
                createRequest.Method,
                createRequest.Topics,
                createRequest.Filters);
        }

        private static object BuildWebhookRequestBody(
            bool disabled,
            string? url,
            string method,
            List<string> topics,
            List<WebhookFilter> filters)
        {
            // When no topics selected, use "*.*" to subscribe to all events
            // This is the API's way of representing "all events"
            var topicsToSend = (topics != null && topics.Count > 0)
                ? topics
                : new List<string> { "*.*" };

            var body = new Dictionary<string, object>
            {
                ["disabled"] = disabled,
                ["request"] = new Dictionary<string, object>
                {
                    ["url"] = url ?? string.Empty,
                    ["method"] = method.ToLowerInvariant()
                },
                ["topic"] = topicsToSend,
                // Always send filters - empty array [] clears existing filters
                ["filters"] = (filters ?? new List<WebhookFilter>()).Select(f =>
                    new Dictionary<string, Dictionary<string, string>>
                    {
                        [f.Field] = new Dictionary<string, string>
                        {
                            [f.Operator] = f.Value
                        }
                    }
                ).ToList()
            };

            return body;
        }

        private static WebhookModel MapToModel(WebhookResponse response)
        {
            var model = new WebhookModel
            {
                Id = response.Id,
                Disabled = response.Disabled,
                Url = response.Request?.Url,
                Method = response.Request?.Method?.ToUpperInvariant() ?? "POST",
                Topics = response.Topic ?? new List<string>(),
                Filters = new List<WebhookFilter>()
            };

            if (response.Filters != null)
            {
                foreach (var filter in response.Filters)
                {
                    foreach (var field in filter)
                    {
                        foreach (var op in field.Value)
                        {
                            model.Filters.Add(new WebhookFilter
                            {
                                Field = field.Key,
                                Operator = op.Key,
                                Value = op.Value
                            });
                        }
                    }
                }
            }

            return model;
        }
    }
}
