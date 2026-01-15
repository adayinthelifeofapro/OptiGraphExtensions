using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.CustomData.Models;

namespace OptiGraphExtensions.Features.CustomData.Services.Abstractions
{
    /// <summary>
    /// Service for managing scheduled import execution and tracking.
    /// </summary>
    public interface IScheduledImportService
    {
        /// <summary>
        /// Gets all import configurations that are due for execution.
        /// This includes configurations where NextScheduledRunAt has passed
        /// or where NextRetryAt has passed (for failed imports).
        /// </summary>
        Task<IEnumerable<ImportConfiguration>> GetDueConfigurationsAsync();

        /// <summary>
        /// Calculates the next scheduled run time based on the configuration's schedule settings.
        /// </summary>
        /// <param name="config">The import configuration.</param>
        /// <returns>The next scheduled run time in UTC.</returns>
        DateTime CalculateNextRunTime(ImportConfiguration config);

        /// <summary>
        /// Calculates the retry delay based on the number of consecutive failures.
        /// Uses exponential backoff: 1 min, 5 min, 15 min, 30 min (max).
        /// </summary>
        /// <param name="consecutiveFailures">Number of consecutive failures.</param>
        /// <returns>The delay before the next retry.</returns>
        TimeSpan CalculateRetryDelay(int consecutiveFailures);

        /// <summary>
        /// Records an import execution in the history table.
        /// </summary>
        /// <param name="configId">The import configuration ID.</param>
        /// <param name="result">The import result.</param>
        /// <param name="wasRetry">Whether this was a retry attempt.</param>
        /// <param name="retryAttempt">The retry attempt number (0 for first run).</param>
        /// <param name="wasScheduled">Whether this was triggered by the scheduled job.</param>
        Task RecordExecutionAsync(Guid configId, ImportResult result, bool wasRetry, int retryAttempt, bool wasScheduled = true);

        /// <summary>
        /// Updates the configuration's next run time and failure tracking after an execution.
        /// </summary>
        /// <param name="config">The import configuration.</param>
        /// <param name="wasSuccess">Whether the import was successful.</param>
        /// <param name="errorMessage">The error message if failed.</param>
        Task UpdateConfigurationAfterExecutionAsync(ImportConfiguration config, bool wasSuccess, string? errorMessage = null);

        /// <summary>
        /// Initializes the next scheduled run time for a configuration when scheduling is enabled.
        /// </summary>
        /// <param name="config">The import configuration.</param>
        Task InitializeScheduleAsync(ImportConfiguration config);
    }
}
