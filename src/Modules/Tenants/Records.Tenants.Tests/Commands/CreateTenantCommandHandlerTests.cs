using Records.Core.Domain.ValueObjects;
using Records.Tenants.Application.Commands.CreateTenant;
using Records.Tenants.Contracts;
using Records.Tenants.Domain;
using Records.Tenants.Domain.Entities;
using Records.Tenants.Domain.Interfaces;

namespace Records.Tenants.Tests.Commands;

public class CreateTenantCommandHandlerTests
{
    [Test]
    public async Task Handle_CreatesTenantAndWritesProjection()
    {
        var unitOfWork = new FakeTenantsUnitOfWork();
        var projectionWriter = new FakeTenantProjectionWriter();
        var handler = new CreateTenantCommandHandler(unitOfWork, projectionWriter);

        var tenantId = await handler.Handle(new CreateTenantCommand("Acme"), CancellationToken.None);

        Assert.That(tenantId, Is.Not.EqualTo(default(UlidId)));
        Assert.That(unitOfWork.AddedTenant, Is.Not.Null);
        Assert.That(unitOfWork.AddedTenant!.Name, Is.EqualTo("Acme"));
        Assert.That(unitOfWork.AddedTenant!.Status, Is.EqualTo(TenantStatus.Active));
        Assert.That(projectionWriter.Upserted, Is.Not.Null);
        Assert.That(projectionWriter.Upserted!.TenantId, Is.EqualTo(tenantId));
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
    }

    private sealed class FakeTenantProjectionWriter : ITenantProjectionWriter
    {
        public TenantProjectionModel? Upserted { get; private set; }

        public Task UpsertAsync(TenantProjectionModel model, CancellationToken cancellationToken)
        {
            Upserted = model;
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(UlidId tenantId, TenantStatus status, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateNameAsync(UlidId tenantId, string name, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}