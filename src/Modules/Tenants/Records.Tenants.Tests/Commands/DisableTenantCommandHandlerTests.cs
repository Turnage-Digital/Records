using Records.Core.Domain.ValueObjects;
using Records.Tenants.Application.Commands;
using Records.Tenants.Domain;
using Records.Tenants.Domain.Events;

namespace Records.Tenants.Tests.Commands;

public class DisableTenantCommandHandlerTests
{
    [Test]
    public async Task Handle_DisablesTenantAndUpdatesProjection()
    {
        var tenant = Tenant.Create(UlidId.NewUlid(), "Acme", DateTimeOffset.UtcNow);
        tenant.ClearDomainEvents();
        var unitOfWork = new FakeTenantsUnitOfWork(tenant);
        var handler = new DisableTenantCommandHandler(unitOfWork);

        await handler.Handle(new DisableTenantCommand(tenant.Id), CancellationToken.None);

        Assert.That(tenant.Status, Is.EqualTo(TenantStatus.Disabled));
        Assert.That(tenant.DomainEvents.OfType<TenantDisabled>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Handle_WhenTenantMissing_Throws()
    {
        var unitOfWork = new FakeTenantsUnitOfWork(null);
        var handler = new DisableTenantCommandHandler(unitOfWork);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new DisableTenantCommand(UlidId.NewUlid()), CancellationToken.None));
    }

    private sealed class FakeTenantsUnitOfWork : ITenantsUnitOfWork
    {
        private readonly Tenant? _tenant;

        public FakeTenantsUnitOfWork(Tenant? tenant)
        {
            _tenant = tenant;
        }

        public void AddTenant(Tenant tenant)
        {
        }

        public Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_tenant);
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