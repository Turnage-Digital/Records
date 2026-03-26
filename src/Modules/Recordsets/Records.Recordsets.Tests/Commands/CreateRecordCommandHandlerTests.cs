using MediatR;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Tests.Commands;

public class CreateRecordCommandHandlerTests
{
    [Test]
    public async Task Handle_AddsRecordAndUpdatesProjection()
    {
        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            "Test",
            [new Column { Name = "Name", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
            DateTimeOffset.UtcNow
        );

        var unitOfWork = new FakeRecordsetsUnitOfWork(recordset);
        var projectionWriter = new FakeProjectionWriter();
        var bagValidator = new FakeBagValidator();
        var handler = new CreateRecordCommandHandler(
            unitOfWork,
            projectionWriter,
            bagValidator,
            new NoopPublisher(),
            new FakeTenantContext(UlidId.NewUlid()));

        await handler.Handle(
            new CreateRecordCommand(recordset.Id, new { name = "test" }),
            CancellationToken.None);

        Assert.That(unitOfWork.AddedRecord, Is.Not.Null);
        Assert.That(projectionWriter.ItemCountUpdated, Is.True);
        Assert.That(projectionWriter.LastChangedUpdated, Is.True);
    }

    private sealed class FakeRecordsetsUnitOfWork : IRecordsetsUnitOfWork
    {
        private readonly Recordset _recordset;

        public FakeRecordsetsUnitOfWork(Recordset recordset)
        {
            _recordset = recordset;
        }

        public Record? AddedRecord { get; private set; }

        public Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(_recordset);
        }

        public Task<Recordset?> GetRecordsetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(_recordset);
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
            AddedRecord = record;
            return Task.CompletedTask;
        }

        public Task UpdateRecordAsync(
            Record record,
            UlidId actorId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }

        public Task<Record?> GetRecordByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Record?>(null);
        }

        public Task<int> GetRecordCountAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
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

    private sealed class FakeProjectionWriter : IRecordsetProjectionWriter
    {
        public bool ItemCountUpdated { get; private set; }
        public bool LastChangedUpdated { get; private set; }

        public Task UpsertAsync(RecordsetProjectionModel model, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateItemCountAsync(UlidId recordsetId, int itemCount, CancellationToken cancellationToken)
        {
            ItemCountUpdated = true;
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

    private sealed class FakeBagValidator : IRecordBagValidator
    {
        public void Validate(Recordset recordset, object bag)
        {
        }

        public void ValidateTransition(Recordset recordset, object? previousBag, object nextBag)
        {
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