using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        ConfigureClockDefinitions(modelBuilder.Entity<ClockDefinitionDb>());
        ConfigureClocks(modelBuilder.Entity<ClockDb>());
        ConfigureClockDefinitionProjections(modelBuilder.Entity<ClockDefinitionProjectionDb>());
        ConfigureClockProjections(modelBuilder.Entity<ClockProjectionDb>());
        ConfigureHolidays(modelBuilder.Entity<HolidayDb>());
    }

    private static void ConfigureClockDefinitions(EntityTypeBuilder<ClockDefinitionDb> builder)
    {
        builder.ToTable("ClockDefinitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AtRiskThresholdUnit).HasConversion<int>();
        builder.Property(x => x.BreachThresholdUnit).HasConversion<int>();
    }

    private static void ConfigureClocks(EntityTypeBuilder<ClockDb> builder)
    {
        builder.ToTable("Clocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.DefinitionId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.State).HasConversion<int>();
        builder.Property(x => x.PauseReason).HasMaxLength(512);
    }

    private static void ConfigureClockDefinitionProjections(EntityTypeBuilder<ClockDefinitionProjectionDb> builder)
    {
        builder.ToTable("ClockDefinitionProjections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AtRiskThresholdUnit).HasConversion<int>();
        builder.Property(x => x.BreachThresholdUnit).HasConversion<int>();
    }

    private static void ConfigureClockProjections(EntityTypeBuilder<ClockProjectionDb> builder)
    {
        builder.ToTable("ClockProjections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26).IsRequired();
        builder.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.DefinitionId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.State).HasConversion<int>();
        builder.Property(x => x.PauseReason).HasMaxLength(512);
    }

    private static void ConfigureHolidays(EntityTypeBuilder<HolidayDb> builder)
    {
        builder.ToTable("Holidays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(26);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
    }
}