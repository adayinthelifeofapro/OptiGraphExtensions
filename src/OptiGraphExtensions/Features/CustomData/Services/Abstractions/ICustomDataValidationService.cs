using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Validation result for custom data operations.
    /// </summary>
    public class CustomDataValidationResult
    {
        /// <summary>
        /// Whether the validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Validation warnings (non-blocking).
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static CustomDataValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        public static CustomDataValidationResult Failure(string error) =>
            new() { IsValid = false, Errors = new List<string> { error } };

        /// <summary>
        /// Creates a failed validation result with multiple error messages.
        /// </summary>
        public static CustomDataValidationResult Failure(IEnumerable<string> errors) =>
            new() { IsValid = false, Errors = errors.ToList() };

        /// <summary>
        /// Creates a successful validation result with warnings.
        /// </summary>
        public static CustomDataValidationResult SuccessWithWarnings(IEnumerable<string> warnings) =>
            new() { IsValid = true, Warnings = warnings.ToList() };
    }

    /// <summary>
    /// Service for validating custom data schemas and items.
    /// </summary>
    public interface ICustomDataValidationService
    {
        /// <summary>
        /// Validates a source ID.
        /// </summary>
        /// <param name="sourceId">The source ID to validate.</param>
        CustomDataValidationResult ValidateSourceId(string sourceId);

        /// <summary>
        /// Validates a schema creation/update request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        CustomDataValidationResult ValidateSchema(CreateSchemaRequest request);

        /// <summary>
        /// Validates a content type definition.
        /// </summary>
        /// <param name="contentType">The content type to validate.</param>
        CustomDataValidationResult ValidateContentType(ContentTypeSchemaModel contentType);

        /// <summary>
        /// Validates a property type definition.
        /// </summary>
        /// <param name="property">The property to validate.</param>
        CustomDataValidationResult ValidateProperty(PropertyTypeModel property);

        /// <summary>
        /// Validates a data item against its schema.
        /// </summary>
        /// <param name="item">The item to validate.</param>
        /// <param name="schema">The schema to validate against (optional).</param>
        CustomDataValidationResult ValidateDataItem(CustomDataItemModel item, ContentTypeSchemaModel? schema = null);

        /// <summary>
        /// Validates a sync data request.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        CustomDataValidationResult ValidateSyncRequest(SyncDataRequest request);

        /// <summary>
        /// Checks if a full sync operation should show a warning.
        /// </summary>
        /// <param name="sourceId">The source ID.</param>
        /// <param name="hasExistingData">Whether the source has existing data.</param>
        CustomDataValidationResult ValidateFullSyncWarning(string sourceId, bool hasExistingData);
    }
}
