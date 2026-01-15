using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptiGraphExtensions.Entities
{
    /// <summary>
    /// Records the history of import execution attempts for tracking and auditing.
    /// </summary>
    [Table("tbl_OptiGraphExtensions_ImportExecutionHistory")]
    public class ImportExecutionHistory
    {
        /// <summary>
        /// Unique identifier for this history record.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the import configuration that was executed.
        /// </summary>
        public Guid ImportConfigurationId { get; set; }

        /// <summary>
        /// When this execution was started.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Whether the execution completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total number of items received from the external API.
        /// </summary>
        public int ItemsReceived { get; set; }

        /// <summary>
        /// Number of items successfully imported to Optimizely Graph.
        /// </summary>
        public int ItemsImported { get; set; }

        /// <summary>
        /// Number of items skipped (e.g., duplicates or invalid data).
        /// </summary>
        public int ItemsSkipped { get; set; }

        /// <summary>
        /// Number of items that failed to import.
        /// </summary>
        public int ItemsFailed { get; set; }

        /// <summary>
        /// Total duration of the execution in ticks.
        /// Use TimeSpan.FromTicks() to convert.
        /// </summary>
        public long DurationTicks { get; set; }

        /// <summary>
        /// Duration as a TimeSpan (not mapped to database).
        /// </summary>
        [NotMapped]
        public TimeSpan Duration
        {
            get => TimeSpan.FromTicks(DurationTicks);
            set => DurationTicks = value.Ticks;
        }

        /// <summary>
        /// Error message if the execution failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// JSON-serialized array of warning messages.
        /// </summary>
        public string? Warnings { get; set; }

        /// <summary>
        /// Whether this execution was a retry attempt.
        /// </summary>
        public bool WasRetry { get; set; }

        /// <summary>
        /// Which retry attempt this was (0 = first run, 1 = first retry, etc.).
        /// </summary>
        public int RetryAttempt { get; set; }

        /// <summary>
        /// Whether this execution was triggered by the scheduled job or manual.
        /// </summary>
        public bool WasScheduled { get; set; }

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// Reference to the parent import configuration.
        /// </summary>
        [ForeignKey(nameof(ImportConfigurationId))]
        public virtual ImportConfiguration? ImportConfiguration { get; set; }
    }
}
