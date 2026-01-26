using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for importing data from external APIs into custom data sources.
    /// </summary>
    public interface IExternalDataImportService
    {
        /// <summary>
        /// Tests connection to an external API without importing data.
        /// Returns success status, message, and sample JSON data.
        /// </summary>
        Task<(bool Success, string Message, string? SampleJson)> TestConnectionAsync(
            ImportConfigurationModel config);

        /// <summary>
        /// Fetches data from external API and returns raw JSON for preview.
        /// </summary>
        Task<(bool Success, string? JsonData, string? ErrorMessage)> FetchExternalDataAsync(
            ImportConfigurationModel config);

        /// <summary>
        /// Previews the mapped data without syncing to Graph.
        /// Returns the mapped items and any warnings encountered.
        /// </summary>
        Task<(IEnumerable<CustomDataItemModel> Items, List<string> Warnings)> PreviewImportAsync(
            ImportConfigurationModel config,
            ContentTypeSchemaModel schema);

        /// <summary>
        /// Executes the full import: fetch, map, and sync to Graph.
        /// </summary>
        Task<ImportResult> ExecuteImportAsync(
            ImportConfigurationModel config,
            ContentTypeSchemaModel schema,
            string sourceId);

        /// <summary>
        /// Parses a JSON array response and maps to CustomDataItemModels using field mappings.
        /// </summary>
        IEnumerable<CustomDataItemModel> MapExternalDataToItems(
            string jsonData,
            ImportConfigurationModel config,
            out List<string> warnings);
    }
}
