using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands.CreateRecord;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Enums;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.Services;
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
            UlidId.NewUlid(),
            DateTimeOffset.UtcNow,
            [new Column { Name = "Name", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }]
        );

        var unitOfWork = new FakeRecordsetsUnitOfWork(recordset);
        var projectionWriter = new FakeProjectionWriter();
        var bagValidator = new FakeBagValidator();
        var handler = new CreateRecordCommandHandler(unitOfWork, projectionWriter, bagValidator);

        await handler.Handle(
            new CreateRecordCommand(recordset.Id, new { name = "test" }, UlidId.NewUlid(), DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.That(unitOfWork.AddedRecord, Is.Not.Null);
        Assert.That(projectionWriter.ItemCountUpdated, Is.True);
    }

    private sealed class FakeRecordsetsUnitOfWork : IRecordsetsUnitOfWork
    {
        private readonly Recordset recordset;

        public FakeRecordsetsUnitOfWork(Recordset recordset)
        {
            this.recordset = recordset;
        }

        public Record? AddedRecord { get; private set; }

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
            return Task.CompletedTask;
        }

        public Task DeleteRecordsetAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddRecordAsync(Record record, CancellationToken cancellationToken)
        {
            AddedRecord = record;
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
            return Task.FromResult(1);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeProjectionWriter : IRecordsetProjectionWriter
    {
        public bool ItemCountUpdated { get; private set; }

        public Task UpsertAsync(RecordsetProjectionModel model, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateItemCountAsync(UlidId recordsetId, int itemCount, CancellationToken cancellationToken)
        {
            ItemCountUpdated = true;
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

    private sealed class FakeBagValidator : IRecordBagValidator
    {
        public void Validate(Recordset recordset, object bag)
        {
        }

        public void ValidateTransition(Recordset recordset, object? previousBag, object nextBag)
        {
        }
    }
}