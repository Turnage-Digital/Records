using System.Text.Json.Serialization;
using Records.Notifications.Domain;

namespace Records.Notifications.Contracts.Dtos;

public sealed record NotificationSummaryDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    [JsonPropertyName("isRead")]
    public bool IsRead { get; init; }

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("recordsetId")]
    public string? RecordsetId { get; init; }

    [JsonPropertyName("recordId")]
    public int? RecordId { get; init; }

    [JsonPropertyName("metadata")]
    public object? Metadata { get; init; }
}

public sealed record NotificationListPageDto
{
    [JsonPropertyName("notifications")]
    public List<NotificationSummaryDto> Notifications { get; init; } = [];

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; init; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }
}

public sealed record NotificationDetailsDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("notificationRuleId")]
    public string? NotificationRuleId { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("recordsetId")]
    public string? RecordsetId { get; init; }

    [JsonPropertyName("recordId")]
    public int? RecordId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    [JsonPropertyName("isRead")]
    public bool IsRead { get; init; }

    [JsonPropertyName("metadata")]
    public object? Metadata { get; init; }

    [JsonPropertyName("history")]
    public List<NotificationHistoryEntryDto> History { get; init; } = [];

    [JsonPropertyName("deliveryAttempts")]
    public List<DeliveryAttemptDto> DeliveryAttempts { get; init; } = [];
}

public sealed record NotificationHistoryEntryDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("actorId")]
    public string? ActorId { get; init; }

    [JsonPropertyName("bag")]
    public object? Bag { get; init; }
}

public sealed record DeliveryAttemptDto
{
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = string.Empty;

    [JsonPropertyName("attemptedAt")]
    public DateTimeOffset AttemptedAt { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    [JsonPropertyName("attemptNumber")]
    public int AttemptNumber { get; init; }
}

public sealed record NotificationPendingDto(
    string Id,
    string TenantId,
    string? RecordsetId,
    int? RecordId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    string RecipientAddress,
    string? RecipientUserId,
    DeliveryStatus Status,
    int AttemptCount,
    DateTimeOffset? ScheduledFor
);