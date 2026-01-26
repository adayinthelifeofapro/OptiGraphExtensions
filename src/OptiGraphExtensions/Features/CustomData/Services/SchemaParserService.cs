using System.Text.Json;
using System.Text.Json.Serialization;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Implementation of schema parser service for converting between models and JSON.
    /// </summary>
    public class SchemaParserService : ISchemaParserService
    {
        private static readonly JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        private static readonly JsonSerializerOptions DisplayJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        private static readonly JsonSerializerOptions ParseOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string ModelToApiJson(CreateSchemaRequest request)
        {
            var apiModel = BuildApiModel(request);
            return JsonSerializer.Serialize(apiModel, ApiJsonOptions);
        }

        public string ModelToDisplayJson(CreateSchemaRequest request)
        {
            var apiModel = BuildApiModel(request);
            return JsonSerializer.Serialize(apiModel, DisplayJsonOptions);
        }

        public CreateSchemaRequest JsonToModel(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var request = new CreateSchemaRequest();

            // Parse label
            if (root.TryGetProperty("label", out var labelElement))
            {
                request.Label = labelElement.GetString();
            }

            // Parse languages
            if (root.TryGetProperty("languages", out var languagesElement) &&
                languagesElement.ValueKind == JsonValueKind.Array)
            {
                request.Languages = languagesElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToList();
            }

            // Parse global property types (can be object or array format)
            if (root.TryGetProperty("propertyTypes", out var propTypesElement))
            {
                if (propTypesElement.ValueKind == JsonValueKind.Object)
                {
                    // Object format: { "TypeName": { "type": "..." } }
                    request.PropertyTypes = propTypesElement.EnumerateObject()
                        .Select(prop => new PropertyTypeModel
                        {
                            Name = prop.Name,
                            Type = prop.Value.TryGetProperty("type", out var t) ? t.GetString() ?? "String" : "String"
                        })
                        .ToList();
                }
                else if (propTypesElement.ValueKind == JsonValueKind.Array)
                {
                    // Array format for backwards compatibility
                    request.PropertyTypes = propTypesElement.EnumerateArray()
                        .Select(ParsePropertyType)
                        .Where(p => p != null)
                        .Cast<PropertyTypeModel>()
                        .ToList();
                }
            }

            // Parse content types (can be object or array format)
            if (root.TryGetProperty("contentTypes", out var contentTypesElement))
            {
                if (contentTypesElement.ValueKind == JsonValueKind.Object)
                {
                    // Object format: { "TypeName": { "contentType": [], "properties": {...} } }
                    request.ContentTypes = contentTypesElement.EnumerateObject()
                        .Select(prop => ParseContentTypeFromObject(prop.Name, prop.Value))
                        .Where(c => c != null)
                        .Cast<ContentTypeSchemaModel>()
                        .ToList();
                }
                else if (contentTypesElement.ValueKind == JsonValueKind.Array)
                {
                    // Array format for backwards compatibility
                    request.ContentTypes = contentTypesElement.EnumerateArray()
                        .Select(ParseContentType)
                        .Where(c => c != null)
                        .Cast<ContentTypeSchemaModel>()
                        .ToList();
                }
            }

            return request;
        }

        private static ContentTypeSchemaModel? ParseContentTypeFromObject(string name, JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var contentType = new ContentTypeSchemaModel
            {
                Name = name
            };

            // Parse base type from contentType array
            if (element.TryGetProperty("contentType", out var baseTypeElement) &&
                baseTypeElement.ValueKind == JsonValueKind.Array)
            {
                var baseTypes = baseTypeElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
                contentType.BaseType = baseTypes.FirstOrDefault();
            }

            if (element.TryGetProperty("label", out var labelElement))
            {
                contentType.Label = labelElement.GetString();
            }

            // Parse properties as object format
            if (element.TryGetProperty("properties", out var propertiesElement) &&
                propertiesElement.ValueKind == JsonValueKind.Object)
            {
                contentType.Properties = propertiesElement.EnumerateObject()
                    .Select(prop => new PropertyTypeModel
                    {
                        Name = prop.Name,
                        Type = prop.Value.TryGetProperty("type", out var t) ? t.GetString() ?? "String" : "String",
                        IsSearchable = prop.Value.TryGetProperty("searchable", out var s) && s.ValueKind == JsonValueKind.True,
                        IndexType = prop.Value.TryGetProperty("index", out var i) ? i.GetString() : null
                    })
                    .ToList();
            }

            return contentType;
        }

        public string SourceToDisplayJson(CustomDataSourceModel source)
        {
            var request = new CreateSchemaRequest
            {
                SourceId = source.SourceId,
                Label = source.Label,
                Languages = source.Languages,
                PropertyTypes = source.PropertyTypes,
                ContentTypes = source.ContentTypes
            };

            return ModelToDisplayJson(request);
        }

        public CustomDataSourceModel ResponseToModel(string sourceId, GraphSchemaResponse response)
        {
            return new CustomDataSourceModel
            {
                SourceId = sourceId,
                Label = response.Label,
                Languages = response.Languages ?? new List<string>(),
                PropertyTypes = response.PropertyTypes?
                    .Select(kvp => new PropertyTypeModel
                    {
                        Name = kvp.Key,
                        Type = kvp.Value.Type ?? "String"
                    })
                    .ToList() ?? new List<PropertyTypeModel>(),
                ContentTypes = response.ContentTypes?
                    .Select(kvp => MapContentTypeFromDict(kvp.Key, kvp.Value))
                    .ToList() ?? new List<ContentTypeSchemaModel>()
            };
        }

        private static ContentTypeSchemaModel MapContentTypeFromDict(string name, GraphContentTypeDefinition def)
        {
            return new ContentTypeSchemaModel
            {
                Name = name,
                Label = def.Label,
                BaseType = def.BaseTypes?.FirstOrDefault(),
                Properties = def.Properties?
                    .Select(kvp => new PropertyTypeModel
                    {
                        Name = kvp.Key,
                        Type = kvp.Value.Type ?? "String",
                        IsSearchable = kvp.Value.Searchable ?? false,
                        IndexType = kvp.Value.Index
                    })
                    .ToList() ?? new List<PropertyTypeModel>()
            };
        }

        public bool IsValidSchemaJson(string json, out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON is empty.";
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check for required properties
                if (!root.TryGetProperty("languages", out var languagesElement) ||
                    languagesElement.ValueKind != JsonValueKind.Array ||
                    languagesElement.GetArrayLength() == 0)
                {
                    error = "Schema must include at least one language.";
                    return false;
                }

                // contentTypes can be an object (API format) or array (legacy)
                if (!root.TryGetProperty("contentTypes", out var contentTypesElement))
                {
                    error = "Schema must include at least one content type.";
                    return false;
                }

                bool hasContentTypes = contentTypesElement.ValueKind switch
                {
                    JsonValueKind.Object => contentTypesElement.EnumerateObject().Any(),
                    JsonValueKind.Array => contentTypesElement.GetArrayLength() > 0,
                    _ => false
                };

                if (!hasContentTypes)
                {
                    error = "Schema must include at least one content type.";
                    return false;
                }

                return true;
            }
            catch (JsonException ex)
            {
                error = $"Invalid JSON: {ex.Message}";
                return false;
            }
        }

        public string PropertiesToJson(Dictionary<string, object?> properties)
        {
            return JsonSerializer.Serialize(properties, DisplayJsonOptions);
        }

        public Dictionary<string, object?> JsonToProperties(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, object?>();
            }

            var result = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, ParseOptions)
                ?? new Dictionary<string, object?>();

            // Convert JsonElement values to proper types
            var converted = new Dictionary<string, object?>();
            foreach (var kvp in result)
            {
                converted[kvp.Key] = ConvertJsonElement(kvp.Value);
            }

            return converted;
        }

        private static object BuildApiModel(CreateSchemaRequest request)
        {
            var model = new Dictionary<string, object?>();

            if (!string.IsNullOrEmpty(request.Label))
            {
                model["label"] = request.Label;
            }

            model["languages"] = request.Languages;

            // propertyTypes is an object with type names as keys
            if (request.PropertyTypes != null && request.PropertyTypes.Any())
            {
                var propertyTypesObj = new Dictionary<string, object>();
                foreach (var pt in request.PropertyTypes)
                {
                    var propsObj = new Dictionary<string, object>();
                    // For custom property types, we'd need nested properties
                    // For now, treat as simple type definition
                    propertyTypesObj[pt.Name] = new Dictionary<string, object?>
                    {
                        ["type"] = pt.Type
                    }.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);
                }
                model["propertyTypes"] = propertyTypesObj;
            }

            // contentTypes is an object with content type names as keys
            var contentTypesObj = new Dictionary<string, object>();
            foreach (var ct in request.ContentTypes)
            {
                // Build properties as an object with property names as keys
                var propertiesObj = new Dictionary<string, object>();
                if (ct.Properties != null)
                {
                    foreach (var prop in ct.Properties)
                    {
                        var propDef = new Dictionary<string, object?>
                        {
                            ["type"] = prop.Type
                        };
                        // Note: searchable functionality disabled due to Optimizely Graph issues
                        // where searchable fields return null values. Can be re-enabled when resolved.
                        if (!string.IsNullOrEmpty(prop.IndexType))
                        {
                            propDef["index"] = prop.IndexType;
                        }
                        propertiesObj[prop.Name] = propDef.Where(kv => kv.Value != null)
                            .ToDictionary(kv => kv.Key, kv => kv.Value);
                    }
                }

                // Build content type definition
                var ctDef = new Dictionary<string, object?>
                {
                    // contentType is an array for base types/inheritance
                    ["contentType"] = !string.IsNullOrEmpty(ct.BaseType)
                        ? new List<string> { ct.BaseType }
                        : new List<string>(),
                    ["properties"] = propertiesObj
                };

                contentTypesObj[ct.Name] = ctDef;
            }
            model["contentTypes"] = contentTypesObj;

            return model;
        }

        private static PropertyTypeModel? ParsePropertyType(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var property = new PropertyTypeModel();

            if (element.TryGetProperty("name", out var nameElement))
            {
                property.Name = nameElement.GetString() ?? string.Empty;
            }

            if (element.TryGetProperty("type", out var typeElement))
            {
                property.Type = typeElement.GetString() ?? "String";
            }

            if (element.TryGetProperty("searchable", out var searchableElement))
            {
                property.IsSearchable = searchableElement.ValueKind == JsonValueKind.True;
            }

            if (element.TryGetProperty("index", out var indexElement))
            {
                property.IndexType = indexElement.GetString();
            }

            return property;
        }

        private static ContentTypeSchemaModel? ParseContentType(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var contentType = new ContentTypeSchemaModel();

            // Check for "contentType" first (API format), then fall back to "name" (display format)
            if (element.TryGetProperty("contentType", out var contentTypeElement))
            {
                contentType.Name = contentTypeElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("name", out var nameElement))
            {
                contentType.Name = nameElement.GetString() ?? string.Empty;
            }

            if (element.TryGetProperty("label", out var labelElement))
            {
                contentType.Label = labelElement.GetString();
            }

            if (element.TryGetProperty("base", out var baseElement))
            {
                contentType.BaseType = baseElement.GetString();
            }

            if (element.TryGetProperty("properties", out var propertiesElement) &&
                propertiesElement.ValueKind == JsonValueKind.Array)
            {
                contentType.Properties = propertiesElement.EnumerateArray()
                    .Select(ParsePropertyType)
                    .Where(p => p != null)
                    .Cast<PropertyTypeModel>()
                    .ToList();
            }

            return contentType;
        }

        private static PropertyTypeModel MapPropertyType(GraphPropertyTypeResponse response)
        {
            return new PropertyTypeModel
            {
                Name = response.Name ?? string.Empty,
                Type = response.Type ?? "String",
                IsSearchable = response.Searchable ?? false,
                IndexType = response.Index
            };
        }

        private static ContentTypeSchemaModel MapContentType(GraphContentTypeResponse response)
        {
            return new ContentTypeSchemaModel
            {
                Name = response.EffectiveName ?? string.Empty,  // Use EffectiveName (contentType or name)
                Label = response.Label,
                BaseType = response.Base,
                Properties = response.Properties?
                    .Select(MapPropertyType)
                    .ToList() ?? new List<PropertyTypeModel>()
            };
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
