namespace Records.Notifications.Domain.ValueObjects;

public sealed record NotificationChannelConfig
{
    public NotificationChannel Type { get; init; }
    public string? Address { get; init; }
    public Dictionary<string, string> Settings { get; init; } = new();
}