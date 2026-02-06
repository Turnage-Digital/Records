namespace Records.Clocks.Domain.Interfaces;

public interface IClocksUnitOfWork
{
    IClockDefinitionRepository ClockDefinitions { get; }
    IClockRepository Clocks { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}