using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Implementation of validation service for custom data operations.
    /// </summary>
    public class CustomDataValidationService : ICustomDataValidationService
    {
        private static readonly Regex SourceIdRegex = new(@"^[a-z0-9]{1,4}$", RegexOptions.Compiled);
        private static readonly Regex IdentifierRegex = new(@"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private static readonly HashSet<string> ValidPropertyTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "String", "Int", "Float", "Boolean", "Date", "DateTime",
            "StringArray", "IntArray", "FloatArray"
        };

        public CustomDataValidationResult ValidateSourceId(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return CustomDataValidationResult.Failure("Source ID is required.");
            }

            if (!SourceIdRegex.IsMatch(sourceId))
            {
                return CustomDataValidationResult.Failure(
                    "Source ID must be 1-4 lowercase letters and/or numbers.");
            }

            return CustomDataValidationResult.Success();
        }

        public CustomDataValidationResult ValidateSchema(CreateSchemaRequest request)
        {
            var errors = new List<string>();

            // Validate source ID
            var sourceIdResult = ValidateSourceId(request.SourceId);
            if (!sourceIdResult.IsValid)
            {
                errors.AddRange(sourceIdResult.Errors);
            }

            // Validate languages
            if (request.Languages == null || !request.Languages.Any())
            {
                errors.Add("At least one language is required.");
            }
            else
            {
                foreach (var lang in request.Languages)
                {
                    if (string.IsNullOrWhiteSpace(lang))
                    {
                        errors.Add("Language values cannot be empty.");
                        break;
                    }
                }
            }

            // Validate content types
            if (request.ContentTypes == null || !request.ContentTypes.Any())
            {
                errors.Add("At least one content type is required.");
            }
            else
            {
                var contentTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var contentType in request.ContentTypes)
                {
                    var ctResult = ValidateContentType(contentType);
                    if (!ctResult.IsValid)
                    {
                        errors.AddRange(ctResult.Errors.Select(e => $"Content type '{contentType.Name}': {e}"));
                    }

                    // Check for duplicate names
                    if (!string.IsNullOrEmpty(contentType.Name) && !contentTypeNames.Add(contentType.Name))
                    {
                        errors.Add($"Duplicate content type name: '{contentType.Name}'.");
                    }
                }
            }

            // Validate global property types (if any)
            if (request.PropertyTypes != null)
            {
                var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in request.PropertyTypes)
                {
                    var propResult = ValidateProperty(property);
                    if (!propResult.IsValid)
                    {
                        errors.AddRange(propResult.Errors.Select(e => $"Global property '{property.Name}': {e}"));
                    }

                    if (!string.IsNullOrEmpty(property.Name) && !propertyNames.Add(property.Name))
                    {
                        errors.Add($"Duplicate global property name: '{property.Name}'.");
                    }
                }
            }

            return errors.Any()
                ? CustomDataValidationResult.Failure(errors)
                : CustomDataValidationResult.Success();
        }

        public CustomDataValidationResult ValidateContentType(ContentTypeSchemaModel contentType)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(contentType.Name))
            {
                errors.Add("Content type name is required.");
            }
            else if (!IdentifierRegex.IsMatch(contentType.Name))
            {
                errors.Add("Content type name must start with a letter and contain only letters, numbers, and underscores.");
            }

            // Validate properties
            if (contentType.Properties != null && contentType.Properties.Any())
            {
                var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in contentType.Properties)
                {
                    var propResult = ValidateProperty(property);
                    if (!propResult.IsValid)
                    {
                        errors.AddRange(propResult.Errors.Select(e => $"Property '{property.Name}': {e}"));
                    }

                    if (!string.IsNullOrEmpty(property.Name) && !propertyNames.Add(property.Name))
                    {
                        errors.Add($"Duplicate property name: '{property.Name}'.");
                    }
                }
            }

            return errors.Any()
                ? CustomDataValidationResult.Failure(errors)
                : CustomDataValidationResult.Success();
        }

        public CustomDataValidationResult ValidateProperty(PropertyTypeModel property)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(property.Name))
            {
                errors.Add("Property name is required.");
            }
            else if (!IdentifierRegex.IsMatch(property.Name))
            {
                errors.Add("Property name must start with a letter and contain only letters, numbers, and underscores.");
            }

            if (string.IsNullOrWhiteSpace(property.Type))
            {
                errors.Add("Property type is required.");
            }
            else if (!ValidPropertyTypes.Contains(property.Type))
            {
                errors.Add($"Invalid property type '{property.Type}'. Valid types: {string.Join(", ", ValidPropertyTypes)}.");
            }

            return errors.Any()
                ? CustomDataValidationResult.Failure(errors)
                : CustomDataValidationResult.Success();
        }

        public CustomDataValidationResult ValidateDataItem(CustomDataItemModel item, ContentTypeSchemaModel? schema = null)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                errors.Add("Item ID is required.");
            }

            if (string.IsNullOrWhiteSpace(item.ContentType))
            {
                errors.Add("Content type is required.");
            }

            // Validate against schema if provided
            if (schema != null && schema.Properties != null)
            {
                foreach (var property in schema.Properties.Where(p => p.IsRequired))
                {
                    if (!item.Properties.ContainsKey(property.Name) ||
                        item.Properties[property.Name] == null ||
                        (item.Properties[property.Name] is string s && string.IsNullOrWhiteSpace(s)))
                    {
                        errors.Add($"Required property '{property.Name}' is missing or empty.");
                    }
                }
            }

            return errors.Any()
                ? CustomDataValidationResult.Failure(errors)
                : CustomDataValidationResult.Success();
        }

        public CustomDataValidationResult ValidateSyncRequest(SyncDataRequest request)
        {
            var errors = new List<string>();

            var sourceIdResult = ValidateSourceId(request.SourceId);
            if (!sourceIdResult.IsValid)
            {
                errors.AddRange(sourceIdResult.Errors);
            }

            if (request.Items == null || !request.Items.Any())
            {
                errors.Add("At least one item is required for sync.");
            }
            else
            {
                var itemIds = new HashSet<string>();
                for (int i = 0; i < request.Items.Count; i++)
                {
                    var item = request.Items[i];
                    var itemResult = ValidateDataItem(item);
                    if (!itemResult.IsValid)
                    {
                        errors.AddRange(itemResult.Errors.Select(e => $"Item {i + 1}: {e}"));
                    }

                    if (!string.IsNullOrEmpty(item.Id) && !itemIds.Add(item.Id))
                    {
                        errors.Add($"Item {i + 1}: Duplicate item ID '{item.Id}'.");
                    }
                }
            }

            return errors.Any()
                ? CustomDataValidationResult.Failure(errors)
                : CustomDataValidationResult.Success();
        }

        public CustomDataValidationResult ValidateFullSyncWarning(string sourceId, bool hasExistingData)
        {
            if (hasExistingData)
            {
                return CustomDataValidationResult.SuccessWithWarnings(new[]
                {
                    $"Full sync will delete ALL existing data in source '{sourceId}'. This action cannot be undone."
                });
            }

            return CustomDataValidationResult.Success();
        }
    }
}
