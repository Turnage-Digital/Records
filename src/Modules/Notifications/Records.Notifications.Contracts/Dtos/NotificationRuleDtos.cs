using System.Text.Json.Serialization;
using Records.Notifications.Domain;

namespace Records.Notifications.Contracts.Dtos;

public sealed record NotificationRuleDto
{
    [JsonPropertyName("trigger")]
    public NotificationTriggerDto Trigger { get; init; } = null!;

    [JsonPropertyName("channels")]
    public NotificationChannelDto[] Channels { get; init; } = [];

    [JsonPropertyName("schedule")]
    public NotificationScheduleDto Schedule { get; init; } = null!;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("templateId")]
    public string? TemplateId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("recordsetId")]
    public string RecordsetId { get; init; } = string.Empty;

    [JsonPropertyName("tenantId")]
    public string TenantId { get; init; } = string.Empty;
}

public sealed record NotificationTriggerDto
{
    [JsonPropertyName("type")]
    public NotificationTriggerType Type { get; init; }

    [JsonPropertyName("fromValue")]
    public string? FromValue { get; init; }

    [JsonPropertyName("toValue")]
    public string? ToValue { get; init; }

    [JsonPropertyName("columnName")]
    public string? ColumnName { get; init; }

    [JsonPropertyName("operator")]
    public string? Operator { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public sealed record NotificationChannelDto
{
    [JsonPropertyName("type")]
    public NotificationChannel Type { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("settings")]
    public Dictionary<string, string> Settings { get; init; } = new();
}

public sealed record NotificationScheduleDto
{
    [JsonPropertyName("type")]
    public ScheduleType Type { get; init; }

    [JsonPropertyName("delay")]
    public TimeSpan? Delay { get; init; }

    [JsonPropertyName("cronExpression")]
    public string? CronExpression { get; init; }

    [JsonPropertyName("dailyAt")]
    public TimeOnly? DailyAt { get; init; }

    [JsonPropertyName("daysOfWeek")]
    public DayOfWeek[]? DaysOfWeek { get; init; }
}