using Records.Clocks.Application.Commands;
using Records.Clocks.Domain;
using Records.Core.Domain.ValueObjects;

namespace Records.Clocks.Tests.Commands;

public sealed class CompleteClockCommandHandlerTests
{
    [Test]
    public async Task Handle_PersistsCompletedAt_WhenClockIsCompleted()
    {
        var startedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var clock = Clock.Start(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            42,
            UlidId.NewUlid(),
            startedAt,
            startedAt.AddMinutes(30),
            startedAt.AddHours(2)
        );

        var unitOfWork = new FakeClocksUnitOfWork(clock);
        var handler = new CompleteClockCommandHandler(unitOfWork);
        var completedAt = DateTimeOffset.UtcNow;

        await handler.Handle(
            new CompleteClockCommand(clock.Id, completedAt),
            CancellationToken.None
        );

        Assert.That(unitOfWork.Repository.UpdatedClock, Is.Not.Null);
        Assert.That(unitOfWork.Repository.UpdatedClock!.State, Is.EqualTo(ClockState.Completed));
        Assert.That(unitOfWork.Repository.UpdatedClock.CompletedAt, Is.EqualTo(completedAt));
        Assert.That(unitOfWork.SaveChangesCalled, Is.True);
    }

    private sealed class FakeClocksUnitOfWork(Clock clock) : IClocksUnitOfWork
    {
        public FakeClockRepository Repository { get; } = new(clock);

        public bool SaveChangesCalled { get; private set; }

        public IClockDefinitionRepository ClockDefinitions => new FakeClockDefinitionRepository();

        public IClockRepository Clocks => Repository;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.FromResult(1);
        }

        public Task<int> SaveChangesAsync(bool deferDispatch, CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.FromResult(1);
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeClockRepository(Clock clock) : IClockRepository
    {
        private readonly Clock _clock = clock;

        public Clock? UpdatedClock { get; private set; }

        public Task<Clock?> GetByIdAsync(UlidId clockId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_clock.Id == clockId ? _clock : null);
        }

        public Task<Clock?> GetByRecordAndDefinitionAsync(
            UlidId recordsetId,
            int recordId,
            UlidId definitionId,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<Clock?>(null);
        }

        public Task<IReadOnlyList<Clock>> ListByRecordAsync(
            UlidId recordsetId,
            int recordId,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<IReadOnlyList<Clock>>([]);
        }

        public Task<IReadOnlyList<Clock>> GetRunningClocksPastThresholdAsync(
            DateTimeOffset asOf,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<IReadOnlyList<Clock>>([]);
        }

        public Task<IReadOnlyList<Clock>> GetRunningClocksPastDeadlineAsync(
            DateTimeOffset asOf,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<IReadOnlyList<Clock>>([]);
        }

        public Task AddAsync(Clock clock, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Clock clock, CancellationToken cancellationToken)
        {
            UpdatedClock = clock;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClockDefinitionRepository : IClockDefinitionRepository
    {
        public Task<ClockDefinition?> GetByIdAsync(UlidId definitionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ClockDefinition?>(null);
        }

        public Task<ClockDefinition?> GetByNameAsync(
            UlidId tenantId,
            string name,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<ClockDefinition?>(null);
        }

        public Task<IReadOnlyList<ClockDefinition>> ListByTenantAsync(
            UlidId tenantId,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<IReadOnlyList<ClockDefinition>>([]);
        }

        public Task AddAsync(ClockDefinition definition, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ClockDefinition definition, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}