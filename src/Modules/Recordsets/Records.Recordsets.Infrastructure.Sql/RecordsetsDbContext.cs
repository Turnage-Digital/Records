using Microsoft.EntityFrameworkCore;
using Records.Recordsets.Domain.Enums;
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

        modelBuilder.Entity<RecordsetDb>(entity =>
        {
            entity.ToTable("recordsets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(26).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(26).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(26);
        });

        modelBuilder.Entity<RecordsetColumnDb>(entity =>
        {
            entity.ToTable("recordset_columns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Type).HasConversion<int>().IsRequired();
            entity.Property(x => x.AllowedValuesJson).HasColumnType("longtext");
            entity.Property(x => x.Regex).HasMaxLength(512);
            entity.HasIndex(x => new { x.RecordsetId, x.StorageKey }).IsUnique();
        });

        modelBuilder.Entity<RecordsetStatusDb>(entity =>
        {
            entity.ToTable("recordset_statuses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.RecordsetId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<RecordsetStatusTransitionDb>(entity =>
        {
            entity.ToTable("recordset_status_transitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.From).HasMaxLength(128).IsRequired();
            entity.Property(x => x.AllowedNextJson).HasColumnType("longtext");
        });

        modelBuilder.Entity<RecordDb>(entity =>
        {
            entity.ToTable("recordset_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.BagJson).HasColumnType("longtext");
            entity.Property(x => x.CreatedBy).HasMaxLength(26).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(26);
            entity.HasIndex(x => new { x.RecordsetId, x.Id }).IsUnique();
        });

        modelBuilder.Entity<RecordsetProjectionDb>(entity =>
        {
            entity.ToTable("recordset_projections");
            entity.HasKey(x => x.RecordsetId);
            entity.Property(x => x.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ItemCount).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<RecordsetMigrationJobDb>(entity =>
        {
            entity.ToTable("recordset_migration_jobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(26).IsRequired();
            entity.Property(x => x.SourceRecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.BackupRecordsetId).HasMaxLength(26);
            entity.Property(x => x.NewRecordsetId).HasMaxLength(26);
            entity.Property(x => x.RequestedBy).HasMaxLength(26).IsRequired();
            entity.Property(x => x.PlanJson).IsRequired();
            entity.Property(x => x.CreatedOn).IsRequired();
            entity.Property(x => x.Stage)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(RecordsetMigrationJobStage.Pending);
            entity.HasIndex(x => new { x.Stage, x.AvailableAfter });
            entity.HasIndex(x => x.CorrelationId).IsUnique();
        });
    }
}