using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.Entities;

namespace Records.Core.Infrastructure.Sql;

public class CoreDbContext(DbContextOptions<CoreDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<EventDb> Events { get; set; } = null!;
    public DbSet<ProjectionCheckpointDb> ProjectionCheckpoints { get; set; } = null!;
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<DataProtectionKey>(builder => { builder.ToTable("data_protection_keys"); });

        modelBuilder.Entity<EventDb>(builder =>
        {
            builder.ToTable("events");
            builder.HasKey(e => e.Position);

            builder.Property(e => e.Position)
                .ValueGeneratedOnAdd();

            builder.Property(e => e.TenantId)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.StreamId)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.StreamType)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(e => e.Version)
                .IsRequired();

            builder.Property(e => e.EventId)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(e => e.Name)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(e => e.Payload)
                .HasColumnType("longtext")
                .IsRequired();

            builder.Property(e => e.Metadata)
                .HasColumnType("longtext");

            builder.Property(e => e.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(e => e.CorrelationId)
                .HasMaxLength(128);

            builder.Property(e => e.CausationId)
                .HasMaxLength(128);

            builder.Property(e => e.ActorId)
                .HasMaxLength(128);

            builder.Property(e => e.IdempotencyKey)
                .HasMaxLength(128)
                .IsRequired();

            builder.HasIndex(e => new { e.TenantId, e.StreamId, e.Version })
                .IsUnique()
                .HasDatabaseName("IX_events_stream_version");

            builder.HasIndex(e => new { e.TenantId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("IX_events_idempotency");

            builder.HasIndex(e => new { e.TenantId, e.StreamType, e.Position })
                .HasDatabaseName("IX_events_streamtype_position");

            builder.Property(e => e.DispatchedAt)
                .HasColumnType("datetime(6)");

            // Index for outbox processor to find undispatched events efficiently
            builder.HasIndex(e => new { e.DispatchedAt, e.Position })
                .HasDatabaseName("IX_events_outbox");
        });

        modelBuilder.Entity<ProjectionCheckpointDb>(builder =>
        {
            builder.ToTable("projection_checkpoints");
            builder.HasKey(e => new { e.ProjectionName, e.TenantId });

            builder.Property(e => e.ProjectionName)
                .HasMaxLength(160)
                .IsRequired();

            builder.Property(e => e.TenantId)
                .HasMaxLength(64)
                .HasDefaultValue("*")
                .IsRequired();

            builder.Property(e => e.Position)
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(e => e.Cursor)
                .HasColumnType("longtext");
        });
    }
}