namespace Records.Notifications.Domain.ValueObjects;

public sealed record NotificationRecipient
{
    public NotificationChannel Channel { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserId { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public static NotificationRecipient InApp(string userId)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.InApp,
            Address = userId,
            UserId = userId
        };
    }

    public static NotificationRecipient Email(string emailAddress, string? displayName = null, string? userId = null)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.Email,
            Address = emailAddress,
            DisplayName = displayName,
            UserId = userId
        };
    }

    public static NotificationRecipient Sms(string phoneNumber, string? displayName = null, string? userId = null)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.Sms,
            Address = phoneNumber,
            DisplayName = displayName,
            UserId = userId
        };
    }

    public static NotificationRecipient Push(string deviceToken, string? userId = null)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.Push,
            Address = deviceToken,
            UserId = userId
        };
    }

    public static NotificationRecipient Webhook(string url, IReadOnlyDictionary<string, string>? headers = null)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.Webhook,
            Address = url,
            Metadata = headers ?? new Dictionary<string, string>()
        };
    }
}