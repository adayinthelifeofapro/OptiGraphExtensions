using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for converting between schema models and JSON format.
    /// </summary>
    public interface ISchemaParserService
    {
        /// <summary>
        /// Converts a CreateSchemaRequest model to JSON for the Graph API.
        /// </summary>
        /// <param name="request">The request model.</param>
        /// <returns>JSON string formatted for the Graph API.</returns>
        string ModelToApiJson(CreateSchemaRequest request);

        /// <summary>
        /// Converts a CreateSchemaRequest model to pretty-printed JSON for display.
        /// </summary>
        /// <param name="request">The request model.</param>
        /// <returns>Pretty-printed JSON string.</returns>
        string ModelToDisplayJson(CreateSchemaRequest request);

        /// <summary>
        /// Parses JSON into a CreateSchemaRequest model.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>Parsed request model.</returns>
        CreateSchemaRequest JsonToModel(string json);

        /// <summary>
        /// Converts a CustomDataSourceModel to JSON for display.
        /// </summary>
        /// <param name="source">The source model.</param>
        /// <returns>Pretty-printed JSON string.</returns>
        string SourceToDisplayJson(CustomDataSourceModel source);

        /// <summary>
        /// Converts a Graph API response to a CustomDataSourceModel.
        /// </summary>
        /// <param name="sourceId">The source ID.</param>
        /// <param name="response">The API response.</param>
        /// <returns>The mapped source model.</returns>
        CustomDataSourceModel ResponseToModel(string sourceId, GraphSchemaResponse response);

        /// <summary>
        /// Validates that a JSON string is valid schema JSON.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        /// <param name="error">Error message if invalid.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool IsValidSchemaJson(string json, out string? error);

        /// <summary>
        /// Converts data item properties to JSON for the Graph API.
        /// </summary>
        /// <param name="properties">The properties dictionary.</param>
        /// <returns>JSON string.</returns>
        string PropertiesToJson(Dictionary<string, object?> properties);

        /// <summary>
        /// Parses JSON into a properties dictionary.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>Properties dictionary.</returns>
        Dictionary<string, object?> JsonToProperties(string json);
    }
}
