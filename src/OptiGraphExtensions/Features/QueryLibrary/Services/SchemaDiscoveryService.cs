using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OptiGraphExtensions.Common;
using OptiGraphExtensions.Features.Common.Caching;
using OptiGraphExtensions.Features.Common.Services;
using OptiGraphExtensions.Features.QueryLibrary.Models;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.QueryLibrary.Services
{
    /// <summary>
    /// Service for discovering the Optimizely Graph schema via GraphQL introspection.
    /// </summary>
    public class SchemaDiscoveryService : ISchemaDiscoveryService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptiGraphConfigurationService _configurationService;
        private readonly IGraphConfigurationValidator _graphConfigurationValidator;
        private readonly ICacheService _cacheService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        private const string ContentTypesCacheKey = "QueryLibrary:ContentTypes";
        private const string FieldsCacheKeyPrefix = "QueryLibrary:Fields:";

        public SchemaDiscoveryService(
            HttpClient httpClient,
            IOptiGraphConfigurationService configurationService,
            IGraphConfigurationValidator graphConfigurationValidator,
            ICacheService cacheService)
        {
            _httpClient = httpClient;
            _configurationService = configurationService;
            _graphConfigurationValidator = graphConfigurationValidator;
            _cacheService = cacheService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<IList<string>> GetContentTypesAsync()
        {
            // Check cache first
            var cached = await _cacheService.GetAsync<IList<string>>(ContentTypesCacheKey);
            if (cached != null)
            {
                return cached;
            }

            var (gatewayUrl, appKey, secret) = GetAndValidateGraphConfiguration();

            // Use introspection to get all types that are likely content types
            var query = @"
                query IntrospectSchema {
                    __schema {
                        types {
                            name
                            kind
                            interfaces {
                                name
                            }
                        }
                    }
                }";

            var response = await ExecuteGraphQueryAsync(gatewayUrl, appKey, secret, query);

            var contentTypes = ParseContentTypes(response);

            // Cache the result
            await _cacheService.SetAsync(ContentTypesCacheKey, contentTypes, _cacheExpiration);

            return contentTypes;
        }

        public async Task<IList<SchemaField>> GetFieldsForContentTypeAsync(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return new List<SchemaField>();
            }

            // Check cache first
            var cacheKey = $"{FieldsCacheKeyPrefix}{contentType}";
            var cached = await _cacheService.GetAsync<IList<SchemaField>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var (gatewayUrl, appKey, secret) = GetAndValidateGraphConfiguration();

            var query = $@"
                query IntrospectType {{
                    __type(name: ""{contentType}"") {{
                        name
                        fields {{
                            name
                            type {{
                                name
                                kind
                                ofType {{
                                    name
                                    kind
                                    ofType {{
                                        name
                                        kind
                                    }}
                                }}
                            }}
                        }}
                    }}
                }}";

            var response = await ExecuteGraphQueryAsync(gatewayUrl, appKey, secret, query);

            var fields = ParseFields(response, contentType);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, fields, _cacheExpiration);

            return fields;
        }

        public async Task ClearCacheAsync()
        {
            await _cacheService.RemoveByPatternAsync("^QueryLibrary:");
        }

        private IList<string> ParseContentTypes(string response)
        {
            try
            {
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("__schema", out var schema) ||
                    !schema.TryGetProperty("types", out var types))
                {
                    return new List<string>();
                }

                var contentTypes = new List<string>();

                foreach (var type in types.EnumerateArray())
                {
                    if (!type.TryGetProperty("name", out var nameProp) ||
                        !type.TryGetProperty("kind", out var kindProp))
                    {
                        continue;
                    }

                    var typeName = nameProp.GetString();
                    var kind = kindProp.GetString();

                    // Skip internal types (start with __) and non-OBJECT types
                    if (string.IsNullOrEmpty(typeName) ||
                        typeName.StartsWith("__") ||
                        kind != "OBJECT")
                    {
                        continue;
                    }

                    // Check if the type implements IContent or similar interface
                    // or has a name that suggests it's a content type
                    if (type.TryGetProperty("interfaces", out var interfaces))
                    {
                        var implementsContent = false;
                        foreach (var iface in interfaces.EnumerateArray())
                        {
                            if (iface.TryGetProperty("name", out var ifaceName))
                            {
                                var ifaceNameStr = ifaceName.GetString();
                                if (ifaceNameStr == "IContent" ||
                                    ifaceNameStr == "IContentData" ||
                                    ifaceNameStr == "ILocale")
                                {
                                    implementsContent = true;
                                    break;
                                }
                            }
                        }

                        if (implementsContent)
                        {
                            contentTypes.Add(typeName);
                        }
                    }

                    // Also include types ending with "Page", "Block", "Media" as they're likely content types
                    if (!contentTypes.Contains(typeName) &&
                        (typeName.EndsWith("Page") ||
                         typeName.EndsWith("Block") ||
                         typeName.EndsWith("Media") ||
                         typeName.EndsWith("Folder") ||
                         typeName.EndsWith("File") ||
                         typeName.EndsWith("Image") ||
                         typeName.EndsWith("Video")))
                    {
                        contentTypes.Add(typeName);
                    }
                }

                return contentTypes.Distinct().OrderBy(n => n).ToList();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        private IList<SchemaField> ParseFields(string response, string contentType)
        {
            try
            {
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("__type", out var typeInfo))
                {
                    return new List<SchemaField>();
                }

                if (typeInfo.ValueKind == JsonValueKind.Null ||
                    !typeInfo.TryGetProperty("fields", out var fields))
                {
                    return new List<SchemaField>();
                }

                var result = new List<SchemaField>();

                foreach (var field in fields.EnumerateArray())
                {
                    var schemaField = ParseFieldInfo(field, "");
                    if (schemaField != null)
                    {
                        result.Add(schemaField);
                    }
                }

                return result.OrderBy(f => f.Name).ToList();
            }
            catch (JsonException)
            {
                return new List<SchemaField>();
            }
        }

        private SchemaField? ParseFieldInfo(JsonElement field, string parentPath)
        {
            if (!field.TryGetProperty("name", out var nameProp) ||
                !field.TryGetProperty("type", out var typeProp))
            {
                return null;
            }

            var fieldName = nameProp.GetString();
            if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("_"))
            {
                // Skip internal fields (except we might want to keep some like _score)
                if (fieldName != "_score")
                {
                    return null;
                }
            }

            var (typeName, typeKind, isList) = ResolveType(typeProp);

            var path = string.IsNullOrEmpty(parentPath)
                ? fieldName
                : $"{parentPath}.{fieldName}";

            var schemaField = new SchemaField
            {
                Name = fieldName!,
                Path = path!,
                Type = typeName ?? "Unknown",
                IsNested = typeKind == "OBJECT",
                IsList = isList
            };

            return schemaField;
        }

        private (string? typeName, string? typeKind, bool isList) ResolveType(JsonElement typeElement)
        {
            var kind = typeElement.TryGetProperty("kind", out var kindProp)
                ? kindProp.GetString()
                : null;

            var name = typeElement.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString()
                : null;

            // Handle NON_NULL and LIST wrappers
            if ((kind == "NON_NULL" || kind == "LIST") &&
                typeElement.TryGetProperty("ofType", out var ofType))
            {
                var isList = kind == "LIST";
                var (innerName, innerKind, innerIsList) = ResolveType(ofType);
                return (innerName, innerKind, isList || innerIsList);
            }

            return (name, kind, false);
        }

        private async Task<string> ExecuteGraphQueryAsync(
            string gatewayUrl,
            string appKey,
            string secret,
            string query)
        {
            var authHeader = $"{appKey}:{secret}".Base64Encode();
            var graphqlEndpoint = $"{gatewayUrl}/content/v2";

            var request = new { query };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, graphqlEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"GraphQL introspection failed: {response.StatusCode} - {errorContent}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        private (string gatewayUrl, string appKey, string secret) GetAndValidateGraphConfiguration()
        {
            var gatewayUrl = _configurationService.GetGatewayUrl();
            var appKey = _configurationService.GetAppKey();
            var secret = _configurationService.GetSecret();

            _graphConfigurationValidator.ValidateConfiguration(gatewayUrl, appKey, secret);

            return (gatewayUrl, appKey, secret);
        }
    }
}
