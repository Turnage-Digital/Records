namespace Records.Core.Domain;

/// <summary>
///     Well-known system actors for audit trail purposes.
///     Background services and automated processes use these IDs
///     so auditors can identify system-initiated actions.
/// </summary>
public static class SystemActors
{
    /// <summary>
    ///     Background service that processes queued notifications.
    /// </summary>
    public const string NotificationProcessor = "SYSTEM:NotificationProcessor";
}