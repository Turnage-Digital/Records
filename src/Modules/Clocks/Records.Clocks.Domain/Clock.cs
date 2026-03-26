using Records.Clocks.Domain.Events;
using Records.Core.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Domain;

public sealed class Clock : AggregateRoot
{
    private Clock()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId TenantId { get; private set; }
    public UlidId RecordsetId { get; private set; }
    public int RecordId { get; private set; }
    public UlidId DefinitionId { get; private set; }
    public ClockState State { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset AtRiskDueAt { get; private set; }
    public DateTimeOffset BreachDueAt { get; private set; }
    public DateTimeOffset? AtRiskAt { get; private set; }
    public DateTimeOffset? BreachedAt { get; private set; }
    public DateTimeOffset? PausedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? PauseReason { get; private set; }
    public TimeSpan AccumulatedPauseTime { get; private set; }

    public bool IsTerminal => State is ClockState.Completed or ClockState.Breached;
    public bool IsActive => State is ClockState.Running or ClockState.AtRisk;

    public static Clock Start(
        UlidId id,
        UlidId tenantId,
        UlidId recordsetId,
        int recordId,
        UlidId definitionId,
        DateTimeOffset startedAt,
        DateTimeOffset atRiskDueAt,
        DateTimeOffset breachDueAt
    )
    {
        var clock = new Clock
        {
            Id = id,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId,
            DefinitionId = definitionId,
            State = ClockState.Running,
            StartedAt = startedAt,
            AtRiskDueAt = atRiskDueAt,
            BreachDueAt = breachDueAt,
            AccumulatedPauseTime = TimeSpan.Zero
        };

        clock.AddDomainEvent(new ClockStarted(id, tenantId, recordsetId, recordId, definitionId, startedAt, atRiskDueAt,
            breachDueAt));
        return clock;
    }

    public static Clock Rehydrate(
        UlidId id,
        UlidId tenantId,
        UlidId recordsetId,
        int recordId,
        UlidId definitionId,
        ClockState state,
        DateTimeOffset startedAt,
        DateTimeOffset atRiskDueAt,
        DateTimeOffset breachDueAt,
        DateTimeOffset? atRiskAt,
        DateTimeOffset? breachedAt,
        DateTimeOffset? pausedAt,
        DateTimeOffset? completedAt,
        string? pauseReason,
        TimeSpan accumulatedPauseTime
    )
    {
        return new Clock
        {
            Id = id,
            TenantId = tenantId,
            RecordsetId = recordsetId,
            RecordId = recordId,
            DefinitionId = definitionId,
            State = state,
            StartedAt = startedAt,
            AtRiskDueAt = atRiskDueAt,
            BreachDueAt = breachDueAt,
            AtRiskAt = atRiskAt,
            BreachedAt = breachedAt,
            PausedAt = pausedAt,
            CompletedAt = completedAt,
            PauseReason = pauseReason,
            AccumulatedPauseTime = accumulatedPauseTime
        };
    }

    public void Pause(string reason, DateTimeOffset pausedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State is ClockState.Completed or ClockState.Breached)
        {
            return;
        }

        if (State == ClockState.Paused)
        {
            PauseReason = reason;
            return;
        }

        PausedAt = pausedAt;
        PauseReason = reason;
        State = ClockState.Paused;

        AddDomainEvent(new ClockPaused(Id, TenantId, RecordsetId, RecordId, DefinitionId, reason, pausedAt));
    }

    public void Resume(DateTimeOffset resumedAt)
    {
        if (State != ClockState.Paused)
        {
            return;
        }

        if (PausedAt is null)
        {
            throw new InvalidOperationException("Clock is paused but has no pause timestamp.");
        }

        var pauseDuration = resumedAt - PausedAt.Value;
        AccumulatedPauseTime += pauseDuration;
        AtRiskDueAt = AtRiskDueAt.Add(pauseDuration);
        BreachDueAt = BreachDueAt.Add(pauseDuration);

        State = AtRiskAt.HasValue ? ClockState.AtRisk : ClockState.Running;
        PausedAt = null;
        PauseReason = null;

        AddDomainEvent(new ClockResumed(Id, TenantId, RecordsetId, RecordId, DefinitionId, resumedAt, pauseDuration));
    }

    public void MarkAtRisk(DateTimeOffset atRiskAt)
    {
        if (State is not (ClockState.Running or ClockState.Paused))
        {
            return;
        }

        AtRiskAt = atRiskAt;

        if (State == ClockState.Running)
        {
            State = ClockState.AtRisk;
        }

        AddDomainEvent(new ClockAtRisk(Id, TenantId, RecordsetId, RecordId, DefinitionId, atRiskAt, BreachDueAt));
    }

    public void MarkBreached(DateTimeOffset breachedAt)
    {
        if (State is ClockState.Completed or ClockState.Breached)
        {
            return;
        }

        BreachedAt = breachedAt;
        State = ClockState.Breached;

        AddDomainEvent(new ClockBreached(Id, TenantId, RecordsetId, RecordId, DefinitionId, breachedAt, BreachDueAt));
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (State is ClockState.Completed or ClockState.Breached)
        {
            return;
        }

        CompletedAt = completedAt;
        var wasAtRisk = AtRiskAt.HasValue;
        var totalElapsed = completedAt - StartedAt - AccumulatedPauseTime;
        State = ClockState.Completed;

        AddDomainEvent(new ClockCompleted(Id, TenantId, RecordsetId, RecordId, DefinitionId, completedAt, BreachDueAt,
            wasAtRisk, totalElapsed));
    }

    public override string GetStreamId()
    {
        return $"{GetStreamType()}:{Id}";
    }

    public override string GetStreamType()
    {
        return "Clock";
    }
}
