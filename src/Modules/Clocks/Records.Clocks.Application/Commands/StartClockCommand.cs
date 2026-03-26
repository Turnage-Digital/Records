using MediatR;
using Records.Clocks.Contracts;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Application.Commands;

public sealed record StartClockCommand(
    UlidId TenantId,
    UlidId RecordsetId,
    int RecordId,
    UlidId DefinitionId,
    DateTimeOffset StartedAt
) : IRequest<UlidId>;

public sealed class StartClockCommandHandler(
    IClocksUnitOfWork unitOfWork,
    IBusinessCalendarService calendarService
) : IRequestHandler<StartClockCommand, UlidId>
{
    public async Task<UlidId> Handle(StartClockCommand request, CancellationToken cancellationToken)
    {
        var definition = await unitOfWork.ClockDefinitions.GetByIdAsync(request.DefinitionId, cancellationToken);
        if (definition is null || !definition.IsActive)
        {
            throw new InvalidOperationException("Clock definition not found or inactive.");
        }

        if (definition.TenantId != request.TenantId)
        {
            throw new InvalidOperationException("Clock definition does not belong to tenant.");
        }

        var existing = await unitOfWork.Clocks.GetByRecordAndDefinitionAsync(
            request.RecordsetId,
            request.RecordId,
            request.DefinitionId,
            cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("A clock for this definition already exists on the record.");
        }

        var atRiskDueAt = CalculateDueDate(
            request.StartedAt,
            definition.AtRiskThreshold.Value,
            definition.AtRiskThreshold.Unit,
            request.TenantId);

        var breachDueAt = CalculateDueDate(
            request.StartedAt,
            definition.BreachThreshold.Value,
            definition.BreachThreshold.Unit,
            request.TenantId);

        if (atRiskDueAt >= breachDueAt)
        {
            throw new InvalidOperationException("At-risk threshold must be before breach threshold.");
        }

        var clock = Clock.Start(
            UlidId.NewUlid(),
            request.TenantId,
            request.RecordsetId,
            request.RecordId,
            request.DefinitionId,
            request.StartedAt,
            atRiskDueAt,
            breachDueAt
        );

        await unitOfWork.Clocks.AddAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        return clock.Id;
    }

    private DateTimeOffset CalculateDueDate(
        DateTimeOffset start,
        int value,
        ClockThresholdUnit unit,
        UlidId tenantId
    )
    {
        return unit switch
        {
            ClockThresholdUnit.Minutes => start.AddMinutes(value),
            ClockThresholdUnit.Hours => start.AddHours(value),
            ClockThresholdUnit.Days => start.AddDays(value),
            ClockThresholdUnit.BusinessDays => calendarService.AddBusinessTime(start, value, unit, tenantId),
            _ => start.AddDays(value)
        };
    }
}
