using Microsoft.EntityFrameworkCore;
using Records.Notifications.Infrastructure.Sql.Entities;

namespace Records.Notifications.Infrastructure.Sql;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public DbSet<NotificationRuleDb> NotificationRules => Set<NotificationRuleDb>();
    public DbSet<NotificationDb> Notifications => Set<NotificationDb>();
    public DbSet<DeliveryAttemptDb> DeliveryAttempts => Set<DeliveryAttemptDb>();
    public DbSet<NotificationProjectionDb> NotificationProjections => Set<NotificationProjectionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationDb>(entity =>
        {
            entity.ToTable("notifications");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(26).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.RecordsetId).HasMaxLength(26);
            entity.Property(e => e.NotificationRuleId).HasMaxLength(26);
            entity.Property(e => e.RecipientAddress).HasMaxLength(512).IsRequired();
            entity.Property(e => e.RecipientDisplayName).HasMaxLength(256);
            entity.Property(e => e.RecipientMetadataJson).HasColumnType("JSON");
            entity.Property(e => e.RecipientUserId).HasMaxLength(450);
            entity.Property(e => e.ContentSubject).HasMaxLength(512).IsRequired();
            entity.Property(e => e.ContentBody).HasColumnType("TEXT");
            entity.Property(e => e.ContentTemplateId).HasMaxLength(128);
            entity.Property(e => e.ContentTemplateDataJson).HasColumnType("JSON");
            entity.Property(e => e.ScheduleJson).HasColumnType("JSON");
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ScheduledFor).HasColumnType("datetime(6)");
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.DeliveredAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ReadAt).HasColumnType("datetime(6)");

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.RecordsetId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => new { e.Status, e.ScheduledFor });
            entity.HasIndex(e => e.RecipientUserId);
            entity.HasIndex(e => new { e.RecipientUserId, e.ReadAt });
            entity.HasIndex(e => e.CorrelationId);

            entity.HasOne(e => e.NotificationRule)
                .WithMany(r => r.Notifications)
                .HasForeignKey(e => e.NotificationRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DeliveryAttemptDb>(entity =>
        {
            entity.ToTable("notification_delivery_attempts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.NotificationId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.ProviderMessageId).HasMaxLength(256);
            entity.Property(e => e.FailureReason).HasMaxLength(1024);
            entity.Property(e => e.AttemptedAt).HasColumnType("datetime(6)");

            entity.HasOne(e => e.Notification)
                .WithMany(n => n.DeliveryAttempts)
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => e.AttemptedAt);
        });

        modelBuilder.Entity<NotificationProjectionDb>(entity =>
        {
            entity.ToTable("notification_projections");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(26).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.RecordsetId).HasMaxLength(26);
            entity.Property(e => e.RecipientUserId).HasMaxLength(450);
            entity.Property(e => e.LastFailureReason).HasMaxLength(1024);
            entity.Property(e => e.ProviderMessageId).HasMaxLength(256);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ScheduledFor).HasColumnType("datetime(6)");
            entity.Property(e => e.QueuedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.DeliveredAt).HasColumnType("datetime(6)");
            entity.Property(e => e.FailedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.BouncedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.CancelledAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ReadAt).HasColumnType("datetime(6)");

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.RecordsetId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.RecipientUserId, e.Status });
        });

        modelBuilder.Entity<NotificationRuleDb>(entity =>
        {
            entity.ToTable("notification_rules");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(26).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.RecordsetId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.TriggerJson).HasColumnType("JSON");
            entity.Property(e => e.ChannelsJson).HasColumnType("JSON");
            entity.Property(e => e.ScheduleJson).HasColumnType("JSON");
            entity.Property(e => e.TemplateId).HasMaxLength(128);
            entity.Property(e => e.CreatedBy).HasMaxLength(450).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(450);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime(6)");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RecordsetId);
            entity.HasIndex(e => new { e.RecordsetId, e.IsActive, e.IsDeleted });
            entity.HasIndex(e => new { e.RecordsetId, e.TriggerType });
        });
    }
}