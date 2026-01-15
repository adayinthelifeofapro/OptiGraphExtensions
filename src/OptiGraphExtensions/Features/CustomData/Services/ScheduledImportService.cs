using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.CustomData.Models;
using OptiGraphExtensions.Features.CustomData.Services.Abstractions;

namespace OptiGraphExtensions.Features.CustomData.Services
{
    /// <summary>
    /// Service for managing scheduled import execution and tracking.
    /// </summary>
    public class ScheduledImportService : IScheduledImportService
    {
        private readonly IOptiGraphExtensionsDataContext _dataContext;
        private readonly ILogger<ScheduledImportService> _logger;

        // Exponential backoff delays in minutes
        private static readonly int[] RetryDelaysMinutes = { 1, 5, 15, 30 };

        public ScheduledImportService(
            IOptiGraphExtensionsDataContext dataContext,
            ILogger<ScheduledImportService> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ImportConfiguration>> GetDueConfigurationsAsync()
        {
            var now = DateTime.UtcNow;

            var dueConfigs = await _dataContext.ImportConfigurations
                .AsNoTracking()
                .Where(c => c.IsActive &&
                           c.ScheduleFrequency != ScheduleFrequency.None &&
                           ((c.NextScheduledRunAt.HasValue && c.NextScheduledRunAt <= now) ||
                            (c.NextRetryAt.HasValue && c.NextRetryAt <= now)))
                .OrderBy(c => c.NextRetryAt ?? c.NextScheduledRunAt)
                .ToListAsync();

            _logger.LogInformation("Found {Count} import configurations due for execution", dueConfigs.Count);

            return dueConfigs;
        }

        /// <inheritdoc />
        public DateTime CalculateNextRunTime(ImportConfiguration config)
        {
            var now = DateTime.UtcNow;

            switch (config.ScheduleFrequency)
            {
                case ScheduleFrequency.Hourly:
                    return CalculateNextHourlyRun(now, config.ScheduleIntervalValue);

                case ScheduleFrequency.Daily:
                    return CalculateNextDailyRun(now, config.ScheduleTimeOfDay ?? TimeSpan.Zero);

                case ScheduleFrequency.Weekly:
                    return CalculateNextWeeklyRun(now, config.ScheduleDayOfWeek ?? DayOfWeek.Monday, config.ScheduleTimeOfDay ?? TimeSpan.Zero);

                case ScheduleFrequency.Monthly:
                    return CalculateNextMonthlyRun(now, config.ScheduleDayOfMonth ?? 1, config.ScheduleTimeOfDay ?? TimeSpan.Zero);

                case ScheduleFrequency.None:
                default:
                    // No schedule - return far future date
                    return DateTime.MaxValue;
            }
        }

        /// <inheritdoc />
        public TimeSpan CalculateRetryDelay(int consecutiveFailures)
        {
            // Use exponential backoff with a maximum
            var index = Math.Min(consecutiveFailures, RetryDelaysMinutes.Length - 1);
            var delayMinutes = RetryDelaysMinutes[Math.Max(0, index)];

            return TimeSpan.FromMinutes(delayMinutes);
        }

        /// <inheritdoc />
        public async Task RecordExecutionAsync(Guid configId, ImportResult result, bool wasRetry, int retryAttempt, bool wasScheduled = true)
        {
            var history = new ImportExecutionHistory
            {
                Id = Guid.NewGuid(),
                ImportConfigurationId = configId,
                ExecutedAt = DateTime.UtcNow,
                Success = result.Success,
                ItemsReceived = result.TotalItemsReceived,
                ItemsImported = result.ItemsImported,
                ItemsSkipped = result.ItemsSkipped,
                ItemsFailed = result.ItemsFailed,
                Duration = result.Duration,
                ErrorMessage = result.Success ? null : string.Join("; ", result.Errors),
                Warnings = result.Warnings.Count > 0 ? JsonSerializer.Serialize(result.Warnings) : null,
                WasRetry = wasRetry,
                RetryAttempt = retryAttempt,
                WasScheduled = wasScheduled
            };

            _dataContext.ImportExecutionHistories.Add(history);
            await _dataContext.SaveChangesAsync();

            _logger.LogInformation(
                "Recorded execution history for config {ConfigId}: Success={Success}, Items={Items}",
                configId, result.Success, result.ItemsImported);
        }

        /// <inheritdoc />
        public async Task UpdateConfigurationAfterExecutionAsync(ImportConfiguration config, bool wasSuccess, string? errorMessage = null)
        {
            // Get the tracked entity
            var trackedConfig = await _dataContext.ImportConfigurations.FindAsync(config.Id);
            if (trackedConfig == null)
            {
                _logger.LogWarning("Could not find configuration {ConfigId} to update", config.Id);
                return;
            }

            trackedConfig.LastImportAt = DateTime.UtcNow;
            trackedConfig.LastImportSuccess = wasSuccess;

            if (wasSuccess)
            {
                // Reset failure tracking on success
                trackedConfig.ConsecutiveFailures = 0;
                trackedConfig.NextRetryAt = null;
                trackedConfig.LastImportError = null;

                // Calculate next scheduled run
                trackedConfig.NextScheduledRunAt = CalculateNextRunTime(trackedConfig);

                _logger.LogInformation(
                    "Configuration {ConfigId} succeeded. Next run: {NextRun}",
                    config.Id, trackedConfig.NextScheduledRunAt);
            }
            else
            {
                // Increment failure count
                trackedConfig.ConsecutiveFailures++;
                trackedConfig.LastImportError = errorMessage;

                if (trackedConfig.ConsecutiveFailures < trackedConfig.MaxRetries)
                {
                    // Schedule retry with exponential backoff
                    var retryDelay = CalculateRetryDelay(trackedConfig.ConsecutiveFailures);
                    trackedConfig.NextRetryAt = DateTime.UtcNow.Add(retryDelay);

                    _logger.LogWarning(
                        "Configuration {ConfigId} failed (attempt {Attempt}/{MaxRetries}). Retry in {Delay}",
                        config.Id, trackedConfig.ConsecutiveFailures, trackedConfig.MaxRetries, retryDelay);
                }
                else
                {
                    // Max retries reached - clear retry, keep scheduled run for next cycle
                    trackedConfig.NextRetryAt = null;
                    trackedConfig.NextScheduledRunAt = CalculateNextRunTime(trackedConfig);

                    _logger.LogError(
                        "Configuration {ConfigId} failed after {MaxRetries} retries. Next scheduled run: {NextRun}",
                        config.Id, trackedConfig.MaxRetries, trackedConfig.NextScheduledRunAt);
                }
            }

            await _dataContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task InitializeScheduleAsync(ImportConfiguration config)
        {
            var trackedConfig = await _dataContext.ImportConfigurations.FindAsync(config.Id);
            if (trackedConfig == null)
            {
                _logger.LogWarning("Could not find configuration {ConfigId} to initialize schedule", config.Id);
                return;
            }

            if (trackedConfig.ScheduleFrequency != ScheduleFrequency.None)
            {
                trackedConfig.NextScheduledRunAt = CalculateNextRunTime(trackedConfig);
                trackedConfig.ConsecutiveFailures = 0;
                trackedConfig.NextRetryAt = null;

                await _dataContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Initialized schedule for configuration {ConfigId}. Next run: {NextRun}",
                    config.Id, trackedConfig.NextScheduledRunAt);
            }
        }

        #region Private Helper Methods

        private static DateTime CalculateNextHourlyRun(DateTime now, int intervalHours)
        {
            // Ensure minimum interval of 1 hour
            intervalHours = Math.Max(1, intervalHours);

            // Calculate next run time by adding the interval
            return now.AddHours(intervalHours);
        }

        private static DateTime CalculateNextDailyRun(DateTime now, TimeSpan timeOfDay)
        {
            var todayRun = now.Date.Add(timeOfDay);

            // If today's run time has passed, schedule for tomorrow
            if (todayRun <= now)
            {
                return todayRun.AddDays(1);
            }

            return todayRun;
        }

        private static DateTime CalculateNextWeeklyRun(DateTime now, DayOfWeek targetDay, TimeSpan timeOfDay)
        {
            var daysUntilTarget = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;

            // If today is the target day but the time has passed, schedule for next week
            if (daysUntilTarget == 0 && now.TimeOfDay >= timeOfDay)
            {
                daysUntilTarget = 7;
            }

            return now.Date.AddDays(daysUntilTarget).Add(timeOfDay);
        }

        private static DateTime CalculateNextMonthlyRun(DateTime now, int dayOfMonth, TimeSpan timeOfDay)
        {
            // Clamp day of month to valid range
            dayOfMonth = Math.Clamp(dayOfMonth, 1, 28); // Use 28 to avoid issues with short months

            var thisMonthRun = new DateTime(now.Year, now.Month, dayOfMonth, timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds, DateTimeKind.Utc);

            // If this month's run has passed, schedule for next month
            if (thisMonthRun <= now)
            {
                var nextMonth = now.AddMonths(1);
                return new DateTime(nextMonth.Year, nextMonth.Month, dayOfMonth, timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds, DateTimeKind.Utc);
            }

            return thisMonthRun;
        }

        #endregion
    }
}
