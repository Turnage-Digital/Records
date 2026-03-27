namespace Records.Notifications.Domain.ValueObjects;

public sealed record NotificationSchedule
{
    public ScheduleType Type { get; init; }
    public TimeSpan? Delay { get; init; }
    public TimeOnly? DailyAt { get; init; }
    public DayOfWeek[]? DaysOfWeek { get; init; }
    public TimeSpan? BatchWindow { get; init; }
    public string? CronExpression { get; init; }

    public static NotificationSchedule Immediate()
    {
        return new NotificationSchedule { Type = ScheduleType.Immediate };
    }

    public static NotificationSchedule Delayed(TimeSpan delay)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Delayed,
            Delay = delay
        };
    }

    public static NotificationSchedule Daily(TimeOnly dailyAt)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Daily,
            DailyAt = dailyAt
        };
    }

    public static NotificationSchedule Weekly(TimeOnly dailyAt, params DayOfWeek[] daysOfWeek)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Weekly,
            DaysOfWeek = daysOfWeek,
            DailyAt = dailyAt
        };
    }

    public static NotificationSchedule Batched(TimeSpan batchWindow)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Batched,
            BatchWindow = batchWindow
        };
    }

    public static NotificationSchedule Custom(string cronExpression)
    {
        return new NotificationSchedule
        {
            Type = ScheduleType.Custom,
            CronExpression = cronExpression
        };
    }
}