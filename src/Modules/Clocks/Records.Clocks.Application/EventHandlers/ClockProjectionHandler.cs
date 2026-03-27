using MediatR;
using Records.Clocks.Contracts.Projections;
using Records.Clocks.Contracts.Queries;
using Records.Clocks.Domain;
using Records.Clocks.Domain.Events;

namespace Records.Clocks.Application.EventHandlers;

public sealed class ClockProjectionHandler(
    IClockProjectionWriter writer,
    IClockQueries queries
) : INotificationHandler<ClockStarted>,
    INotificationHandler<ClockPaused>,
    INotificationHandler<ClockResumed>,
    INotificationHandler<ClockAtRisk>,
    INotificationHandler<ClockBreached>,
    INotificationHandler<ClockCompleted>
{
    public Task Handle(ClockAtRisk notification, CancellationToken cancellationToken)
    {
        return writer.UpdateAtRiskAsync(
            notification.ClockId,
            ClockState.AtRisk,
            notification.AtRiskAt,
            cancellationToken
        );
    }

    public Task Handle(ClockBreached notification, CancellationToken cancellationToken)
    {
        return writer.UpdateBreachedAsync(
            notification.ClockId,
            ClockState.Breached,
            notification.BreachedAt,
            cancellationToken
        );
    }

    public Task Handle(ClockCompleted notification, CancellationToken cancellationToken)
    {
        return writer.UpdateCompletedAsync(
            notification.ClockId,
            ClockState.Completed,
            notification.CompletedAt,
            cancellationToken
        );
    }

    public Task Handle(ClockPaused notification, CancellationToken cancellationToken)
    {
        return writer.UpdatePauseInfoAsync(
            notification.ClockId,
            ClockState.Paused,
            notification.PausedAt,
            notification.Reason,
            cancellationToken
        );
    }

    public async Task Handle(ClockResumed notification, CancellationToken cancellationToken)
    {
        var clock = await queries.GetByIdAsync(notification.ClockId, cancellationToken);
        if (clock is null)
        {
            await writer.UpdateStateAsync(notification.ClockId, ClockState.Running, cancellationToken);
            return;
        }

        await writer.UpdateResumeInfoAsync(
            notification.ClockId,
            clock.State,
            clock.AtRiskDueAt,
            clock.BreachDueAt,
            clock.AccumulatedPauseTime,
            cancellationToken
        );
    }

    public Task Handle(ClockStarted notification, CancellationToken cancellationToken)
    {
        var model = new ClockProjectionModel(
            notification.ClockId,
            notification.RecordsetId,
            notification.RecordId,
            notification.TenantId,
            notification.DefinitionId,
            ClockState.Running,
            notification.StartedAt,
            notification.AtRiskDueAt,
            notification.BreachDueAt
        );

        return writer.UpsertAsync(model, cancellationToken);
    }
}