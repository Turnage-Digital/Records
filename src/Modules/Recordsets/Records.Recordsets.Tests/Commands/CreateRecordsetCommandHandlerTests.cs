using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands.CreateRecordset;
using Records.Recordsets.Contracts.Projections;
using Records.Recordsets.Domain.Entities;
using Records.Recordsets.Domain.Enums;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.ValueObjects;

namespace Records.Recordsets.Tests.Commands;

public class CreateRecordsetCommandHandlerTests
{
    [Test]
    public async Task Handle_CreatesRecordsetAndProjection()
    {
        var unitOfWork = new FakeRecordsetsUnitOfWork();
        var projectionWriter = new FakeProjectionWriter();
        var handler = new CreateRecordsetCommandHandler(unitOfWork, projectionWriter);

        var adminId = UlidId.NewUlid();
        var id = await handler.Handle(new CreateRecordsetCommand(
            "Test",
            [new Column { Name = "Name", Type = ColumnType.Text }],
            [new Status { Name = "Open", Color = "green" }],
            [new StatusTransition { From = "Open", AllowedNext = ["Closed"] }],
            adminId,
            DateTimeOffset.UtcNow
        ), CancellationToken.None);

        Assert.That(id, Is.Not.EqualTo(default(UlidId)));
        Assert.That(unitOfWork.AddedRecordset, Is.Not.Null);
        Assert.That(projectionWriter.Upserted, Is.Not.Null);
    }

    private sealed class FakeRecordsetsUnitOfWork : IRecordsetsUnitOfWork
    {
        public Recordset? AddedRecordset { get; private set; }

        public Task<Recordset?> GetRecordsetByIdAsync(UlidId recordsetId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(null);
        }

        public Task<Recordset?> GetRecordsetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return Task.FromResult<Recordset?>(null);
        }

        public Task AddRecordsetAsync(Recordset recordset, CancellationToken cancellationToken)
        {
            AddedRecordset = recordset;
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