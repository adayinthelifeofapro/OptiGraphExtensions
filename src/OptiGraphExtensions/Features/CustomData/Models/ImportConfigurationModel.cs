using System.ComponentModel.DataAnnotations;
using System.Text;
using OptiGraphExtensions.Entities;

namespace OptiGraphExtensions.Features.CustomData.Models
{
    /// <summary>
    /// DTO model for import configuration UI operations.
    /// </summary>
    public class ImportConfigurationModel
    {
        /// <summary>
        /// Unique identifier (null for new configurations).
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// User-friendly name for this import configuration.
        /// </summary>
        [Required(ErrorMessage = "Configuration name is required")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of what this import does.
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// The custom data source ID to import data into.
        /// </summary>
        [Required(ErrorMessage = "Target source ID is required")]
        public string TargetSourceId { get; set; } = string.Empty;

        /// <summary>
        /// The content type to import data as.
        /// </summary>
        [Required(ErrorMessage = "Target content type is required")]
        public string TargetContentType { get; set; } = string.Empty;

        /// <summary>
        /// The external API endpoint URL.
        /// </summary>
        [Required(ErrorMessage = "API URL is required")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method for the API request.
        /// </summary>
        public string HttpMethod { get; set; } = "GET";

        /// <summary>
        /// Authentication type.
        /// </summary>
        public AuthenticationType AuthType { get; set; } = AuthenticationType.None;

        /// <summary>
        /// For ApiKey: the header name (e.g., "X-API-Key").
        /// For Basic: the username.
        /// </summary>
        public string? AuthKeyOrUsername { get; set; }

        /// <summary>
        /// For ApiKey: the API key value.
        /// For Basic: the password.
        /// For Bearer: the token.
        /// </summary>
        public string? AuthValueOrPassword { get; set; }

        /// <summary>
        /// Field mappings from external data to schema properties.
        /// </summary>
        public List<FieldMapping> FieldMappings { get; set; } = new();

        /// <summary>
        /// The external field path to use as the item ID (required for deduplication).
        /// </summary>
        [Required(ErrorMessage = "ID field mapping is required for deduplication")]
        public string IdFieldMapping { get; set; } = string.Empty;

        /// <summary>
        /// Language routing value for imported items.
        /// </summary>
        public string? LanguageRouting { get; set; }

        /// <summary>
        /// Optional JSON path to navigate to the data array within the response.
        /// Examples: "data", "results", "items", "data.records"
        /// </summary>
        [StringLength(255)]
        public string? JsonPath { get; set; }

        /// <summary>
        /// Custom HTTP headers to include in the request.
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();

        /// <summary>
        /// Whether this configuration is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last time this import was executed (read-only).
        /// </summary>
        public DateTime? LastImportAt { get; set; }

        /// <summary>
        /// Number of items imported in last execution (read-only).
        /// </summary>
        public int? LastImportCount { get; set; }

        /// <summary>
        /// Whether the last import was successful (read-only).
        /// </summary>
        public bool? LastImportSuccess { get; set; }

        /// <summary>
        /// Error message from the last failed import (read-only).
        /// </summary>
        public string? LastImportError { get; set; }

        #region Schedule Configuration

        /// <summary>
        /// The schedule frequency type.
        /// </summary>
        public ScheduleFrequency ScheduleFrequency { get; set; } = ScheduleFrequency.None;

        /// <summary>
        /// The interval value for the schedule (e.g., 2 for "every 2 hours").
        /// </summary>
        [Range(1, 100, ErrorMessage = "Interval must be between 1 and 100")]
        public int ScheduleIntervalValue { get; set; } = 1;

        /// <summary>
        /// Time of day for daily/weekly/monthly schedules.
        /// </summary>
        public TimeSpan? ScheduleTimeOfDay { get; set; }

        /// <summary>
        /// Day of week for weekly schedules.
        /// </summary>
        public int? ScheduleDayOfWeek { get; set; }

        /// <summary>
        /// Day of month for monthly schedules (1-31).
        /// </summary>
        [Range(1, 31, ErrorMessage = "Day of month must be between 1 and 31")]
        public int? ScheduleDayOfMonth { get; set; }

        /// <summary>
        /// The next scheduled run time (read-only, calculated).
        /// </summary>
        public DateTime? NextScheduledRunAt { get; set; }

        #endregion

        #region Retry Configuration

        /// <summary>
        /// Maximum number of retry attempts after a failure.
        /// </summary>
        [Range(0, 10, ErrorMessage = "Max retries must be between 0 and 10")]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Current count of consecutive failures (read-only).
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// Next retry time if in backoff (read-only).
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        #endregion

        #region Notification Settings

        /// <summary>
        /// Email address to notify on import failure (after max retries exhausted).
        /// </summary>
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(255)]
        public string? NotificationEmail { get; set; }

        #endregion

        #region Helper Properties

        /// <summary>
        /// Whether this configuration has scheduling enabled.
        /// </summary>
        public bool IsScheduled => ScheduleFrequency != ScheduleFrequency.None;

        /// <summary>
        /// Whether there is a pending retry due to previous failure.
        /// </summary>
        public bool HasPendingRetry => NextRetryAt.HasValue && NextRetryAt <= DateTime.UtcNow;

        /// <summary>
        /// Human-readable description of the schedule.
        /// </summary>
        public string ScheduleDescription => GetScheduleDescription();

        /// <summary>
        /// Gets the next execution time (either scheduled or retry).
        /// </summary>
        public DateTime? NextExecutionAt => NextRetryAt ?? NextScheduledRunAt;

        /// <summary>
        /// Gets the execution status for display.
        /// </summary>
        public string ExecutionStatus => GetExecutionStatus();

        #endregion

        #region Static Options

        /// <summary>
        /// Available HTTP methods for the dropdown.
        /// </summary>
        public static IEnumerable<string> AvailableHttpMethods => new[] { "GET", "POST" };

        /// <summary>
        /// Available schedule frequencies for the dropdown.
        /// </summary>
        public static IEnumerable<(ScheduleFrequency Value, string Label)> AvailableScheduleFrequencies => new[]
        {
            (ScheduleFrequency.None, "Manual Only"),
            (ScheduleFrequency.Hourly, "Hourly"),
            (ScheduleFrequency.Daily, "Daily"),
            (ScheduleFrequency.Weekly, "Weekly"),
            (ScheduleFrequency.Monthly, "Monthly")
        };

        /// <summary>
        /// Available days of week for the dropdown.
        /// </summary>
        public static IEnumerable<(int Value, string Label)> AvailableDaysOfWeek => new[]
        {
            (0, "Sunday"),
            (1, "Monday"),
            (2, "Tuesday"),
            (3, "Wednesday"),
            (4, "Thursday"),
            (5, "Friday"),
            (6, "Saturday")
        };

        /// <summary>
        /// Available days of month for the dropdown.
        /// </summary>
        public static IEnumerable<int> AvailableDaysOfMonth => Enumerable.Range(1, 31);

        /// <summary>
        /// Available hours for the time picker.
        /// </summary>
        public static IEnumerable<int> AvailableHours => Enumerable.Range(0, 24);

        /// <summary>
        /// Available minutes for the time picker (in 15-minute increments).
        /// </summary>
        public static IEnumerable<int> AvailableMinutes => new[] { 0, 15, 30, 45 };

        #endregion

        #region Private Methods

        private string GetScheduleDescription()
        {
            return ScheduleFrequency switch
            {
                ScheduleFrequency.None => "Manual only",
                ScheduleFrequency.Hourly => ScheduleIntervalValue == 1
                    ? "Every hour"
                    : $"Every {ScheduleIntervalValue} hours",
                ScheduleFrequency.Daily => ScheduleTimeOfDay.HasValue
                    ? $"Daily at {FormatTime(ScheduleTimeOfDay.Value)}"
                    : "Daily",
                ScheduleFrequency.Weekly => FormatWeeklySchedule(),
                ScheduleFrequency.Monthly => FormatMonthlySchedule(),
                _ => "Unknown schedule"
            };
        }

        private string FormatWeeklySchedule()
        {
            var sb = new StringBuilder("Weekly");
            if (ScheduleDayOfWeek.HasValue)
            {
                var dayName = AvailableDaysOfWeek.FirstOrDefault(d => d.Value == ScheduleDayOfWeek.Value).Label ?? "Unknown";
                sb.Append($" on {dayName}");
            }
            if (ScheduleTimeOfDay.HasValue)
            {
                sb.Append($" at {FormatTime(ScheduleTimeOfDay.Value)}");
            }
            return sb.ToString();
        }

        private string FormatMonthlySchedule()
        {
            var sb = new StringBuilder("Monthly");
            if (ScheduleDayOfMonth.HasValue)
            {
                sb.Append($" on day {ScheduleDayOfMonth.Value}");
            }
            if (ScheduleTimeOfDay.HasValue)
            {
                sb.Append($" at {FormatTime(ScheduleTimeOfDay.Value)}");
            }
            return sb.ToString();
        }

        private static string FormatTime(TimeSpan time)
        {
            return DateTime.Today.Add(time).ToString("HH:mm");
        }

        private string GetExecutionStatus()
        {
            if (ConsecutiveFailures >= MaxRetries && MaxRetries > 0)
            {
                return "Disabled (max retries exceeded)";
            }
            if (HasPendingRetry)
            {
                return $"Retry pending (attempt {ConsecutiveFailures + 1}/{MaxRetries})";
            }
            if (!LastImportSuccess.HasValue)
            {
                return "Never run";
            }
            return LastImportSuccess.Value ? "Success" : "Failed";
        }

        #endregion
    }
}
