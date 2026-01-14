using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Implementation of NdJSON builder service for custom data synchronization.
    /// NdJSON (Newline Delimited JSON) is the format required by Optimizely Graph data API.
    /// </summary>
    public class NdJsonBuilderService : INdJsonBuilderService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        private static readonly JsonSerializerOptions ParseOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string BuildNdJson(IEnumerable<CustomDataItemModel> items)
        {
            var sb = new StringBuilder();

            foreach (var item in items)
            {
                // Action line (index operation)
                var actionLine = BuildIndexActionLine(item.Id, item.LanguageRouting);
                sb.AppendLine(actionLine);

                // Data line (the actual data)
                var dataLine = BuildDataLine(item);
                sb.AppendLine(dataLine);
            }

            return sb.ToString();
        }

        public IEnumerable<CustomDataItemModel> ParseNdJson(string ndJson)
        {
            if (string.IsNullOrWhiteSpace(ndJson))
            {
                yield break;
            }

            var lines = ndJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length - 1; i += 2)
            {
                var actionLine = lines[i].Trim();
                var dataLine = lines[i + 1].Trim();

                if (string.IsNullOrEmpty(actionLine) || string.IsNullOrEmpty(dataLine))
                {
                    continue;
                }

                var item = ParseItemFromLines(actionLine, dataLine);
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        public string BuildIndexActionLine(string itemId, string? languageRouting = null)
        {
            var indexData = new Dictionary<string, object>
            {
                ["_id"] = itemId
            };

            if (!string.IsNullOrWhiteSpace(languageRouting))
            {
                indexData["language_routing"] = languageRouting;
            }

            var action = new Dictionary<string, object>
            {
                ["index"] = indexData
            };

            return JsonSerializer.Serialize(action, JsonOptions);
        }

        public string BuildDeleteActionLine(string itemId)
        {
            var action = new Dictionary<string, object>
            {
                ["delete"] = new Dictionary<string, object>
                {
                    ["_id"] = itemId
                }
            };

            return JsonSerializer.Serialize(action, JsonOptions);
        }

        public bool IsValidNdJson(string ndJson)
        {
            if (string.IsNullOrWhiteSpace(ndJson))
            {
                return false;
            }

            var lines = ndJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Must have even number of non-empty lines (action + data pairs)
            if (lines.Length == 0 || lines.Length % 2 != 0)
            {
                return false;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    using var doc = JsonDocument.Parse(lines[i].Trim());

                    // Action lines (even indices) should have "index" or "delete"
                    if (i % 2 == 0)
                    {
                        var hasAction = doc.RootElement.TryGetProperty("index", out _) ||
                                       doc.RootElement.TryGetProperty("delete", out _);
                        if (!hasAction)
                        {
                            return false;
                        }
                    }
                }
                catch (JsonException)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetItemCount(string ndJson)
        {
            if (string.IsNullOrWhiteSpace(ndJson))
            {
                return 0;
            }

            var lines = ndJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length / 2;
        }

        private static string BuildDataLine(CustomDataItemModel item)
        {
            // Start with properties
            var data = new Dictionary<string, object?>(item.Properties);

            // ContentType array is required by Optimizely Graph
            if (!string.IsNullOrEmpty(item.ContentType) && !data.ContainsKey("ContentType"))
            {
                data["ContentType"] = new[] { item.ContentType };
            }

            // Status is required - default to Published
            if (!data.ContainsKey("Status"))
            {
                data["Status"] = "Published";
            }

            // RolesWithReadAccess is required - default to Everyone
            if (!data.ContainsKey("RolesWithReadAccess"))
            {
                data["RolesWithReadAccess"] = "Everyone";
            }

            return JsonSerializer.Serialize(data, JsonOptions);
        }

        private static CustomDataItemModel? ParseItemFromLines(string actionLine, string dataLine)
        {
            try
            {
                using var actionDoc = JsonDocument.Parse(actionLine);

                string? itemId = null;
                string? languageRouting = null;

                // Try to get from "index" action
                if (actionDoc.RootElement.TryGetProperty("index", out var indexElement))
                {
                    if (indexElement.TryGetProperty("_id", out var idElement))
                    {
                        itemId = idElement.ValueKind == JsonValueKind.String
                            ? idElement.GetString()
                            : idElement.GetRawText();
                    }

                    if (indexElement.TryGetProperty("language_routing", out var langElement))
                    {
                        languageRouting = langElement.GetString();
                    }
                }

                if (string.IsNullOrEmpty(itemId))
                {
                    return null;
                }

                // Parse data line
                var properties = JsonSerializer.Deserialize<Dictionary<string, object?>>(dataLine, ParseOptions)
                    ?? new Dictionary<string, object?>();

                // Extract content type from _type if present
                string? contentType = null;
                if (properties.TryGetValue("_type", out var typeValue) && typeValue != null)
                {
                    contentType = typeValue.ToString();
                    properties.Remove("_type");
                }

                // Convert JsonElement values to proper types
                var convertedProperties = new Dictionary<string, object?>();
                foreach (var kvp in properties)
                {
                    convertedProperties[kvp.Key] = ConvertJsonElement(kvp.Value);
                }

                return new CustomDataItemModel
                {
                    Id = itemId,
                    ContentType = contentType ?? string.Empty,
                    LanguageRouting = languageRouting,
                    Properties = convertedProperties
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static object? ConvertJsonElement(object? value)
        {
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.Array => element.EnumerateArray()
                        .Select(e => ConvertJsonElement(e))
                        .ToList(),
                    JsonValueKind.Object => element.EnumerateObject()
                        .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                    _ => element.GetRawText()
                };
            }

            return value;
        }
    }
}
