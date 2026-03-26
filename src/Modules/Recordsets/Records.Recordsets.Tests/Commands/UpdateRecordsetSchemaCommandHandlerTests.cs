using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands;
using Records.Recordsets.Domain;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Tests.Commands;

public class UpdateRecordsetSchemaCommandHandlerTests
{
    [Test]
    public async Task Handle_UpdatesSchemaAndProjection()
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
        var handler = new UpdateRecordsetSchemaCommandHandler(unitOfWork);

        await handler.Handle(new UpdateRecordsetSchemaCommand(
            recordset.Id,
            [new Column { Name = "Title", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
        ), CancellationToken.None);

        Assert.That(unitOfWork.UpdatedRecordset, Is.Not.Null);
    }

    [Test]
    public void Handle_WhenColumnTypeChanges_ThrowsMigrationRequired()
    {
        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            "Test",
            [new Column { Name = "Amount", Type = ColumnType.Number, StorageKey = "amount" }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
            DateTimeOffset.UtcNow
        );

        var unitOfWork = new FakeRecordsetsUnitOfWork(recordset);
        var handler = new UpdateRecordsetSchemaCommandHandler(unitOfWork);

        Assert.ThrowsAsync<MigrationRequiredException>(() =>
            handler.Handle(new UpdateRecordsetSchemaCommand(
                recordset.Id,
                [new Column { Name = "Amount", Type = ColumnType.Text, StorageKey = "amount" }],
                [new Status { Name = "Open", Color = "green" }],
                [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
            ), CancellationToken.None));
    }

    [Test]
    public void Handle_WhenStatusRemoved_ThrowsMigrationRequired()
    {
        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            "Test",
            [new Column { Name = "Name", Type = ColumnType.Text, StorageKey = "name" }],
            [new Status { Name = "Open", Color = "green" }, new Status { Name = "Closed", Color = "gray" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
            DateTimeOffset.UtcNow
        );

        var unitOfWork = new FakeRecordsetsUnitOfWork(recordset);
        var handler = new UpdateRecordsetSchemaCommandHandler(unitOfWork);

        Assert.ThrowsAsync<MigrationRequiredException>(() =>
            handler.Handle(new UpdateRecordsetSchemaCommand(
                recordset.Id,
                [new Column { Name = "Name", Type = ColumnType.Text, StorageKey = "name" }],
                [new Status { Name = "Open", Color = "green" }],
                [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
            ), CancellationToken.None));
    }

    private sealed class FakeRecordsetsUnitOfWork : IRecordsetsUnitOfWork
    {
        private readonly Recordset _recordset;

        public FakeRecordsetsUnitOfWork(Recordset recordset)
        {
            _recordset = recordset;
        }

        public Recordset? UpdatedRecordset { get; private set; }

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
            UpdatedRecordset = recordset;
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
            return Task.CompletedTask;
        }

        public Task<Record?> GetRecordByIdAsync(UlidId recordsetId, int recordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Record?>(null);
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
}
