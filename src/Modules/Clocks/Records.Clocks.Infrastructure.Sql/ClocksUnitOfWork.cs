using Records.Clocks.Domain.Interfaces;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClocksUnitOfWork(ClocksDbContext context) : IClocksUnitOfWork
{
    public IClockDefinitionRepository ClockDefinitions { get; } = new ClockDefinitionRepository(context);
    public IClockRepository Clocks { get; } = new ClockRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}