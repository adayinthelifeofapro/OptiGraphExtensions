using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.CustomData.Repositories
{
    /// <summary>
    /// Repository for managing import execution history records.
    /// </summary>
    public interface IImportExecutionHistoryRepository
    {
        /// <summary>
        /// Gets execution history for a specific configuration.
        /// </summary>
        /// <param name="configId">The import configuration ID.</param>
        /// <param name="limit">Maximum number of records to return.</param>
        /// <returns>List of execution history records, ordered by most recent first.</returns>
        Task<IEnumerable<ImportExecutionHistory>> GetByConfigurationIdAsync(Guid configId, int limit = 50);

        /// <summary>
        /// Gets recent failures for a specific configuration.
        /// </summary>
        /// <param name="configId">The import configuration ID.</param>
        /// <param name="limit">Maximum number of records to return.</param>
        /// <returns>List of failed execution records, ordered by most recent first.</returns>
        Task<IEnumerable<ImportExecutionHistory>> GetRecentFailuresAsync(Guid configId, int limit = 10);

        /// <summary>
        /// Gets the most recent execution for a configuration.
        /// </summary>
        /// <param name="configId">The import configuration ID.</param>
        /// <returns>The most recent execution history record, or null if none exists.</returns>
        Task<ImportExecutionHistory?> GetLastExecutionAsync(Guid configId);

        /// <summary>
        /// Adds a new execution history record.
        /// </summary>
        /// <param name="history">The execution history to add.</param>
        Task<ImportExecutionHistory> CreateAsync(ImportExecutionHistory history);

        /// <summary>
        /// Deletes all execution history for a configuration.
        /// </summary>
        /// <param name="configId">The import configuration ID.</param>
        /// <returns>Number of records deleted.</returns>
        Task<int> DeleteByConfigurationIdAsync(Guid configId);

        /// <summary>
        /// Deletes old execution history records.
        /// </summary>
        /// <param name="olderThan">Delete records older than this date.</param>
        /// <returns>Number of records deleted.</returns>
        Task<int> DeleteOlderThanAsync(DateTime olderThan);

        /// <summary>
        /// Gets execution statistics for a configuration.
        /// </summary>
        /// <param name="configId">The import configuration ID.</param>
        /// <param name="fromDate">Start date for statistics.</param>
        /// <returns>Statistics including success rate, average duration, etc.</returns>
        Task<ExecutionStatistics> GetStatisticsAsync(Guid configId, DateTime? fromDate = null);
    }

    /// <summary>
    /// Statistics about import execution history.
    /// </summary>
    public class ExecutionStatistics
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;
        public TimeSpan AverageDuration { get; set; }
        public int TotalItemsImported { get; set; }
        public DateTime? LastSuccessfulExecution { get; set; }
        public DateTime? LastFailedExecution { get; set; }
    }
}
