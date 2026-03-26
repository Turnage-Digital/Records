using MediatR;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Tests.Commands;

public class UpdateRecordCommandHandlerTests
{
    [Test]
    public async Task Handle_UpdatesRecord()
    {
        var record = new Record(1, UlidId.NewUlid(), new { name = "old" });
        var unitOfWork = new FakeRecordsetsUnitOfWork(record);
        var bagValidator = new FakeBagValidator();
        var projectionWriter = new FakeProjectionWriter();
        var handler = new UpdateRecordCommandHandler(
            unitOfWork,
            bagValidator,
            projectionWriter,
            new NoopPublisher(),
            new FakeTenantContext(UlidId.NewUlid()));

        await handler.Handle(
            new UpdateRecordCommand(record.RecordsetId, record.Id, new { name = "new" }),
            CancellationToken.None);

        Assert.That(unitOfWork.UpdatedRecord, Is.Not.Null);
        Assert.That(projectionWriter.LastChangedUpdated, Is.True);
    }

    private sealed class FakeRecordsetsUnitOfWork : IRecordsetsUnitOfWork
    {
        private readonly Record _record;

        public FakeRecordsetsUnitOfWork(Record record)
        {
            _record = record;
        }

        public Record? UpdatedRecord { get; private set; }

        public Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(Recordset.Create(
                _record.RecordsetId,
                "Test",
                [new Column { Name = "Name", Type = ColumnType.Text }],
                [new Status { Name = "Open", Color = "green" }],
                [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
                DateTimeOffset.UtcNow
            ));
        }

        public Task<Recordset?> GetRecordsetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(null);
        }

        public Task AddRecordsetAsync(Recordset recordset, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateRecordsetAsync(Recordset recordset, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteRecordsetAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddRecordAsync(
            Record record,
            UlidId actorId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }

        public Task UpdateRecordAsync(
            Record record,
            UlidId actorId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken
        )
        {
            UpdatedRecord = record;
            return Task.CompletedTask;
        }

        public Task<Record?> GetRecordByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Record?>(_record);
        }

        public Task<int> GetRecordCountAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }

        public Task<int> SaveChangesAsync(bool deferDispatch, CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeBagValidator : IRecordBagValidator
    {
        public void Validate(Recordset recordset, object bag)
        {
        }

        public void ValidateTransition(Recordset recordset, object? previousBag, object nextBag)
        {
        }
    }

    private sealed class FakeProjectionWriter : IRecordsetProjectionWriter
    {
        public bool LastChangedUpdated { get; private set; }

        public Task UpsertAsync(RecordsetProjectionModel model, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateItemCountAsync(UlidId recordsetId, int itemCount, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateLastChangedAsync(
            UlidId recordsetId,
            DateTimeOffset lastChangedAt,
            CancellationToken cancellationToken
        )
        {
            LastChangedUpdated = true;
            return Task.CompletedTask;
        }
    }

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTenantContext(UlidId actorId) : ITenantContext
    {
        public string? TenantId => null;
        public string? ActorId => actorId.ToString();
    }
}
