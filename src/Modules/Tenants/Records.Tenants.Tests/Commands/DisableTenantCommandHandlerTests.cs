using Records.Core.Domain.ValueObjects;
using Records.Tenants.Application.Commands;
using Records.Tenants.Contracts.Projections;
using Records.Tenants.Domain;

namespace Records.Tenants.Tests.Commands;

public class DisableTenantCommandHandlerTests
{
    [Test]
    public async Task Handle_DisablesTenantAndUpdatesProjection()
    {
        var tenant = new Tenant(UlidId.NewUlid(), "Acme");
        var unitOfWork = new FakeTenantsUnitOfWork(tenant);
        var projectionWriter = new FakeTenantProjectionWriter();
        var handler = new DisableTenantCommandHandler(unitOfWork, projectionWriter);

        await handler.Handle(new DisableTenantCommand(tenant.Id), CancellationToken.None);

        Assert.That(tenant.Status, Is.EqualTo(TenantStatus.Disabled));
        Assert.That(projectionWriter.UpdatedStatus, Is.EqualTo(TenantStatus.Disabled));
    }

    [Test]
    public void Handle_WhenTenantMissing_Throws()
    {
        var unitOfWork = new FakeTenantsUnitOfWork(null);
        var projectionWriter = new FakeTenantProjectionWriter();
        var handler = new DisableTenantCommandHandler(unitOfWork, projectionWriter);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new DisableTenantCommand(UlidId.NewUlid()), CancellationToken.None));
    }

    private sealed class FakeTenantsUnitOfWork : ITenantsUnitOfWork
    {
        private readonly Tenant? tenant;

        public FakeTenantsUnitOfWork(Tenant? tenant)
        {
            this.tenant = tenant;
        }

        public void AddTenant(Tenant tenant)
        {
        }

        public Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult(tenant);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeTenantProjectionWriter : ITenantProjectionWriter
    {
        public TenantStatus? UpdatedStatus { get; private set; }

        public Task UpsertAsync(TenantProjectionModel model, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(UlidId tenantId, TenantStatus status, CancellationToken cancellationToken)
        {
            UpdatedStatus = status;
            return Task.CompletedTask;
        }

        public Task UpdateNameAsync(UlidId tenantId, string name, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}