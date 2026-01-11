using OptiGraphExtensions.Features.QueryLibrary.Models;

namespace OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions
{
    /// <summary>
    /// Service for handling raw GraphQL queries.
    /// </summary>
    public interface IRawQueryService
    {
        /// <summary>
        /// Validates a raw GraphQL query.
        /// </summary>
        QueryValidationResult ValidateQuery(string rawQuery);

        /// <summary>
        /// Parses query variables from JSON string.
        /// </summary>
        Dictionary<string, object>? ParseVariables(string? variablesJson);

        /// <summary>
        /// Checks if the query supports cursor-based pagination.
        /// </summary>
        bool SupportsPagination(string rawQuery);

        /// <summary>
        /// Injects pagination variables into the query if missing.
        /// </summary>
        (string query, Dictionary<string, object> variables) InjectPaginationSupport(
            string rawQuery,
            Dictionary<string, object>? existingVariables,
            int pageSize,
            string? cursor);
    }
}
