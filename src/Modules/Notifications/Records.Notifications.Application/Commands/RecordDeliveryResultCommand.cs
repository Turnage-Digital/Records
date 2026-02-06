using Records.Core.Application;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Application.Commands;

public sealed record RecordDeliveryResultCommand(
    UlidId NotificationId,
    bool Success,
    string? ProviderMessageId = null,
    string? ErrorMessage = null,
    bool IsPermanentFailure = false
) : RequestBase<Result>;