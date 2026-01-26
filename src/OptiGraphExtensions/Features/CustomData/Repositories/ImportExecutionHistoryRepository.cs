using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.CustomData.Repositories
{
    /// <summary>
    /// Repository for managing import execution history records.
    /// </summary>
    public class ImportExecutionHistoryRepository : IImportExecutionHistoryRepository
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;
        private readonly ILogger<ImportExecutionHistoryRepository> _logger;

        public ImportExecutionHistoryRepository(
            IOptiGraphExtensionsDataContext dataContext,
            ILogger<ImportExecutionHistoryRepository> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ImportExecutionHistory>> GetByConfigurationIdAsync(Guid configId, int limit = 50)
        {
            return await _dataContext.ImportExecutionHistories
                .AsNoTracking()
                .Where(h => h.ImportConfigurationId == configId)
                .OrderByDescending(h => h.ExecutedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ImportExecutionHistory>> GetRecentFailuresAsync(Guid configId, int limit = 10)
        {
            return await _dataContext.ImportExecutionHistories
                .AsNoTracking()
                .Where(h => h.ImportConfigurationId == configId && !h.Success)
                .OrderByDescending(h => h.ExecutedAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<ImportExecutionHistory?> GetLastExecutionAsync(Guid configId)
        {
            return await _dataContext.ImportExecutionHistories
                .AsNoTracking()
                .Where(h => h.ImportConfigurationId == configId)
                .OrderByDescending(h => h.ExecutedAt)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<ImportExecutionHistory> CreateAsync(ImportExecutionHistory history)
        {
            if (history.Id == Guid.Empty)
            {
                history.Id = Guid.NewGuid();
            }

            _dataContext.ImportExecutionHistories.Add(history);
            await _dataContext.SaveChangesAsync();

            _logger.LogDebug("Created execution history record {Id} for config {ConfigId}", history.Id, history.ImportConfigurationId);

            return history;
        }

        /// <inheritdoc />
        public async Task<int> DeleteByConfigurationIdAsync(Guid configId)
        {
            var records = await _dataContext.ImportExecutionHistories
                .Where(h => h.ImportConfigurationId == configId)
                .ToListAsync();

            if (records.Count == 0)
            {
                return 0;
            }

            _dataContext.ImportExecutionHistories.RemoveRange(records);
            var deletedCount = await _dataContext.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} execution history records for config {ConfigId}", deletedCount, configId);

            return deletedCount;
        }

        /// <inheritdoc />
        public async Task<int> DeleteOlderThanAsync(DateTime olderThan)
        {
            var records = await _dataContext.ImportExecutionHistories
                .Where(h => h.ExecutedAt < olderThan)
                .ToListAsync();

            if (records.Count == 0)
            {
                return 0;
            }

            _dataContext.ImportExecutionHistories.RemoveRange(records);
            var deletedCount = await _dataContext.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} execution history records older than {Date}", deletedCount, olderThan);

            return deletedCount;
        }

        /// <inheritdoc />
        public async Task<ExecutionStatistics> GetStatisticsAsync(Guid configId, DateTime? fromDate = null)
        {
            var query = _dataContext.ImportExecutionHistories
                .AsNoTracking()
                .Where(h => h.ImportConfigurationId == configId);

            if (fromDate.HasValue)
            {
                query = query.Where(h => h.ExecutedAt >= fromDate.Value);
            }

            var records = await query.ToListAsync();

            if (records.Count == 0)
            {
                return new ExecutionStatistics();
            }

            var successfulRecords = records.Where(r => r.Success).ToList();
            var failedRecords = records.Where(r => !r.Success).ToList();

            return new ExecutionStatistics
            {
                TotalExecutions = records.Count,
                SuccessfulExecutions = successfulRecords.Count,
                FailedExecutions = failedRecords.Count,
                AverageDuration = records.Count > 0
                    ? TimeSpan.FromTicks((long)records.Average(r => r.DurationTicks))
                    : TimeSpan.Zero,
                TotalItemsImported = records.Sum(r => r.ItemsImported),
                LastSuccessfulExecution = successfulRecords.MaxBy(r => r.ExecutedAt)?.ExecutedAt,
                LastFailedExecution = failedRecords.MaxBy(r => r.ExecutedAt)?.ExecutedAt
            };
        }
    }
}
