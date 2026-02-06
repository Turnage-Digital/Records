using Microsoft.EntityFrameworkCore;
using Records.Clocks.Infrastructure.Sql.Entities;

namespace Records.Clocks.Infrastructure.Sql;

public sealed class ClocksDbContext(DbContextOptions<ClocksDbContext> options)
    : DbContext(options)
{
    public DbSet<ClockDefinitionDb> ClockDefinitions => Set<ClockDefinitionDb>();
    public DbSet<ClockDb> Clocks => Set<ClockDb>();
    public DbSet<ClockDefinitionProjectionDb> ClockDefinitionProjections => Set<ClockDefinitionProjectionDb>();
    public DbSet<ClockProjectionDb> ClockProjections => Set<ClockProjectionDb>();
    public DbSet<HolidayDb> Holidays => Set<HolidayDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClockDefinitionDb>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(26).IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(26).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(26);
            entity.Property(x => x.AtRiskThresholdUnit).HasConversion<int>();
            entity.Property(x => x.BreachThresholdUnit).HasConversion<int>();
        });

        modelBuilder.Entity<ClockDb>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(26).IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.DefinitionId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.State).HasConversion<int>();
            entity.Property(x => x.PauseReason).HasMaxLength(512);
        });

        modelBuilder.Entity<ClockDefinitionProjectionDb>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(26).IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.AtRiskThresholdUnit).HasConversion<int>();
            entity.Property(x => x.BreachThresholdUnit).HasConversion<int>();
        });

        modelBuilder.Entity<ClockProjectionDb>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(26).IsRequired();
            entity.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.DefinitionId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.State).HasConversion<int>();
            entity.Property(x => x.PauseReason).HasMaxLength(512);
        });

        modelBuilder.Entity<HolidayDb>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(26);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
        });
    }
}