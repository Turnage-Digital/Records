using MediatR;
using Records.Clocks.Domain;
using Records.Core.Contracts;
using Records.Core.Contracts.Events;
using Records.Core.Infrastructure.Sql;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClocksUnitOfWork(
    ClocksDbContext context,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<ClocksDbContext>(context, mediator, eventStore, serializer, tenantContext), IClocksUnitOfWork
{
    public IClockDefinitionRepository ClockDefinitions { get; } = new ClockDefinitionRepository(context);
    public IClockRepository Clocks { get; } = new ClockRepository(context);
}