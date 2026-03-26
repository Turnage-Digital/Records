using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        ConfigureNotifications(modelBuilder.Entity<NotificationDb>());
        ConfigureDeliveryAttempts(modelBuilder.Entity<DeliveryAttemptDb>());
        ConfigureNotificationProjections(modelBuilder.Entity<NotificationProjectionDb>());
        ConfigureNotificationRules(modelBuilder.Entity<NotificationRuleDb>());
    }

    private static void ConfigureNotifications(EntityTypeBuilder<NotificationDb> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(26).IsRequired();
        builder.Property(e => e.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(e => e.RecordsetId).HasMaxLength(26);
        builder.Property(e => e.NotificationRuleId).HasMaxLength(26);
        builder.Property(e => e.RecipientAddress).HasMaxLength(512).IsRequired();
        builder.Property(e => e.RecipientDisplayName).HasMaxLength(256);
        builder.Property(e => e.RecipientMetadataJson).HasColumnType("JSON");
        builder.Property(e => e.RecipientUserId).HasMaxLength(450);
        builder.Property(e => e.ContentSubject).HasMaxLength(512).IsRequired();
        builder.Property(e => e.ContentBody).HasColumnType("TEXT");
        builder.Property(e => e.ContentTemplateId).HasMaxLength(128);
        builder.Property(e => e.ContentTemplateDataJson).HasColumnType("JSON");
        builder.Property(e => e.ScheduleJson).HasColumnType("JSON");
        builder.Property(e => e.CorrelationId).HasMaxLength(64);
        builder.Property(e => e.ScheduledFor).HasColumnType("datetime(6)");
        builder.Property(e => e.ReadAt).HasColumnType("datetime(6)");

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.RecordsetId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.ScheduledFor });
        builder.HasIndex(e => e.RecipientUserId);
        builder.HasIndex(e => new { e.RecipientUserId, e.ReadAt });
        builder.HasIndex(e => e.CorrelationId);

        builder.HasOne(e => e.NotificationRule)
            .WithMany(r => r.Notifications)
            .HasForeignKey(e => e.NotificationRuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureDeliveryAttempts(EntityTypeBuilder<DeliveryAttemptDb> builder)
    {
        builder.ToTable("NotificationDeliveryAttempts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.NotificationId).HasMaxLength(26).IsRequired();
        builder.Property(e => e.ProviderMessageId).HasMaxLength(256);
        builder.Property(e => e.FailureReason).HasMaxLength(1024);
        builder.Property(e => e.AttemptedAt).HasColumnType("datetime(6)");

        builder.HasOne(e => e.Notification)
            .WithMany(n => n.DeliveryAttempts)
            .HasForeignKey(e => e.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.NotificationId);
        builder.HasIndex(e => e.AttemptedAt);
    }

    private static void ConfigureNotificationProjections(EntityTypeBuilder<NotificationProjectionDb> builder)
    {
        builder.ToTable("NotificationProjections");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(26).IsRequired();
        builder.Property(e => e.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(e => e.RecordsetId).HasMaxLength(26);
        builder.Property(e => e.RecipientUserId).HasMaxLength(450);
        builder.Property(e => e.LastFailureReason).HasMaxLength(1024);
        builder.Property(e => e.ProviderMessageId).HasMaxLength(256);
        builder.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(e => e.ScheduledFor).HasColumnType("datetime(6)");
        builder.Property(e => e.QueuedAt).HasColumnType("datetime(6)");
        builder.Property(e => e.DeliveredAt).HasColumnType("datetime(6)");
        builder.Property(e => e.FailedAt).HasColumnType("datetime(6)");
        builder.Property(e => e.BouncedAt).HasColumnType("datetime(6)");
        builder.Property(e => e.CancelledAt).HasColumnType("datetime(6)");
        builder.Property(e => e.ReadAt).HasColumnType("datetime(6)");

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.RecordsetId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.CreatedAt });
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.RecipientUserId, e.Status });
    }

    private static void ConfigureNotificationRules(EntityTypeBuilder<NotificationRuleDb> builder)
    {
        builder.ToTable("NotificationRules");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasMaxLength(26).IsRequired();
        builder.Property(e => e.TenantId).HasMaxLength(26).IsRequired();
        builder.Property(e => e.RecordsetId).HasMaxLength(26).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        builder.Property(e => e.TriggerJson).HasColumnType("JSON");
        builder.Property(e => e.ChannelsJson).HasColumnType("JSON");
        builder.Property(e => e.ScheduleJson).HasColumnType("JSON");
        builder.Property(e => e.TemplateId).HasMaxLength(128);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.RecordsetId);
        builder.HasIndex(e => new { e.RecordsetId, e.IsActive, e.IsDeleted });
        builder.HasIndex(e => new { e.RecordsetId, e.TriggerType });
    }
}