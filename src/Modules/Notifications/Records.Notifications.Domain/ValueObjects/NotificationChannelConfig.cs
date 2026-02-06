namespace Records.Notifications.Domain.ValueObjects;

public sealed record NotificationChannelConfig
{
    public NotificationChannel Type { get; init; }
    public string? Address { get; init; }
    public Dictionary<string, string> Settings { get; init; } = new();

    public static NotificationChannelConfig Email(string address)
    {
        return new NotificationChannelConfig { Type = NotificationChannel.Email, Address = address };
    }

    public static NotificationChannelConfig Sms(string address)
    {
        return new NotificationChannelConfig { Type = NotificationChannel.Sms, Address = address };
    }

    public static NotificationChannelConfig InApp()
    {
        return new NotificationChannelConfig { Type = NotificationChannel.InApp };
    }

    public static NotificationChannelConfig Push(string deviceToken)
    {
        return new NotificationChannelConfig { Type = NotificationChannel.Push, Address = deviceToken };
    }

    public static NotificationChannelConfig Webhook(string url)
    {
        return new NotificationChannelConfig { Type = NotificationChannel.Webhook, Address = url };
    }
}