using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Domain;
using Records.Clocks.Infrastructure.Sql.Entities;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Infrastructure.Sql.Projections;

public sealed class ClockProjectionWriter(
    ClocksDbContext dbContext,
    ILogger<ClockProjectionWriter> logger
) : IClockProjectionWriter
{
    public async Task UpsertAsync(ClockProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.ClockProjections
            .FirstOrDefaultAsync(x => x.Id == model.ClockId.ToString(), cancellationToken);

        if (record is null)
        {
            record = new ClockProjectionDb
            {
                Id = model.ClockId.ToString(),
                RecordsetId = model.RecordsetId.ToString(),
                RecordId = model.RecordId,
                TenantId = model.TenantId.ToString(),
                DefinitionId = model.DefinitionId.ToString(),
                State = (int)model.State,
                StartedAt = model.StartedAt.UtcDateTime,
                AtRiskDueAt = model.AtRiskDueAt.UtcDateTime,
                BreachDueAt = model.BreachDueAt.UtcDateTime
            };

            dbContext.ClockProjections.Add(record);
        }
        else
        {
            record.State = (int)model.State;
            record.StartedAt = model.StartedAt.UtcDateTime;
            record.AtRiskDueAt = model.AtRiskDueAt.UtcDateTime;
            record.BreachDueAt = model.BreachDueAt.UtcDateTime;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("ClockProjection upserted: {ClockId}", model.ClockId);
    }

    public Task UpdateStateAsync(UlidId clockId, ClockState state, CancellationToken cancellationToken)
    {
        return UpdateProjectionAsync(
            clockId,
            record => record.State = (int)state,
            cancellationToken
        );
    }

    public Task UpdateAtRiskAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset atRiskAt,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            clockId,
            record =>
            {
                record.State = (int)state;
                record.AtRiskAt = atRiskAt.UtcDateTime;
            },
            cancellationToken
        );
    }

    public Task UpdateBreachedAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset breachedAt,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            clockId,
            record =>
            {
                record.State = (int)state;
                record.BreachedAt = breachedAt.UtcDateTime;
            },
            cancellationToken
        );
    }

    public Task UpdateCompletedAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            clockId,
            record =>
            {
                record.State = (int)state;
                record.CompletedAt = completedAt.UtcDateTime;
            },
            cancellationToken
        );
    }

    public Task UpdatePauseInfoAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset pausedAt,
        string reason,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            clockId,
            record =>
            {
                record.State = (int)state;
                record.PausedAt = pausedAt.UtcDateTime;
                record.PauseReason = reason;
            },
            cancellationToken
        );
    }

    public Task UpdateResumeInfoAsync(
        UlidId clockId,
        ClockState state,
        DateTimeOffset atRiskDueAt,
        DateTimeOffset breachDueAt,
        TimeSpan accumulatedPauseTime,
        CancellationToken cancellationToken
    )
    {
        return UpdateProjectionAsync(
            clockId,
            record =>
            {
                record.State = (int)state;
                record.PausedAt = null;
                record.PauseReason = null;
                record.AtRiskDueAt = atRiskDueAt.UtcDateTime;
                record.BreachDueAt = breachDueAt.UtcDateTime;
                record.AccumulatedPauseMs = (long)accumulatedPauseTime.TotalMilliseconds;
            },
            cancellationToken
        );
    }

    private async Task UpdateProjectionAsync(
        UlidId clockId,
        Action<ClockProjectionDb> update,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.ClockProjections
            .FirstOrDefaultAsync(x => x.Id == clockId.ToString(), cancellationToken);

        if (record is null)
        {
            return;
        }

        update(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("ClockProjection updated: {ClockId}", clockId);
    }
}