namespace Records.Notifications.Domain;

public enum DeliveryStatus
{
    Pending,
    Queued,
    Delivered,
    Failed,
    Bounced,
    Cancelled
}