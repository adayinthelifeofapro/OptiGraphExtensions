using OptiGraphExtensions.Features.QueryLibrary.Models;

namespace OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions
{
    /// <summary>
    /// Service for exporting query results to CSV format.
    /// </summary>
    public interface ICsvExportService
    {
        /// <summary>
        /// Streams CSV export to the output stream, handling large datasets efficiently.
        /// </summary>
        Task ExportToCsvAsync(
            IAsyncEnumerable<QueryExecutionResult> results,
            Stream outputStream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates CSV content from a single result set (for preview).
        /// </summary>
        string GenerateCsvPreview(QueryExecutionResult result, int maxRows = 100);

        /// <summary>
        /// Generates the filename for a CSV export.
        /// </summary>
        string GenerateFilename(string queryName);
    }
}
