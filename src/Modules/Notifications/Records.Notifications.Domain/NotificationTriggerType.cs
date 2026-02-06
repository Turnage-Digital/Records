namespace Records.Notifications.Domain;

public enum NotificationTriggerType
{
    ItemCreated,
    ItemDeleted,
    ItemUpdated,
    StatusChanged,
    ColumnValueChanged,
    ListDeleted,
    ListUpdated,
    CustomCondition,
    ClockAtRisk,
    ClockBreached,
    NotificationFailed
}