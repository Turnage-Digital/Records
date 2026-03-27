namespace Records.Notifications.Domain;

public enum ScheduleType
{
    Immediate,
    Delayed,
    Daily,
    Weekly,
    Batched,
    Custom
}