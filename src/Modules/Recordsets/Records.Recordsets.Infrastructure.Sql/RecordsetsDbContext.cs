using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Records.Recordsets.Domain;
using Records.Recordsets.Infrastructure.Sql.Entities;

namespace Records.Recordsets.Infrastructure.Sql;

public class RecordsetsDbContext(DbContextOptions<RecordsetsDbContext> options)
    : DbContext(options)
{
    public DbSet<RecordsetDb> Recordsets => Set<RecordsetDb>();
    public DbSet<RecordDb> RecordsetItems => Set<RecordDb>();
    public DbSet<RecordsetColumnDb> RecordsetColumns => Set<RecordsetColumnDb>();
    public DbSet<RecordsetStatusDb> RecordsetStatuses => Set<RecordsetStatusDb>();
    public DbSet<RecordsetStatusTransitionDb> RecordsetStatusTransitions => Set<RecordsetStatusTransitionDb>();
    public DbSet<RecordsetProjectionDb> RecordsetProjections => Set<RecordsetProjectionDb>();
    public DbSet<RecordsetMigrationJobDb> RecordsetMigrationJobs => Set<RecordsetMigrationJobDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureRecordsets(modelBuilder.Entity<RecordsetDb>());
        ConfigureRecordsetColumns(modelBuilder.Entity<RecordsetColumnDb>());
        ConfigureRecordsetStatuses(modelBuilder.Entity<RecordsetStatusDb>());
        ConfigureRecordsetStatusTransitions(modelBuilder.Entity<RecordsetStatusTransitionDb>());
        ConfigureRecordsetItems(modelBuilder.Entity<RecordDb>());
        ConfigureRecordsetProjections(modelBuilder.Entity<RecordsetProjectionDb>());
        ConfigureRecordsetMigrationJobs(modelBuilder.Entity<RecordsetMigrationJobDb>());
    }

    private static void ConfigureRecordsets(EntityTypeBuilder<RecordsetDb> builder)
    {
        builder.ToTable("Recordsets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
    }

    private static void ConfigureRecordsetColumns(EntityTypeBuilder<RecordsetColumnDb> builder)
    {
        builder.ToTable("RecordsetColumns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.AllowedValuesJson).HasColumnType("longtext");
        builder.Property(x => x.Regex).HasMaxLength(512);
        builder.HasIndex(x => new { x.RecordsetId, x.StorageKey }).IsUnique();
    }

    private static void ConfigureRecordsetStatuses(EntityTypeBuilder<RecordsetStatusDb> builder)
    {
        builder.ToTable("RecordsetStatuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.RecordsetId, x.Name }).IsUnique();
    }

    private static void ConfigureRecordsetStatusTransitions(EntityTypeBuilder<RecordsetStatusTransitionDb> builder)
    {
        builder.ToTable("RecordsetStatusTransitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.From).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AllowedNextJson).HasColumnType("longtext");
    }

    private static void ConfigureRecordsetItems(EntityTypeBuilder<RecordDb> builder)
    {
        builder.ToTable("RecordsetItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.BagJson).HasColumnType("longtext");
        builder.Property(x => x.CreatedBy).HasMaxLength(26).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(26);
        builder.HasIndex(x => new { x.RecordsetId, x.Id }).IsUnique();
    }

    private static void ConfigureRecordsetProjections(EntityTypeBuilder<RecordsetProjectionDb> builder)
    {
        builder.ToTable("RecordsetProjections");
        builder.HasKey(x => x.RecordsetId);
        builder.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ItemCount).IsRequired();
        builder.Property(x => x.LastChangedAt).IsRequired();
    }

    private static void ConfigureRecordsetMigrationJobs(EntityTypeBuilder<RecordsetMigrationJobDb> builder)
    {
        builder.ToTable("RecordsetMigrationJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26).IsRequired();
        builder.Property(x => x.SourceRecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.BackupRecordsetId).HasMaxLength(26);
        builder.Property(x => x.NewRecordsetId).HasMaxLength(26);
        builder.Property(x => x.RequestedBy).HasMaxLength(26).IsRequired();
        builder.Property(x => x.PlanJson).IsRequired();
        builder.Property(x => x.CreatedOn).IsRequired();
        builder.Property(x => x.Stage)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RecordsetMigrationJobStage.Pending);
        builder.HasIndex(x => new { x.Stage, x.AvailableAfter });
        builder.HasIndex(x => x.CorrelationId).IsUnique();
    }
}
