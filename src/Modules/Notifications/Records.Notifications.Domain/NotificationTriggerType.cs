namespace Records.Notifications.Domain;

public enum NotificationTriggerType
{
    RecordCreated,
    RecordDeleted,
    RecordUpdated,
    StatusChanged,
    ColumnValueChanged,
    RecordsetDeleted,
    RecordsetUpdated,
    CustomCondition,
    ClockAtRisk,
    ClockBreached,
    NotificationFailed
}