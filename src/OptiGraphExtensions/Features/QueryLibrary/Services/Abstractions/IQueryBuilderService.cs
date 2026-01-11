using OptiGraphExtensions.Features.QueryLibrary.Models;

namespace OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions
{
    /// <summary>
    /// Service for building GraphQL queries from visual query configurations.
    /// </summary>
    public interface IQueryBuilderService
    {
        /// <summary>
        /// Builds a GraphQL query string from a query execution request.
        /// </summary>
        string BuildGraphQLQuery(QueryExecutionRequest request);

        /// <summary>
        /// Builds GraphQL variables for the query.
        /// </summary>
        Dictionary<string, object> BuildVariables(QueryExecutionRequest request);

        /// <summary>
        /// Converts selected field paths to GraphQL field selection syntax.
        /// </summary>
        string BuildFieldSelection(IEnumerable<string> fieldPaths);

        /// <summary>
        /// Builds the WHERE clause from filter conditions.
        /// </summary>
        string BuildWhereClause(IEnumerable<QueryFilter> filters);
    }
}
