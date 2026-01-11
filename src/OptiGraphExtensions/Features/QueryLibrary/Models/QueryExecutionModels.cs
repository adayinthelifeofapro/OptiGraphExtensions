using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.QueryLibrary.Models
{
    /// <summary>
    /// Request model for executing a query.
    /// </summary>
    public class QueryExecutionRequest
    {
        /// <summary>
        /// The type of query (Visual or Raw).
        /// </summary>
        public QueryType QueryType { get; set; } = QueryType.Visual;

        // === Visual Query Fields ===

        /// <summary>
        /// The content type to query (for Visual queries).
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// List of field paths to select (for Visual queries).
        /// </summary>
        public List<string> SelectedFields { get; set; } = new();

        /// <summary>
        /// List of filter conditions (for Visual queries).
        /// </summary>
        public List<QueryFilter> Filters { get; set; } = new();

        /// <summary>
        /// Language filter.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Field to sort results by (for Visual queries).
        /// </summary>
        public string? SortField { get; set; }

        /// <summary>
        /// Whether to sort in descending order.
        /// </summary>
        public bool SortDescending { get; set; }

        // === Raw Query Fields ===

        /// <summary>
        /// The raw GraphQL query string (for Raw queries).
        /// </summary>
        public string? RawGraphQuery { get; set; }

        /// <summary>
        /// Query variables as a dictionary (for Raw queries).
        /// </summary>
        public Dictionary<string, object>? QueryVariables { get; set; }

        // === Common Fields ===

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// Cursor for pagination (null for first page).
        /// </summary>
        public string? Cursor { get; set; }
    }

    /// <summary>
    /// Result model for query execution.
    /// </summary>
    public class QueryExecutionResult
    {
        /// <summary>
        /// The data rows as a list of dictionaries (column name -> value).
        /// </summary>
        public List<Dictionary<string, object?>> Rows { get; set; } = new();

        /// <summary>
        /// The column names/headers in order.
        /// </summary>
        public List<string> Columns { get; set; } = new();

        /// <summary>
        /// Total count of items matching the query.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Cursor for the next page (null if no more pages).
        /// </summary>
        public string? NextCursor { get; set; }

        /// <summary>
        /// Whether there are more pages available.
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// Error message if the query failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Whether the query execution was successful.
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }

    /// <summary>
    /// Result model for query validation.
    /// </summary>
    public class QueryValidationResult
    {
        /// <summary>
        /// Whether the query is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors or warnings.
        /// </summary>
        public List<string> Messages { get; set; } = new();

        /// <summary>
        /// Whether the query supports pagination (has cursor and total fields).
        /// </summary>
        public bool SupportsPagination { get; set; }
    }
}
