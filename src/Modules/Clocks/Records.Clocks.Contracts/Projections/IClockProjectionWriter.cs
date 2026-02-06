using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Contracts.Projections;

public interface IClockProjectionWriter
{
    Task UpsertAsync(ClockProjectionModel model, CancellationToken cancellationToken);
    Task UpdateStateAsync(UlidId clockId, ClockState state, CancellationToken cancellationToken);

    Task UpdateAtRiskAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset atRiskAt,
        CancellationToken cancellationToken
    );

    Task UpdateBreachedAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset breachedAt,
        CancellationToken cancellationToken
    );

    Task UpdateCompletedAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    );

    Task UpdatePauseInfoAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset pausedAt,
        string reason,
        CancellationToken cancellationToken
    );

    Task UpdateResumeInfoAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset atRiskDueAt,
        DateTimeOffset breachDueAt,
        TimeSpan accumulatedPauseTime,
        CancellationToken cancellationToken
    );
}

public sealed record ClockProjectionModel(
    UlidId ClockId,
    UlidId RecordsetId,
    int RecordId,
    UlidId TenantId,
    UlidId DefinitionId,
    ClockState State,
    DateTimeOffset StartedAt,
    DateTimeOffset AtRiskDueAt,
    DateTimeOffset BreachDueAt
);