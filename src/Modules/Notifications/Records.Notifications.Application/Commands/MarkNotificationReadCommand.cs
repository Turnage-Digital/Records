using Records.Core.Application;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands;

public sealed record MarkNotificationReadCommand(
    UlidId NotificationId,
    string ReaderUserId,
    DateTimeOffset ReadAt
) : RequestBase<Result>;