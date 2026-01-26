namespace OptiGraphExtensions.Entities
{
    /// <summary>
    /// Frequency types for scheduled import execution.
    /// </summary>
    public enum ScheduleFrequency
    {
        /// <summary>
        /// No schedule - manual execution only.
        /// </summary>
        None = 0,

        /// <summary>
        /// Run every N hours.
        /// </summary>
        Hourly = 1,

        /// <summary>
        /// Run daily at a specific time.
        /// </summary>
        Daily = 2,

        /// <summary>
        /// Run weekly on a specific day and time.
        /// </summary>
        Weekly = 3,

        /// <summary>
        /// Run monthly on a specific day and time.
        /// </summary>
        Monthly = 4
    }
}
