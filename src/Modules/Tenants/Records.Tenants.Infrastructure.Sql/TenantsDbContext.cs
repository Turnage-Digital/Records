using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Records.Tenants.Infrastructure.Sql.Entities;

namespace Records.Tenants.Infrastructure.Sql;

public class TenantsDbContext(DbContextOptions<TenantsDbContext> options)
    : DbContext(options)
{
    public DbSet<TenantDb> Tenants => Set<TenantDb>();
    public DbSet<TenantProjectionDb> TenantProjections => Set<TenantProjectionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureTenants(modelBuilder.Entity<TenantDb>());
        ConfigureTenantProjections(modelBuilder.Entity<TenantProjectionDb>());
    }

    private static void ConfigureTenants(EntityTypeBuilder<TenantDb> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();
    }

    private static void ConfigureTenantProjections(EntityTypeBuilder<TenantProjectionDb> builder)
    {
        builder.ToTable("TenantProjections");
        builder.HasKey(x => x.TenantId);
        builder.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
    }
}
