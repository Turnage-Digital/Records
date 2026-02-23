using Records.Clocks.Domain.Events;
using Records.Clocks.Domain.ValueObjects;
using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain;

public sealed class ClockDefinition : AggregateRoot
{
    private ClockDefinition()
    {
    }

    public ClockDefinition(
        UlidId id,
        UlidId tenantId,
        string name,
        ClockThreshold atRiskThreshold,
        ClockThreshold breachThreshold,
        UlidId createdBy,
        DateTimeOffset createdAt
    )
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        AtRiskThreshold = atRiskThreshold;
        BreachThreshold = breachThreshold;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        IsActive = true;
    }

    public UlidId Id { get; private set; }
    public UlidId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ClockThreshold AtRiskThreshold { get; private set; } = ClockThreshold.From(1, ClockThresholdUnit.Days);
    public ClockThreshold BreachThreshold { get; private set; } = ClockThreshold.From(1, ClockThresholdUnit.Days);
    public bool IsActive { get; private set; }
    public UlidId CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public UlidId? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ClockDefinition Create(
        UlidId id,
        UlidId tenantId,
        string name,
        ClockThreshold atRiskThreshold,
        ClockThreshold breachThreshold,
        UlidId createdBy,
        DateTimeOffset createdAt
    )
    {
        var definition =
            new ClockDefinition(id, tenantId, name, atRiskThreshold, breachThreshold, createdBy, createdAt);
        definition.AddDomainEvent(new ClockDefinitionCreated(id, tenantId, name, atRiskThreshold, breachThreshold,
            createdBy, createdAt));
        return definition;
    }

    public static ClockDefinition Rehydrate(
        UlidId id,
        UlidId tenantId,
        string name,
        ClockThreshold atRiskThreshold,
        ClockThreshold breachThreshold,
        bool isActive,
        UlidId createdBy,
        DateTimeOffset createdAt,
        UlidId? updatedBy,
        DateTimeOffset? updatedAt
    )
    {
        return new ClockDefinition
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            AtRiskThreshold = atRiskThreshold,
            BreachThreshold = breachThreshold,
            IsActive = isActive,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            UpdatedBy = updatedBy,
            UpdatedAt = updatedAt
        };
    }

    public void Update(
        string name,
        ClockThreshold atRiskThreshold,
        ClockThreshold breachThreshold,
        UlidId updatedBy,
        DateTimeOffset updatedAt
    )
    {
        Name = name;
        AtRiskThreshold = atRiskThreshold;
        BreachThreshold = breachThreshold;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;

        AddDomainEvent(new ClockDefinitionUpdated(Id, TenantId, name, atRiskThreshold, breachThreshold, updatedBy,
            updatedAt));
    }

    public void Disable(UlidId updatedBy, DateTimeOffset updatedAt)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;

        AddDomainEvent(new ClockDefinitionDisabled(Id, TenantId, updatedBy, updatedAt));
    }

    public override string GetStreamId()
    {
        return $"{GetStreamType()}:{Id}";
    }

    public override string GetStreamType()
    {
        return "ClockDefinition";
    }
}