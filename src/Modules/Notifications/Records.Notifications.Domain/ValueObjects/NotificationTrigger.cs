using Records.Core.Domain.ValueObjects;

namespace Records.Notifications.Domain.ValueObjects;

public sealed record NotificationTrigger
{
    public NotificationTriggerType Type { get; init; }
    public UlidId TenantId { get; init; }
    public UlidId? RecordsetId { get; init; }
    public int? RecordId { get; init; }
    public string? UserId { get; init; }
    public string? FromValue { get; init; }
    public string? ToValue { get; init; }
    public string? ColumnName { get; init; }
    public string? Operator { get; init; }
    public string? Value { get; init; }
    public IReadOnlyDictionary<string, object> Context { get; init; } = new Dictionary<string, object>();

    public static NotificationTrigger ItemCreated(UlidId tenantId, UlidId recordsetId, int recordId)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ItemCreated,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId
        };
    }

    public static NotificationTrigger ItemUpdated(UlidId tenantId, UlidId recordsetId, int recordId)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ItemUpdated,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId
        };
    }

    public static NotificationTrigger ItemDeleted(UlidId tenantId, UlidId recordsetId, int recordId)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ItemDeleted,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId
        };
    }

    public static NotificationTrigger StatusChanged(
        UlidId tenantId,
        UlidId recordsetId,
        int recordId,
        string? fromState,
        string? toState
    )
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.StatusChanged,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId,
            FromValue = fromState,
            ToValue = toState
        };
    }

    public static NotificationTrigger ColumnValueChanged(
        UlidId tenantId,
        UlidId recordsetId,
        int recordId,
        string fieldName,
        string? fromValue,
        string? toValue
    )
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ColumnValueChanged,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId,
            FromValue = fromValue,
            ToValue = toValue,
            ColumnName = fieldName
        };
    }

    public static NotificationTrigger ListUpdated(UlidId tenantId, UlidId recordsetId)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ListUpdated,
            TenantId = tenantId,
            RecordsetId = recordsetId
        };
    }

    public static NotificationTrigger ListDeleted(UlidId tenantId, UlidId recordsetId)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ListDeleted,
            TenantId = tenantId,
            RecordsetId = recordsetId
        };
    }

    public static NotificationTrigger CustomCondition(
        UlidId tenantId,
        UlidId recordsetId,
        string columnName,
        string operatorType,
        string? value
    )
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.CustomCondition,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            ColumnName = columnName,
            Operator = operatorType,
            Value = value
        };
    }

    public static NotificationTrigger ClockAtRisk(UlidId tenantId, UlidId recordsetId, int recordId, string clockName)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ClockAtRisk,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId,
            Context = new Dictionary<string, object> { ["ClockName"] = clockName }
        };
    }

    public static NotificationTrigger ClockBreached(UlidId tenantId, UlidId recordsetId, int recordId, string clockName)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.ClockBreached,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId,
            Context = new Dictionary<string, object> { ["ClockName"] = clockName }
        };
    }

    public static NotificationTrigger DeliveryFailed(UlidId tenantId, UlidId notificationId, string reason)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.NotificationFailed,
            TenantId = tenantId,
            Context = new Dictionary<string, object>
            {
                ["FailedNotificationId"] = notificationId.ToString(),
                ["FailureReason"] = reason
            }
        };
    }
}