using OptiGraphExtensions.Features.QueryLibrary.Models;

namespace OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions
{
    /// <summary>
    /// Service for executing queries against Optimizely Graph.
    /// </summary>
    public interface IQueryExecutionService
    {
        /// <summary>
        /// Executes a query and returns a single page of results.
        /// </summary>
        Task<QueryExecutionResult> ExecuteQueryAsync(QueryExecutionRequest request);

        /// <summary>
        /// Executes a query with auto-pagination to fetch all results.
        /// </summary>
        IAsyncEnumerable<QueryExecutionResult> ExecuteQueryWithPaginationAsync(
            QueryExecutionRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count for a query without fetching data.
        /// </summary>
        Task<int> GetQueryCountAsync(QueryExecutionRequest request);

        /// <summary>
        /// Executes a raw GraphQL query directly.
        /// </summary>
        Task<QueryExecutionResult> ExecuteRawQueryAsync(
            string query,
            Dictionary<string, object>? variables = null);
    }
}
