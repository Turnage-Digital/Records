using Records.Core.Domain;

namespace Records.Clocks.Domain.Interfaces;

public interface IClocksUnitOfWork : IUnitOfWork
{
    IClockDefinitionRepository ClockDefinitions { get; }
    IClockRepository Clocks { get; }
}
