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
        ClockThreshold breachThreshold
    )
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        AtRiskThreshold = atRiskThreshold;
        BreachThreshold = breachThreshold;
        IsActive = true;
    }

    public UlidId Id { get; private set; }
    public UlidId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ClockThreshold AtRiskThreshold { get; private set; } = ClockThreshold.From(1, ClockThresholdUnit.Days);
    public ClockThreshold BreachThreshold { get; private set; } = ClockThreshold.From(1, ClockThresholdUnit.Days);
    public bool IsActive { get; private set; }

    public static ClockDefinition Create(
        UlidId id,
        UlidId tenantId,
        string name,
        ClockThreshold atRiskThreshold,
        ClockThreshold breachThreshold
    )
    {
        var definition = new ClockDefinition(id, tenantId, name, atRiskThreshold, breachThreshold);
        definition.AddDomainEvent(new ClockDefinitionCreated(
            id,
            tenantId,
            name,
            atRiskThreshold,
            breachThreshold,
            definition.IsActive));
        return definition;
    }

    public static ClockDefinition Rehydrate(
        UlidId id,
        UlidId tenantId,
        string name,
        ClockThreshold atRiskThreshold,
        ClockThreshold breachThreshold,
        bool isActive
    )
    {
        return new ClockDefinition
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            AtRiskThreshold = atRiskThreshold,
            BreachThreshold = breachThreshold,
            IsActive = isActive
        };
    }

    public void Update(
        string name,
        ClockThreshold atRiskThreshold,
        ClockThreshold breachThreshold
    )
    {
        Name = name;
        AtRiskThreshold = atRiskThreshold;
        BreachThreshold = breachThreshold;

        AddDomainEvent(new ClockDefinitionUpdated(
            Id,
            TenantId,
            name,
            atRiskThreshold,
            breachThreshold,
            IsActive));
    }

    public void Disable()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;

        AddDomainEvent(new ClockDefinitionDisabled(Id, TenantId));
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
