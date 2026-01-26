namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// Result of an external data import operation.
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// Whether the import completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total number of items received from the external API.
        /// </summary>
        public int TotalItemsReceived { get; set; }

        /// <summary>
        /// Number of items successfully imported to Graph.
        /// </summary>
        public int ItemsImported { get; set; }

        /// <summary>
        /// Number of items skipped (e.g., missing ID field).
        /// </summary>
        public int ItemsSkipped { get; set; }

        /// <summary>
        /// Number of items that failed to import.
        /// </summary>
        public int ItemsFailed { get; set; }

        /// <summary>
        /// Error messages encountered during import.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Warning messages (non-fatal issues).
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// How long the import took.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static ImportResult Successful(int itemsImported, int totalReceived, TimeSpan duration)
        {
            return new ImportResult
            {
                Success = true,
                ItemsImported = itemsImported,
                TotalItemsReceived = totalReceived,
                Duration = duration
            };
        }

        /// <summary>
        /// Creates a failed result with an error message.
        /// </summary>
        public static ImportResult Failed(string errorMessage, TimeSpan duration = default)
        {
            return new ImportResult
            {
                Success = false,
                Duration = duration,
                Errors = new List<string> { errorMessage }
            };
        }
    }
}
