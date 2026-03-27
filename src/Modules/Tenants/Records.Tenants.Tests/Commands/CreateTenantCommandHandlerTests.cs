using Records.Core.Domain.ValueObjects;
using Records.Tenants.Application.Commands;
using Records.Tenants.Domain;
using Records.Tenants.Domain.Events;

namespace Records.Tenants.Tests.Commands;

public class CreateTenantCommandHandlerTests
{
    [Test]
    public async Task Handle_CreatesTenantAndWritesProjection()
    {
        var unitOfWork = new FakeTenantsUnitOfWork();
        var handler = new CreateTenantCommandHandler(unitOfWork);

        var tenantId = await handler.Handle(new CreateTenantCommand("Acme"), CancellationToken.None);

        Assert.That(tenantId, Is.Not.EqualTo(default(UlidId)));
        Assert.That(unitOfWork.AddedTenant, Is.Not.Null);
        Assert.That(unitOfWork.AddedTenant!.Name, Is.EqualTo("Acme"));
        Assert.That(unitOfWork.AddedTenant!.Status, Is.EqualTo(TenantStatus.Active));
        Assert.That(unitOfWork.AddedTenant.DomainEvents.OfType<TenantCreated>().Single().TenantId,
            Is.EqualTo(tenantId));
    }

    private sealed class FakeTenantsUnitOfWork : ITenantsUnitOfWork
    {
        public Tenant? AddedTenant { get; private set; }

        public void AddTenant(Tenant tenant)
        {
            AddedTenant = tenant;
        }

        public Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Tenant?>(null);
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