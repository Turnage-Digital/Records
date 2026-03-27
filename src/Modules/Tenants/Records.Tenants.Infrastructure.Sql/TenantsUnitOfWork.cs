using MediatR;
using Microsoft.EntityFrameworkCore;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;
using Records.Core.Infrastructure.Sql;
using Records.Tenants.Domain;
using Records.Tenants.Infrastructure.Sql.Entities;
using Records.Tenants.Infrastructure.Sql.Mappers;

namespace Records.Tenants.Infrastructure.Sql;

public sealed class TenantsUnitOfWork : UnitOfWork<TenantsDbContext>, ITenantsUnitOfWork
{
    private readonly TenantsDbContext _dbContext;
    private readonly Dictionary<string, (Tenant Domain, TenantDb Entity)> _trackedTenants = new(StringComparer.Ordinal);

    public TenantsUnitOfWork(
        TenantsDbContext dbContext,
        IMediator mediator,
        IEventStore? eventStore = null,
        IDomainEventSerializer? serializer = null,
        ITenantContext? tenantContext = null
    )
        : base(dbContext, mediator, eventStore, serializer, tenantContext)
    {
        _dbContext = dbContext;
    }

    public void AddTenant(Tenant tenant)
    {
        var entity = TenantMapper.ToDb(tenant);
        _dbContext.Tenants.Add(entity);
        _trackedTenants[entity.Id] = (tenant, entity);
    }

    public async Task<Tenant?> GetTenantByIdAsync(UlidId tenantId, CancellationToken cancellationToken)
    {
        var key = tenantId.ToString();

        if (_trackedTenants.TryGetValue(key, out var tracked))
        {
            return tracked.Domain;
        }

        var entity = await _dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == key, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var domain = TenantMapper.ToDomain(entity);
        _trackedTenants[key] = (domain, entity);
        return domain;
    }

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SyncTrackedTenants();
        return base.SaveChangesAsync(cancellationToken);
    }

    public new Task<int> SaveChangesAsync(bool deferDispatch, CancellationToken cancellationToken)
    {
        SyncTrackedTenants();
        return base.SaveChangesAsync(deferDispatch, cancellationToken);
    }

    private void SyncTrackedTenants()
    {
        foreach (var tracked in _trackedTenants.Values)
        {
            TenantMapper.UpdateDb(tracked.Domain, tracked.Entity);
        }
    }
}