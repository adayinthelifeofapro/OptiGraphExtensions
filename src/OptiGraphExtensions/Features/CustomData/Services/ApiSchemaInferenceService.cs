using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Service for inferring content type schemas from external API responses.
    /// </summary>
    public class ApiSchemaInferenceService : IApiSchemaInferenceService
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiSchemaInferenceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiSchemaInferenceResult> InferSchemaFromApiAsync(
            string apiUrl,
            string contentTypeName,
            string? jsonPath = null,
            Dictionary<string, string>? headers = null)
        {
            var debugInfo = new StringBuilder();

            try
            {
                // Validate URL
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    return new ApiSchemaInferenceResult
                    {
                        Success = false,
                        ErrorMessage = "API URL is required"
                    };
                }

                if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
                {
                    return new ApiSchemaInferenceResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid API URL format"
                    };
                }

                // Validate content type name
                var sanitizedName = SanitizeContentTypeName(contentTypeName);
                if (string.IsNullOrEmpty(sanitizedName))
                {
                    return new ApiSchemaInferenceResult
                    {
                        Success = false,
                        ErrorMessage = "Content type name is required and must start with a letter"
                    };
                }

                debugInfo.AppendLine($"Fetching: {apiUrl}");

                // Create request
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Add("Accept", "application/json");

                // Add custom headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                // Fetch API response
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                debugInfo.AppendLine($"Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    return new ApiSchemaInferenceResult
                    {
                        Success = false,
                        ErrorMessage = $"API returned error: {response.StatusCode}",
                        DebugInfo = debugInfo.ToString()
                    };
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return new ApiSchemaInferenceResult
                    {
                        Success = false,
                        ErrorMessage = "API returned empty response",
                        DebugInfo = debugInfo.ToString()
                    };
                }

                debugInfo.AppendLine($"Response length: {responseContent.Length} characters");

                // Parse JSON
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                // Navigate to JSON path if specified
                if (!string.IsNullOrWhiteSpace(jsonPath))
                {
                    debugInfo.AppendLine($"Navigating to path: {jsonPath}");
                    var navigated = NavigateToJsonPath(root, jsonPath);
                    if (navigated == null)
                    {
                        return new ApiSchemaInferenceResult
                        {
                            Success = false,
                            ErrorMessage = $"JSON path '{jsonPath}' not found in response",
                            DebugInfo = debugInfo.ToString()
                        };
                    }
                    root = navigated.Value;
                }

                // Find array of objects to infer schema from
                var items = FindItemsArray(root);
                if (items == null || items.Count == 0)
                {
                    // Try using root object directly if it's an object
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        items = new List<JsonElement> { root };
                        debugInfo.AppendLine("Using root object for schema inference");
                    }
                    else
                    {
                        return new ApiSchemaInferenceResult
                        {
                            Success = false,
                            ErrorMessage = "Could not find an array of objects in the API response. Try specifying a JSON path.",
                            DebugInfo = debugInfo.ToString()
                        };
                    }
                }

                debugInfo.AppendLine($"Found {items.Count} items for schema inference");

                // Infer schema from items
                var properties = InferPropertiesFromItems(items, debugInfo);

                if (!properties.Any())
                {
                    return new ApiSchemaInferenceResult
                    {
                        Success = false,
                        ErrorMessage = "Could not infer any properties from the API response",
                        DebugInfo = debugInfo.ToString()
                    };
                }

                // Create content type model
                var contentType = new ContentTypeSchemaModel
                {
                    Name = sanitizedName,
                    Label = contentTypeName,
                    Properties = properties
                };

                // Extract sample data (up to 5 items)
                var sampleData = ExtractSampleData(items.Take(5).ToList());

                debugInfo.AppendLine($"\nInferred {properties.Count} properties:");
                foreach (var prop in properties)
                {
                    debugInfo.AppendLine($"  - {prop.Name}: {prop.Type}");
                }

                return new ApiSchemaInferenceResult
                {
                    Success = true,
                    ContentType = contentType,
                    SampleData = sampleData,
                    DebugInfo = debugInfo.ToString()
                };
            }
            catch (HttpRequestException ex)
            {
                return new ApiSchemaInferenceResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}",
                    DebugInfo = debugInfo.ToString()
                };
            }
            catch (JsonException ex)
            {
                return new ApiSchemaInferenceResult
                {
                    Success = false,
                    ErrorMessage = $"Invalid JSON response: {ex.Message}",
                    DebugInfo = debugInfo.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiSchemaInferenceResult
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}",
                    DebugInfo = debugInfo.ToString()
                };
            }
        }

        private static string SanitizeContentTypeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Remove invalid characters, keep letters, numbers, underscores
            var sanitized = Regex.Replace(name, @"[^A-Za-z0-9_]", "");

            // Ensure starts with a letter
            if (string.IsNullOrEmpty(sanitized) || !char.IsLetter(sanitized[0]))
            {
                // Prepend a letter if needed
                sanitized = "T" + sanitized;
            }

            // PascalCase the name
            if (sanitized.Length > 0)
            {
                sanitized = char.ToUpper(sanitized[0]) + sanitized.Substring(1);
            }

            return sanitized;
        }

        private static JsonElement? NavigateToJsonPath(JsonElement root, string path)
        {
            var segments = path.Split(new[] { '.', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            foreach (var segment in segments)
            {
                // Handle array index notation (e.g., "items[0]")
                var match = Regex.Match(segment, @"^(\w+)\[(\d+)\]$");
                if (match.Success)
                {
                    var propName = match.Groups[1].Value;
                    var index = int.Parse(match.Groups[2].Value);

                    if (!current.TryGetProperty(propName, out var arrElement))
                        return null;

                    if (arrElement.ValueKind != JsonValueKind.Array || arrElement.GetArrayLength() <= index)
                        return null;

                    current = arrElement[index];
                }
                else
                {
                    if (!current.TryGetProperty(segment, out var next))
                        return null;
                    current = next;
                }
            }

            return current;
        }

        private static List<JsonElement>? FindItemsArray(JsonElement element)
        {
            // If it's already an array, use it
            if (element.ValueKind == JsonValueKind.Array)
            {
                var items = element.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Object)
                    .ToList();

                if (items.Any())
                    return items;
            }

            // If it's an object, look for common array property names
            if (element.ValueKind == JsonValueKind.Object)
            {
                var commonArrayNames = new[] { "data", "items", "results", "records", "rows", "values", "content", "entries", "list" };

                foreach (var name in commonArrayNames)
                {
                    if (element.TryGetProperty(name, out var arrayProp) && arrayProp.ValueKind == JsonValueKind.Array)
                    {
                        var items = arrayProp.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.Object)
                            .ToList();

                        if (items.Any())
                            return items;
                    }
                }

                // Check any property that's an array of objects
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var items = prop.Value.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.Object)
                            .ToList();

                        if (items.Any())
                            return items;
                    }
                }
            }

            return null;
        }

        private static List<PropertyTypeModel> InferPropertiesFromItems(List<JsonElement> items, StringBuilder debugInfo)
        {
            var propertyTypes = new Dictionary<string, HashSet<JsonValueKind>>();
            var propertyArrayItemTypes = new Dictionary<string, HashSet<JsonValueKind>>();

            // Analyze all items to determine property types
            foreach (var item in items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var prop in item.EnumerateObject())
                {
                    var propName = prop.Name;

                    // Skip internal/system properties
                    if (propName.StartsWith("_") || propName.StartsWith("$"))
                        continue;

                    if (!propertyTypes.ContainsKey(propName))
                    {
                        propertyTypes[propName] = new HashSet<JsonValueKind>();
                        propertyArrayItemTypes[propName] = new HashSet<JsonValueKind>();
                    }

                    propertyTypes[propName].Add(prop.Value.ValueKind);

                    // For arrays, track the item types
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var arrItem in prop.Value.EnumerateArray())
                        {
                            propertyArrayItemTypes[propName].Add(arrItem.ValueKind);
                        }
                    }
                }
            }

            // Convert to property models
            var properties = new List<PropertyTypeModel>();

            foreach (var kvp in propertyTypes.OrderBy(k => k.Key))
            {
                var propName = kvp.Key;
                var types = kvp.Value;
                var arrayItemTypes = propertyArrayItemTypes[propName];

                // Sanitize property name
                var sanitizedName = SanitizePropertyName(propName);
                if (string.IsNullOrEmpty(sanitizedName))
                    continue;

                var graphType = InferGraphType(types, arrayItemTypes);

                properties.Add(new PropertyTypeModel
                {
                    Name = sanitizedName,
                    Type = graphType,
                    IsSearchable = graphType == "String" || graphType == "[String]",
                    IsRequired = false
                });
            }

            return properties;
        }

        private static string SanitizePropertyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Remove invalid characters
            var sanitized = Regex.Replace(name, @"[^A-Za-z0-9_]", "");

            // Ensure starts with a letter
            if (string.IsNullOrEmpty(sanitized) || !char.IsLetter(sanitized[0]))
            {
                sanitized = "P" + sanitized;
            }

            // PascalCase
            if (sanitized.Length > 0)
            {
                sanitized = char.ToUpper(sanitized[0]) + sanitized.Substring(1);
            }

            return sanitized;
        }

        private static string InferGraphType(HashSet<JsonValueKind> types, HashSet<JsonValueKind> arrayItemTypes)
        {
            // Remove null from consideration
            types.Remove(JsonValueKind.Null);
            types.Remove(JsonValueKind.Undefined);

            if (!types.Any())
                return "String"; // Default to string

            // If it's an array
            if (types.Contains(JsonValueKind.Array))
            {
                arrayItemTypes.Remove(JsonValueKind.Null);
                arrayItemTypes.Remove(JsonValueKind.Undefined);

                if (arrayItemTypes.Contains(JsonValueKind.Number))
                {
                    // Check if all numbers are integers
                    return "[Int]"; // Optimizely Graph uses bracket notation for arrays
                }

                return "[String]";
            }

            // Single type detection
            if (types.Count == 1)
            {
                var type = types.First();
                return type switch
                {
                    JsonValueKind.String => "String",
                    JsonValueKind.Number => "Int", // Could detect Float based on decimal presence
                    JsonValueKind.True or JsonValueKind.False => "Boolean",
                    JsonValueKind.Object => "String", // Nested objects become JSON strings
                    _ => "String"
                };
            }

            // Multiple types - use String as safe default
            if (types.Contains(JsonValueKind.String))
                return "String";

            if (types.Contains(JsonValueKind.Number))
                return "Int";

            if (types.Contains(JsonValueKind.True) || types.Contains(JsonValueKind.False))
                return "Boolean";

            return "String";
        }

        private static List<Dictionary<string, object?>> ExtractSampleData(List<JsonElement> items)
        {
            var result = new List<Dictionary<string, object?>>();

            foreach (var item in items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var dict = new Dictionary<string, object?>();

                foreach (var prop in item.EnumerateObject())
                {
                    // Skip internal properties
                    if (prop.Name.StartsWith("_") || prop.Name.StartsWith("$"))
                        continue;

                    var sanitizedName = SanitizePropertyName(prop.Name);
                    if (string.IsNullOrEmpty(sanitizedName))
                        continue;

                    dict[sanitizedName] = ConvertJsonElement(prop.Value);
                }

                result.Add(dict);
            }

            return result;
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.Object => element.GetRawText(), // Convert nested objects to JSON string
                _ => element.GetRawText()
            };
        }
    }
}
