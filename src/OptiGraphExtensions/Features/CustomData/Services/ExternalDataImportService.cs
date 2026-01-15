using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Service for importing data from external APIs into custom data sources.
    /// </summary>
    public class ExternalDataImportService : IExternalDataImportService
    {
        private readonly HttpClient _httpClient;
        private readonly ICustomDataService _customDataService;
        private readonly INdJsonBuilderService _ndJsonBuilder;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ExternalDataImportService(
            HttpClient httpClient,
            ICustomDataService customDataService,
            INdJsonBuilderService ndJsonBuilder)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _customDataService = customDataService ?? throw new ArgumentNullException(nameof(customDataService));
            _ndJsonBuilder = ndJsonBuilder ?? throw new ArgumentNullException(nameof(ndJsonBuilder));
        }

        public async Task<(bool Success, string Message, string? SampleJson)> TestConnectionAsync(
            ImportConfigurationModel config)
        {
            try
            {
                // Debug: Log received values
                System.Diagnostics.Debug.WriteLine($"[ExternalDataImportService.TestConnection] ApiUrl: '{config.ApiUrl}'");
                System.Diagnostics.Debug.WriteLine($"[ExternalDataImportService.TestConnection] JsonPath: '{config.JsonPath}'");

                // Validate JsonPath doesn't look like a URL (common mistake)
                if (!string.IsNullOrWhiteSpace(config.JsonPath) &&
                    (config.JsonPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     config.JsonPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, "JSON Path should be the path to the data array within the JSON response (e.g., 'products', 'data', 'results'), not a URL. The API URL should be entered in the 'API URL' field.", null);
                }

                using var request = BuildHttpRequest(config);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"API returned {(int)response.StatusCode} {response.StatusCode}: {TruncateString(errorContent, 200)}", null);
                }

                var content = await response.Content.ReadAsStringAsync();

                if (!IsValidJsonArray(content, config.JsonPath))
                {
                    var pathHint = string.IsNullOrWhiteSpace(config.JsonPath)
                        ? "The API must return a JSON array at the root level, or specify a JSON Path to navigate to the array (e.g., 'data', 'results', 'items')."
                        : $"Could not find a JSON array at path '{config.JsonPath}'. Check that the path is correct.";
                    return (false, pathHint, TruncateString(content, 500));
                }

                var arrayJson = ExtractJsonArrayAtPath(content, config.JsonPath) ?? content;
                var sample = TruncateJsonForPreview(arrayJson, maxItems: 2);
                return (true, "Connection successful", sample);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Connection failed: {ex.Message}", null);
            }
            catch (TaskCanceledException)
            {
                return (false, "Connection timed out", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string? JsonData, string? ErrorMessage)> FetchExternalDataAsync(
            ImportConfigurationModel config)
        {
            try
            {
                using var request = BuildHttpRequest(config);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, null, $"API returned {(int)response.StatusCode}: {TruncateString(errorContent, 200)}");
                }

                var content = await response.Content.ReadAsStringAsync();

                if (!IsValidJsonArray(content, config.JsonPath))
                {
                    var pathHint = string.IsNullOrWhiteSpace(config.JsonPath)
                        ? "Response is not a valid JSON array at root level. Specify a JSON Path to navigate to the array."
                        : $"Could not find a JSON array at path '{config.JsonPath}'.";
                    return (false, content, pathHint);
                }

                // Extract the array at the specified path
                var arrayJson = ExtractJsonArrayAtPath(content, config.JsonPath) ?? content;
                return (true, arrayJson, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<(IEnumerable<CustomDataItemModel> Items, List<string> Warnings)> PreviewImportAsync(
            ImportConfigurationModel config,
            ContentTypeSchemaModel schema)
        {
            var warnings = new List<string>();

            var (success, jsonData, errorMessage) = await FetchExternalDataAsync(config);

            if (!success || string.IsNullOrEmpty(jsonData))
            {
                warnings.Add(errorMessage ?? "Failed to fetch data from external API");
                return (Enumerable.Empty<CustomDataItemModel>(), warnings);
            }

            var items = MapExternalDataToItems(jsonData, config, out var mappingWarnings);
            warnings.AddRange(mappingWarnings);

            return (items, warnings);
        }

        public async Task<ImportResult> ExecuteImportAsync(
            ImportConfigurationModel config,
            ContentTypeSchemaModel schema,
            string sourceId)
        {
            var stopwatch = Stopwatch.StartNew();
            var debugInfo = new StringBuilder();

            try
            {
                debugInfo.AppendLine($"=== IMPORT DEBUG INFO ===");
                debugInfo.AppendLine($"Source ID: {sourceId}");
                debugInfo.AppendLine($"Content Type: {config.TargetContentType}");
                debugInfo.AppendLine($"API URL: {config.ApiUrl}");
                debugInfo.AppendLine($"Started at: {DateTime.UtcNow:O}");
                debugInfo.AppendLine();

                // Fetch data from external API
                debugInfo.AppendLine("Fetching data from external API...");
                var (success, jsonData, errorMessage) = await FetchExternalDataAsync(config);

                if (!success || string.IsNullOrEmpty(jsonData))
                {
                    stopwatch.Stop();
                    var failedResult = ImportResult.Failed(errorMessage ?? "Failed to fetch data from external API", stopwatch.Elapsed);
                    failedResult.DebugInfo = debugInfo.ToString();
                    return failedResult;
                }

                debugInfo.AppendLine($"Received {jsonData.Length} bytes of JSON data");
                debugInfo.AppendLine();

                // Map external data to items
                debugInfo.AppendLine("Mapping external data to items...");
                var items = MapExternalDataToItems(jsonData, config, out var warnings).ToList();

                debugInfo.AppendLine($"Mapped {items.Count} items");
                if (warnings.Any())
                {
                    debugInfo.AppendLine($"Warnings: {string.Join("; ", warnings)}");
                }
                debugInfo.AppendLine();

                if (!items.Any())
                {
                    stopwatch.Stop();
                    return new ImportResult
                    {
                        Success = false,
                        TotalItemsReceived = 0,
                        ItemsImported = 0,
                        Duration = stopwatch.Elapsed,
                        Errors = new List<string> { "No items could be mapped from the external data" },
                        Warnings = warnings,
                        DebugInfo = debugInfo.ToString()
                    };
                }

                // Build NdJSON
                debugInfo.AppendLine("Building NdJSON payload...");
                var ndJson = _ndJsonBuilder.BuildNdJson(items);
                debugInfo.AppendLine($"NdJSON payload size: {ndJson.Length} bytes");
                debugInfo.AppendLine();

                // Sync to Graph
                debugInfo.AppendLine("Syncing to Optimizely Graph...");
                var syncRequest = new SyncDataRequest
                {
                    SourceId = sourceId,
                    Items = items,
                    JobId = Guid.NewGuid().ToString()
                };

                var syncResponse = await _customDataService.SyncDataAsync(syncRequest);
                debugInfo.AppendLine($"Sync response: {syncResponse}");

                stopwatch.Stop();

                return new ImportResult
                {
                    Success = true,
                    TotalItemsReceived = items.Count,
                    ItemsImported = items.Count,
                    ItemsSkipped = 0,
                    ItemsFailed = 0,
                    Duration = stopwatch.Elapsed,
                    Warnings = warnings,
                    DebugInfo = debugInfo.ToString()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                debugInfo.AppendLine($"ERROR: {ex.Message}");
                debugInfo.AppendLine($"Stack trace: {ex.StackTrace}");

                var errorResult = ImportResult.Failed(ex.Message, stopwatch.Elapsed);
                errorResult.DebugInfo = debugInfo.ToString();
                return errorResult;
            }
        }

        public IEnumerable<CustomDataItemModel> MapExternalDataToItems(
            string jsonData,
            ImportConfigurationModel config,
            out List<string> warnings)
        {
            warnings = new List<string>();
            var items = new List<CustomDataItemModel>();

            try
            {
                using var doc = JsonDocument.Parse(jsonData);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    warnings.Add("JSON response is not an array at root level");
                    return items;
                }

                var index = 0;
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    index++;
                    try
                    {
                        var item = MapSingleItem(element, config, index, warnings);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Item {index}: Failed to map - {ex.Message}");
                    }
                }

                return items;
            }
            catch (JsonException ex)
            {
                warnings.Add($"Invalid JSON: {ex.Message}");
                return items;
            }
        }

        private CustomDataItemModel? MapSingleItem(
            JsonElement element,
            ImportConfigurationModel config,
            int index,
            List<string> warnings)
        {
            // Extract ID field
            var idValue = GetValueFromPath(element, config.IdFieldMapping);
            if (idValue == null)
            {
                warnings.Add($"Item {index}: Skipped - ID field '{config.IdFieldMapping}' not found or null");
                return null;
            }

            var item = new CustomDataItemModel
            {
                Id = idValue.ToString()!,
                ContentType = config.TargetContentType,
                LanguageRouting = config.LanguageRouting,
                Properties = new Dictionary<string, object?>()
            };

            // Map each configured field
            foreach (var mapping in config.FieldMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.SourcePath) || string.IsNullOrWhiteSpace(mapping.TargetProperty))
                {
                    continue;
                }

                var value = GetValueFromPath(element, mapping.SourcePath);
                var transformedValue = ApplyTransformation(value, mapping.Transformation, mapping.DefaultValue);
                item.Properties[mapping.TargetProperty] = transformedValue;
            }

            return item;
        }

        private static object? GetValueFromPath(JsonElement element, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            // Support dot notation: "data.product.name"
            var segments = path.Split('.');
            JsonElement current = element;

            foreach (var segment in segments)
            {
                if (current.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                // Try exact match first, then case-insensitive match
                if (!current.TryGetProperty(segment, out var next))
                {
                    // Case-insensitive fallback
                    var found = false;
                    foreach (var prop in current.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, segment, StringComparison.OrdinalIgnoreCase))
                        {
                            next = prop.Value;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return null;
                    }
                }

                current = next;
            }

            return ConvertJsonElement(current);
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.Object => element.GetRawText(),
                _ => null
            };
        }

        private static object? ApplyTransformation(object? value, FieldTransformation transformation, string? defaultValue)
        {
            if (value == null)
            {
                return ParseDefaultValue(defaultValue, transformation);
            }

            return transformation switch
            {
                FieldTransformation.None => value,
                FieldTransformation.ToString => value.ToString(),
                FieldTransformation.ToInt => ConvertToInt(value),
                FieldTransformation.ToFloat => ConvertToFloat(value),
                FieldTransformation.ToBoolean => ConvertToBoolean(value),
                FieldTransformation.ToDate => ConvertToDate(value),
                FieldTransformation.ToDateTime => ConvertToDateTime(value),
                _ => value
            };
        }

        private static object? ParseDefaultValue(string? defaultValue, FieldTransformation transformation)
        {
            if (string.IsNullOrEmpty(defaultValue))
            {
                return null;
            }

            return transformation switch
            {
                FieldTransformation.ToInt when int.TryParse(defaultValue, out var i) => i,
                FieldTransformation.ToFloat when double.TryParse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
                FieldTransformation.ToBoolean when bool.TryParse(defaultValue, out var b) => b,
                _ => defaultValue
            };
        }

        private static int? ConvertToInt(object value)
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            if (int.TryParse(value.ToString(), out var result)) return result;
            return null;
        }

        private static double? ConvertToFloat(object value)
        {
            if (value is double d) return d;
            if (value is int i) return i;
            if (value is long l) return l;
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)) return result;
            return null;
        }

        private static bool? ConvertToBoolean(object value)
        {
            if (value is bool b) return b;
            var str = value.ToString()?.ToLowerInvariant();
            return str switch
            {
                "true" or "1" or "yes" => true,
                "false" or "0" or "no" => false,
                _ => null
            };
        }

        private static string? ConvertToDate(object value)
        {
            if (value is string s && DateTime.TryParse(s, out var dt))
            {
                return dt.ToString("yyyy-MM-dd");
            }
            return value.ToString();
        }

        private static string? ConvertToDateTime(object value)
        {
            if (value is string s && DateTime.TryParse(s, out var dt))
            {
                return dt.ToString("O");
            }
            return value.ToString();
        }

        private HttpRequestMessage BuildHttpRequest(ImportConfigurationModel config)
        {
            var request = new HttpRequestMessage(
                new HttpMethod(config.HttpMethod),
                config.ApiUrl);

            // Apply authentication
            switch (config.AuthType)
            {
                case AuthenticationType.ApiKey:
                    var headerName = string.IsNullOrWhiteSpace(config.AuthKeyOrUsername)
                        ? "X-API-Key"
                        : config.AuthKeyOrUsername;
                    request.Headers.TryAddWithoutValidation(headerName, config.AuthValueOrPassword);
                    break;

                case AuthenticationType.Basic:
                    if (!string.IsNullOrEmpty(config.AuthKeyOrUsername))
                    {
                        var basicAuth = Convert.ToBase64String(
                            Encoding.UTF8.GetBytes($"{config.AuthKeyOrUsername}:{config.AuthValueOrPassword}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
                    }
                    break;

                case AuthenticationType.Bearer:
                    if (!string.IsNullOrEmpty(config.AuthValueOrPassword))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthValueOrPassword);
                    }
                    break;
            }

            // Add custom headers
            foreach (var header in config.CustomHeaders)
            {
                if (!string.IsNullOrWhiteSpace(header.Key))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return request;
        }

        private static bool IsValidJsonArray(string json, string? jsonPath = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var element = NavigateToJsonPath(doc.RootElement, jsonPath);
                return element?.ValueKind == JsonValueKind.Array;
            }
            catch
            {
                return false;
            }
        }

        private static JsonElement? NavigateToJsonPath(JsonElement root, string? jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                return root;
            }

            var segments = jsonPath.Split(new[] { '.', '/' }, StringSplitOptions.RemoveEmptyEntries);
            JsonElement current = root;

            foreach (var segment in segments)
            {
                // Handle array index notation (e.g., "items[0]")
                var match = System.Text.RegularExpressions.Regex.Match(segment, @"^(\w+)\[(\d+)\]$");
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

        private static string? ExtractJsonArrayAtPath(string json, string? jsonPath)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var element = NavigateToJsonPath(doc.RootElement, jsonPath);

                if (element == null || element.Value.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                return element.Value.GetRawText();
            }
            catch
            {
                return null;
            }
        }

        private static string TruncateJsonForPreview(string json, int maxItems)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return TruncateString(json, 1000);
                }

                var items = doc.RootElement.EnumerateArray().Take(maxItems).ToList();

                return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return TruncateString(json, 1000);
            }
        }

        private static string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
            {
                return str;
            }

            return str[..maxLength] + "...";
        }
    }
}
