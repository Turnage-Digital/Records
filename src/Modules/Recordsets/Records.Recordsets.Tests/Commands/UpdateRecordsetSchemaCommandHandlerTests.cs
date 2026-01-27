using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands.UpdateRecordsetSchema;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Enums;
using Records.Recordsets.Domain.Exceptions;
using Records.Recordsets.Domain.Interfaces;
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
            UlidId.NewUlid(),
            DateTimeOffset.UtcNow,
            [new Column { Name = "Name", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
        );

        var unitOfWork = new FakeRecordsetsUnitOfWork(recordset);
        var projectionWriter = new FakeProjectionWriter();
        var handler = new UpdateRecordsetSchemaCommandHandler(unitOfWork, projectionWriter);

        await handler.Handle(new UpdateRecordsetSchemaCommand(
            recordset.Id,
            [new Column { Name = "Title", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
            UlidId.NewUlid(),
            DateTimeOffset.UtcNow
        ), CancellationToken.None);

        Assert.That(unitOfWork.UpdatedRecordset, Is.Not.Null);
        Assert.That(projectionWriter.Upserted, Is.Not.Null);
    }

    [Test]
    public void Handle_WhenColumnTypeChanges_ThrowsMigrationRequired()
    {
        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            "Test",
            UlidId.NewUlid(),
            DateTimeOffset.UtcNow,
            [new Column { Name = "Amount", Type = ColumnType.Number, StorageKey = "amount" }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
        );

        var unitOfWork = new FakeRecordsetsUnitOfWork(recordset);
        var projectionWriter = new FakeProjectionWriter();
        var handler = new UpdateRecordsetSchemaCommandHandler(unitOfWork, projectionWriter);

        Assert.ThrowsAsync<MigrationRequiredException>(() =>
            handler.Handle(new UpdateRecordsetSchemaCommand(
                recordset.Id,
                [new Column { Name = "Amount", Type = ColumnType.Text, StorageKey = "amount" }],
                [new Status { Name = "Open", Color = "green" }],
                [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
                UlidId.NewUlid(),
                DateTimeOffset.UtcNow
            ), CancellationToken.None));
    }

    [Test]
    public void Handle_WhenStatusRemoved_ThrowsMigrationRequired()
    {
        var recordset = Recordset.Create(
            UlidId.NewUlid(),
            "Test",
            UlidId.NewUlid(),
            DateTimeOffset.UtcNow,
            [new Column { Name = "Name", Type = ColumnType.Text, StorageKey = "name" }],
            [new Status { Name = "Open", Color = "green" }, new Status { Name = "Closed", Color = "gray" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
        );

        var unitOfWork = new FakeRecordsetsUnitOfWork(recordset);
        var projectionWriter = new FakeProjectionWriter();
        var handler = new UpdateRecordsetSchemaCommandHandler(unitOfWork, projectionWriter);

        Assert.ThrowsAsync<MigrationRequiredException>(() =>
            handler.Handle(new UpdateRecordsetSchemaCommand(
                recordset.Id,
                [new Column { Name = "Name", Type = ColumnType.Text, StorageKey = "name" }],
                [new Status { Name = "Open", Color = "green" }],
                [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
                UlidId.NewUlid(),
                DateTimeOffset.UtcNow
            ), CancellationToken.None));
    }

    private sealed class FakeRecordsetsUnitOfWork : IRecordsetsUnitOfWork
    {
        private readonly Recordset recordset;

        public FakeRecordsetsUnitOfWork(Recordset recordset)
        {
            this.recordset = recordset;
        }

        public Recordset? UpdatedRecordset { get; private set; }

        public Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(recordset);
        }

        public Task<Recordset?> GetRecordsetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(recordset);
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

        public Task AddRecordAsync(Record record, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateRecordAsync(Record record, CancellationToken cancellationToken)
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
    }

    private sealed class FakeProjectionWriter : IRecordsetProjectionWriter
    {
        public RecordsetProjectionModel? Upserted { get; private set; }

        public Task UpsertAsync(RecordsetProjectionModel model, CancellationToken cancellationToken)
        {
            Upserted = model;
            return Task.CompletedTask;
        }

        public Task UpdateItemCountAsync(UlidId recordsetId, int itemCount, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateLastUpdatedAsync(
            UlidId recordsetId,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }
    }
}