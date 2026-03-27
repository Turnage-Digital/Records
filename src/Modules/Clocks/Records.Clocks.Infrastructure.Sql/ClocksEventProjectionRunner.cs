using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Records.Core.Contracts;
using Records.Core.Infrastructure.Sql;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClocksEventProjectionRunner : EventProjectionRunner
{
    private readonly ClocksDbContext _clocksDbContext;

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
        _clocksDbContext = clocksDbContext;
    }

    protected override string ProjectionName => "clocks.projections.events";

    protected override string[]? StreamTypes => ["Clock", "ClockDefinition"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_clocksDbContext.Database.IsRelational())
        {
            await _clocksDbContext.ClockProjections.ExecuteDeleteAsync(cancellationToken);
            await _clocksDbContext.ClockDefinitionProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _clocksDbContext.ClockProjections.RemoveRange(_clocksDbContext.ClockProjections);
            _clocksDbContext.ClockDefinitionProjections.RemoveRange(_clocksDbContext.ClockDefinitionProjections);
            await _clocksDbContext.SaveChangesAsync(cancellationToken);
        }

        _clocksDbContext.ChangeTracker.Clear();
    }
}