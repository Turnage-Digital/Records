namespace Records.Core.Domain;

/// <summary>
///     Well-known system actors for audit trail purposes.
///     Background services and automated processes use these IDs
///     so auditors can identify system-initiated actions.
/// </summary>
public static class SystemActors
{
    /// <summary>
    ///     General system actor for automated processes that don't
    ///     have a more specific identity.
    /// </summary>
    public const string System = "SYSTEM:Automated";

    /// <summary>
    ///     Background service that marks record clocks at-risk/breached.
    /// </summary>
    public const string ClockWatchdog = "SYSTEM:ClockWatchdog";

    /// <summary>
    ///     Background service that processes queued notifications.
    /// </summary>
    public const string NotificationProcessor = "SYSTEM:NotificationProcessor";
}