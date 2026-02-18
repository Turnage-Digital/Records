using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Records.Core.Contracts.Events;
using Records.Core.Infrastructure.Sql;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClocksEventProjectionRunner : EventProjectionRunner
{
    private readonly ClocksDbContext clocksDbContext;

    public ClocksEventProjectionRunner(
        ClocksDbContext clocksDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<ClocksEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        this.clocksDbContext = clocksDbContext;
    }

    protected override string ProjectionName => "clocks.projections.events";

    protected override string[]? StreamTypes => ["Clock", "ClockDefinition"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (clocksDbContext.Database.IsRelational())
        {
            await clocksDbContext.ClockProjections.ExecuteDeleteAsync(cancellationToken);
            await clocksDbContext.ClockDefinitionProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            clocksDbContext.ClockProjections.RemoveRange(clocksDbContext.ClockProjections);
            clocksDbContext.ClockDefinitionProjections.RemoveRange(clocksDbContext.ClockDefinitionProjections);
            await clocksDbContext.SaveChangesAsync(cancellationToken);
        }

        clocksDbContext.ChangeTracker.Clear();
    }
}
