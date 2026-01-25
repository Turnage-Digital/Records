using Microsoft.EntityFrameworkCore;
using Records.Tenants.Domain.Entities;
using Records.Tenants.Infrastructure.Sql.Entities;

namespace Records.Tenants.Infrastructure.Sql;

public class TenantsDbContext(DbContextOptions<TenantsDbContext> options)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantProjectionDb> TenantProjections => Set<TenantProjectionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
        });

        modelBuilder.Entity<TenantProjectionDb>(entity =>
        {
            entity.HasKey(x => x.TenantId);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
        });
    }
}
