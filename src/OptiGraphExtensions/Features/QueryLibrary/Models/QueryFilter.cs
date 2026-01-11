namespace OptiGraphExtensions.Features.QueryLibrary.Models
{
    /// <summary>
    /// Represents a filter condition for a visual query.
    /// </summary>
    public class QueryFilter
    {
        /// <summary>
        /// The field name to filter on.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// The filter operator (eq, neq, contains, startsWith, gt, lt, gte, lte).
        /// </summary>
        public string Operator { get; set; } = "eq";

        /// <summary>
        /// The value to filter by.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
