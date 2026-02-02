namespace ReportingService.Api.Models
{
    /// <summary>
    /// Represents a scheduling configuration, including recurrence, retention, and descriptive metadata for a scheduled
    /// process or event.
    /// </summary>
    /// <remarks>Use this class to define the parameters and metadata for a schedule, such as its type,
    /// recurrence pattern, and retention period. The schedule can be customized for different time ranges and
    /// descriptive purposes. Thread safety is not guaranteed; if multiple threads access instances of this class
    /// concurrently, external synchronization is required.</remarks>
    public class Schedule
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Name { get; set; } = string.Empty;
        public ScheduleType Type { get; set; } = ScheduleType.Recurring;
        public int StartingDay { get; set; } = 1; //monday
        public int EndingDay { get; set; } = 5;   // Friday
        public int SendHour { get; set; } = 9;    // 9 AM
        public int SendMinute { get; set; } = 0;  // 0 Minutes
        public RecurrencePattern? Recurrence { get; set; } = RecurrencePattern.Daily;
        public DataRetention RetentionPeriod { get; set; } = DataRetention.CurrentDay;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

    }


    /// <summary>
    /// Specifies the available data retention periods for filtering or managing stored data.
    /// </summary>
    /// <remarks>Use this enumeration to indicate the time range of data to retain or process, such as the
    /// current day, previous day, previous week, or previous month. The <see cref="DataRetention.None"/> value
    /// indicates that no data should be retained.</remarks>
    public enum DataRetention
    {
        CurrentDay,
        PreviousDay,
        PreviousWeek,
        PreviousMonth,
        None
    }

    /// <summary>
    /// Specifies the type of scheduling to be used for an operation or event.
    /// </summary>
    /// <remarks>Use this enumeration to indicate whether an action should occur only once or on a recurring
    /// basis. The value determines how the scheduling logic interprets and executes the associated operation.</remarks>
    public enum ScheduleType
    {
        OneTime,
        Recurring
    }


    /// <summary>
    /// Specifies the recurrence pattern for a scheduled event or action.
    /// </summary>
    /// <remarks>Use this enumeration to indicate how often an event should repeat. The available patterns are
    /// daily, weekly, monthly, and yearly. This type is commonly used in scheduling APIs to define the frequency of
    /// recurring tasks or appointments.</remarks>
    public enum RecurrencePattern
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }
}
