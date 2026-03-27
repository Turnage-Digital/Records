using Records.Core.Domain.ValueObjects;
using Records.Tenants.Application.EventHandlers;
using Records.Tenants.Contracts.Projections;
using Records.Tenants.Domain;
using Records.Tenants.Domain.Events;

namespace Records.Tenants.Tests.Commands;

public class TenantProjectionHandlerTests
{
    [Test]
    public async Task Handle_UpsertsProjection_WhenTenantCreated()
    {
        var projectionWriter = new FakeTenantProjectionWriter();
        var handler = new TenantProjectionHandler(projectionWriter);
        var occurredAt = DateTimeOffset.UtcNow;

        await handler.Handle(
            new TenantCreated(UlidId.NewUlid(), "Acme", TenantStatus.Active, occurredAt),
            CancellationToken.None);

        Assert.That(projectionWriter.Upserted, Is.Not.Null);
        Assert.That(projectionWriter.Upserted!.CreatedAt, Is.EqualTo(occurredAt));
    }

    [Test]
    public async Task Handle_UpdatesProjectionStatus_WhenTenantDisabled()
    {
        var projectionWriter = new FakeTenantProjectionWriter();
        var handler = new TenantProjectionHandler(projectionWriter);
        var tenantId = UlidId.NewUlid();

        await handler.Handle(new TenantDisabled(tenantId), CancellationToken.None);

        Assert.That(projectionWriter.UpdatedTenantId, Is.EqualTo(tenantId));
        Assert.That(projectionWriter.UpdatedStatus, Is.EqualTo(TenantStatus.Disabled));
    }

    private sealed class FakeTenantProjectionWriter : ITenantProjectionWriter
    {
        public TenantProjectionModel? Upserted { get; private set; }
        public UlidId? UpdatedTenantId { get; private set; }
        public TenantStatus? UpdatedStatus { get; private set; }

        public Task UpsertAsync(TenantProjectionModel model, CancellationToken cancellationToken)
        {
            Upserted = model;
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(UlidId tenantId, TenantStatus status, CancellationToken cancellationToken)
        {
            UpdatedTenantId = tenantId;
            UpdatedStatus = status;
            return Task.CompletedTask;
        }

        public Task UpdateNameAsync(UlidId tenantId, string name, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}