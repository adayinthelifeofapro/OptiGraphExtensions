using System.Globalization;
using System.Text;
using OptiGraphExtensions.Features.QueryLibrary.Models;
using OptiGraphExtensions.Features.QueryLibrary.Services.Abstractions;

namespace OptiGraphExtensions.Features.QueryLibrary.Services
{
    /// <summary>
    /// Service for exporting query results to CSV format.
    /// </summary>
    public class CsvExportService : ICsvExportService
    {
        // UTF-8 BOM for Excel compatibility
        private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

        public async Task ExportToCsvAsync(
            IAsyncEnumerable<QueryExecutionResult> results,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);

            // Write UTF-8 BOM for Excel compatibility
            await outputStream.WriteAsync(Utf8Bom, cancellationToken);

            var headersWritten = false;
            var columns = new List<string>();

            await foreach (var result in results.WithCancellation(cancellationToken))
            {
                if (!result.IsSuccess)
                {
                    // Write error message if query failed
                    await writer.WriteLineAsync($"Error: {result.ErrorMessage}");
                    break;
                }

                // Write headers on first result
                if (!headersWritten && result.Columns.Any())
                {
                    columns = result.Columns;
                    await writer.WriteLineAsync(FormatCsvRow(columns));
                    headersWritten = true;
                }

                // Write data rows
                foreach (var row in result.Rows)
                {
                    var values = columns.Select(col =>
                        row.TryGetValue(col, out var value) ? FormatCsvValue(value) : "");
                    await writer.WriteLineAsync(FormatCsvRow(values));
                }

                await writer.FlushAsync();
            }
        }

        public string GenerateCsvPreview(QueryExecutionResult result, int maxRows = 100)
        {
            var sb = new StringBuilder();

            if (!result.IsSuccess)
            {
                sb.AppendLine($"Error: {result.ErrorMessage}");
                return sb.ToString();
            }

            if (!result.Columns.Any())
            {
                sb.AppendLine("No data to display");
                return sb.ToString();
            }

            // Write headers
            sb.AppendLine(FormatCsvRow(result.Columns));

            // Write data rows (limited)
            var rowCount = 0;
            foreach (var row in result.Rows)
            {
                if (rowCount >= maxRows) break;

                var values = result.Columns.Select(col =>
                    row.TryGetValue(col, out var value) ? FormatCsvValue(value) : "");
                sb.AppendLine(FormatCsvRow(values));
                rowCount++;
            }

            if (result.Rows.Count > maxRows)
            {
                sb.AppendLine($"... and {result.Rows.Count - maxRows} more rows");
            }

            return sb.ToString();
        }

        public string GenerateFilename(string queryName)
        {
            // Sanitize query name for filename
            var sanitized = string.Join("_", queryName.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "export";
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            return $"{sanitized}_{timestamp}.csv";
        }

        private string FormatCsvRow(IEnumerable<string> values)
        {
            return string.Join(",", values.Select(v => EscapeCsvValue(v)));
        }

        private string FormatCsvValue(object? value)
        {
            if (value == null)
            {
                return "";
            }

            return value switch
            {
                bool b => b ? "true" : "false",
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? "",
                _ => value.ToString() ?? ""
            };
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            // Check if value needs quoting (RFC 4180)
            var needsQuoting = value.Contains(',') ||
                               value.Contains('"') ||
                               value.Contains('\n') ||
                               value.Contains('\r') ||
                               value.StartsWith(' ') ||
                               value.EndsWith(' ');

            if (!needsQuoting)
            {
                return value;
            }

            // Escape double quotes by doubling them
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}
