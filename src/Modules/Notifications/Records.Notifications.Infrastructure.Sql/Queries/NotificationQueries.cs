using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.Specifications;
using Records.Notifications.Contracts;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Entities;
using Records.Notifications.Infrastructure.Sql.Specifications;

namespace Records.Notifications.Infrastructure.Sql.Queries;

public sealed class NotificationQueries(NotificationsDbContext dbContext)
    : INotificationQueries
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<NotificationDetailsDto?> GetByIdAsync(
        string notificationId,
        string userId,
        CancellationToken cancellationToken
    )
    {
        var entity = await dbContext.Notifications
            .AsNoTracking()
            .AsSplitQuery()
            .Include(n => n.DeliveryAttempts)
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.RecipientUserId == userId,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var metadata = DeserializeDictionary(entity.ContentTemplateDataJson);
        var history = BuildHistory(entity);
        var attempts = entity.DeliveryAttempts
            .OrderBy(a => a.AttemptedAt)
            .Select(a => new DeliveryAttemptDto
            {
                Channel = ((NotificationChannel)a.Channel).ToString(),
                AttemptedOn = new DateTimeOffset(a.AttemptedAt, TimeSpan.Zero),
                Status = MapDeliveryStatus((DeliveryStatus)a.Status),
                FailureReason = a.FailureReason,
                AttemptNumber = a.AttemptNumber
            })
            .ToList();

        return new NotificationDetailsDto
        {
            Id = entity.Id,
            NotificationRuleId = entity.NotificationRuleId,
            UserId = entity.RecipientUserId ?? string.Empty,
            RecordsetId = entity.RecordsetId,
            RecordId = entity.RecordId,
            Title = entity.ContentSubject,
            Body = entity.ContentBody,
            Metadata = metadata.Count > 0 ? metadata : null,
            IsRead = entity.ReadAt.HasValue,
            History = history,
            DeliveryAttempts = attempts
        };
    }

    public async Task<NotificationListPageDto> GetPageAsync(
        string userId,
        string? recordsetId,
        DateTimeOffset? since,
        bool? unread,
        int pageSize,
        int page,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.Notifications.AsNoTracking().Where(n => n.RecipientUserId == userId);

        if (since.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= since.Value.UtcDateTime);
        }

        if (!string.IsNullOrWhiteSpace(recordsetId))
        {
            query = query.Where(n => n.RecordsetId == recordsetId);
        }

        if (unread.HasValue)
        {
            query = unread.Value ? query.Where(n => n.ReadAt == null) : query.Where(n => n.ReadAt != null);
        }

        var total = await query.CountAsync(cancellationToken);
        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .CountAsync(cancellationToken);

        var pageRows = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.Id,
                n.RecordsetId,
                n.RecordId,
                n.ContentSubject,
                n.ContentBody,
                n.ContentTemplateDataJson,
                n.ReadAt,
                n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = pageRows.Select(row =>
            {
                var metadata = DeserializeDictionary(row.ContentTemplateDataJson);
                return new NotificationSummaryDto
                {
                    Id = row.Id,
                    RecordsetId = row.RecordsetId,
                    RecordId = row.RecordId,
                    Title = row.ContentSubject,
                    Body = row.ContentBody,
                    Metadata = metadata.Count > 0 ? metadata : null,
                    IsRead = row.ReadAt.HasValue,
                    OccurredOn = new DateTimeOffset(row.CreatedAt, TimeSpan.Zero)
                };
            })
            .ToList();

        return new NotificationListPageDto
        {
            Notifications = items,
            TotalCount = total,
            UnreadCount = unreadCount,
            HasMore = (page + 1) * pageSize < total
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId, string? recordsetId, CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null);

        if (!string.IsNullOrWhiteSpace(recordsetId))
        {
            query = query.Where(n => n.RecordsetId == recordsetId);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationPendingDto>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken
    )
    {
        var spec = new PendingNotificationsSpec(DateTime.UtcNow, limit);
        var query = SpecificationEvaluator.GetQuery(dbContext.Notifications.AsNoTracking(), spec);

        return await query
            .Select(n => new NotificationPendingDto(
                n.Id,
                n.TenantId,
                n.RecordsetId,
                n.RecordId,
                (NotificationTriggerType)n.TriggerType,
                (NotificationChannel)n.Channel,
                n.RecipientAddress,
                n.RecipientUserId,
                (DeliveryStatus)n.Status,
                n.DeliveryAttempts.Count,
                n.ScheduledFor.HasValue ? new DateTimeOffset(n.ScheduledFor.Value, TimeSpan.Zero) : null
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationPendingDto>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var cutoff = DateTime.UtcNow.Subtract(retryAfter);
        var spec = new FailedNotificationsForRetrySpec(maxAttempts, cutoff, limit);
        var query = SpecificationEvaluator.GetQuery(dbContext.Notifications.AsNoTracking(), spec);

        return await query
            .Select(n => new NotificationPendingDto(
                n.Id,
                n.TenantId,
                n.RecordsetId,
                n.RecordId,
                (NotificationTriggerType)n.TriggerType,
                (NotificationChannel)n.Channel,
                n.RecipientAddress,
                n.RecipientUserId,
                (DeliveryStatus)n.Status,
                n.DeliveryAttempts.Count,
                n.ScheduledFor.HasValue ? new DateTimeOffset(n.ScheduledFor.Value, TimeSpan.Zero) : null
            ))
            .ToListAsync(cancellationToken);
    }

    private static Dictionary<string, object> DeserializeDictionary(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, SerializerOptions)
               ?? new Dictionary<string, object>();
    }

    private static List<NotificationHistoryEntryDto> BuildHistory(NotificationDb entity)
    {
        var history = new List<NotificationHistoryEntryDto>
        {
            new()
            {
                Type = "Created",
                On = new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
                By = entity.RecipientUserId
            }
        };

        if (entity.ProcessedAt.HasValue)
        {
            history.Add(new NotificationHistoryEntryDto
            {
                Type = "Processing",
                On = new DateTimeOffset(entity.ProcessedAt.Value, TimeSpan.Zero)
            });
        }

        if (entity.DeliveredAt.HasValue)
        {
            history.Add(new NotificationHistoryEntryDto
            {
                Type = "Delivered",
                On = new DateTimeOffset(entity.DeliveredAt.Value, TimeSpan.Zero)
            });
        }

        foreach (var attempt in entity.DeliveryAttempts.OrderBy(a => a.AttemptedAt))
        {
            if ((DeliveryStatus)attempt.Status == DeliveryStatus.Failed)
            {
                history.Add(new NotificationHistoryEntryDto
                {
                    Type = "Failed",
                    On = new DateTimeOffset(attempt.AttemptedAt, TimeSpan.Zero),
                    Bag = new { attempt.FailureReason, attempt.AttemptNumber }
                });
            }
            else if ((DeliveryStatus)attempt.Status == DeliveryStatus.Bounced)
            {
                history.Add(new NotificationHistoryEntryDto
                {
                    Type = "Bounced",
                    On = new DateTimeOffset(attempt.AttemptedAt, TimeSpan.Zero),
                    Bag = new { attempt.FailureReason, attempt.AttemptNumber }
                });
            }
        }

        if (entity.ReadAt.HasValue)
        {
            history.Add(new NotificationHistoryEntryDto
            {
                Type = "Read",
                On = new DateTimeOffset(entity.ReadAt.Value, TimeSpan.Zero),
                By = entity.RecipientUserId
            });
        }

        return history;
    }

    private static string MapDeliveryStatus(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Pending => "Pending",
            DeliveryStatus.Queued => "Processing",
            DeliveryStatus.Delivered => "Delivered",
            DeliveryStatus.Failed => "Failed",
            DeliveryStatus.Bounced => "Skipped",
            DeliveryStatus.Cancelled => "Skipped",
            _ => "Pending"
        };
    }
}