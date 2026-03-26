using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Records.Core.Infrastructure.Sql.QueryCriteria;
using Records.Notifications.Contracts.Dtos;
using Records.Notifications.Contracts.Queries;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Entities;
using Records.Notifications.Infrastructure.Sql.QueryCriteria;

namespace Records.Notifications.Infrastructure.Sql;

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
        var projection = await dbContext.NotificationProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.RecipientUserId == userId,
                cancellationToken);

        if (projection is null)
        {
            return null;
        }

        var entity = await dbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var attempts = await dbContext.DeliveryAttempts
            .AsNoTracking()
            .Where(a => a.NotificationId == notificationId)
            .OrderBy(a => a.AttemptedAt)
            .Select(a => new DeliveryAttemptDto
            {
                Channel = ((NotificationChannel)a.Channel).ToString(),
                AttemptedOn = new DateTimeOffset(a.AttemptedAt, TimeSpan.Zero),
                Status = MapDeliveryStatus((DeliveryStatus)a.Status),
                FailureReason = a.FailureReason,
                AttemptNumber = a.AttemptNumber
            })
            .ToListAsync(cancellationToken);
        var metadata = DeserializeDictionary(entity.ContentTemplateDataJson);
        var history = BuildHistory(projection, attempts);

        return new NotificationDetailsDto
        {
            Id = entity.Id,
            NotificationRuleId = entity.NotificationRuleId,
            UserId = projection.RecipientUserId ?? string.Empty,
            RecordsetId = projection.RecordsetId,
            RecordId = projection.RecordId,
            Title = entity.ContentSubject,
            Body = entity.ContentBody,
            Metadata = metadata.Count > 0 ? metadata : null,
            IsRead = projection.ReadAt.HasValue,
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
        var query = dbContext.NotificationProjections
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

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
        var unreadCount = await dbContext.NotificationProjections
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
                n.ReadAt,
                n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var contentById = await LoadNotificationContentByIdAsync(
            pageRows.Select(row => row.Id).ToArray(),
            cancellationToken);

        var items = pageRows.Select(row =>
            {
                contentById.TryGetValue(row.Id, out var content);
                var metadata = DeserializeDictionary(content?.MetadataJson ?? "{}");
                return new NotificationSummaryDto
                {
                    Id = row.Id,
                    RecordsetId = row.RecordsetId,
                    RecordId = row.RecordId,
                    Title = content?.Title ?? string.Empty,
                    Body = content?.Body ?? string.Empty,
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
        var query = dbContext.NotificationProjections
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
        var pendingRows = await dbContext.NotificationProjections
            .AsNoTracking()
            .Where(n =>
                n.Status == (int)DeliveryStatus.Pending &&
                (n.ScheduledFor == null || n.ScheduledFor <= DateTime.UtcNow))
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new PendingProjectionRow(
                n.Id,
                n.TenantId,
                n.RecordsetId,
                n.RecordId,
                n.TriggerType,
                n.Channel,
                n.RecipientUserId,
                n.Status,
                n.AttemptCount,
                n.ScheduledFor))
            .ToListAsync(cancellationToken);

        var addressesById = await LoadRecipientAddressesByIdAsync(
            pendingRows.Select(row => row.Id).ToArray(),
            cancellationToken);

        return pendingRows
            .Select(row => new NotificationPendingDto(
                row.Id,
                row.TenantId,
                row.RecordsetId,
                row.RecordId,
                (NotificationTriggerType)row.TriggerType,
                (NotificationChannel)row.Channel,
                addressesById.GetValueOrDefault(row.Id, string.Empty),
                row.RecipientUserId,
                (DeliveryStatus)row.Status,
                row.AttemptCount,
                row.ScheduledFor.HasValue ? new DateTimeOffset(row.ScheduledFor.Value, TimeSpan.Zero) : null
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<NotificationPendingDto>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken
    )
    {
        if (limit <= 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var candidates = await dbContext.NotificationProjections
            .AsNoTracking()
            .Where(n =>
                n.Status == (int)DeliveryStatus.Failed &&
                n.AttemptCount > 0 &&
                n.AttemptCount < maxAttempts)
            .OrderBy(n => n.CreatedAt)
            .Take(limit * 4)
            .Select(n => new PendingProjectionRow(
                n.Id,
                n.TenantId,
                n.RecordsetId,
                n.RecordId,
                n.TriggerType,
                n.Channel,
                n.RecipientUserId,
                n.Status,
                n.AttemptCount,
                n.ScheduledFor))
            .ToListAsync(cancellationToken);

        var candidateIds = candidates.Select(c => c.Id).ToArray();

        var attempts = await dbContext.DeliveryAttempts
            .AsNoTracking()
            .Where(x => candidateIds.Contains(x.NotificationId))
            .ToListAsync(cancellationToken);

        var attemptsByNotificationId = attempts
            .GroupBy(x => x.NotificationId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(x => x.AttemptedAt).ToList());

        var addressesById = await LoadRecipientAddressesByIdAsync(
            candidateIds,
            cancellationToken);

        var eligible = candidates
            .Where(candidate =>
            {
                if (!attemptsByNotificationId.TryGetValue(candidate.Id, out var attempts) || attempts.Count == 0)
                {
                    return false;
                }

                var lastAttempt = attempts[0];
                var effectiveRetryAfter = lastAttempt.NextRetryAfter ?? retryAfter;
                return lastAttempt.AttemptedAt.Add(effectiveRetryAfter) <= now;
            })
            .Take(limit)
            .ToList();

        return eligible
            .Select(candidate => new NotificationPendingDto(
                candidate.Id,
                candidate.TenantId,
                candidate.RecordsetId,
                candidate.RecordId,
                (NotificationTriggerType)candidate.TriggerType,
                (NotificationChannel)candidate.Channel,
                addressesById.GetValueOrDefault(candidate.Id, string.Empty),
                candidate.RecipientUserId,
                (DeliveryStatus)candidate.Status,
                candidate.AttemptCount,
                candidate.ScheduledFor.HasValue ? new DateTimeOffset(candidate.ScheduledFor.Value, TimeSpan.Zero) : null
            ))
            .ToList();
    }

    private static Dictionary<string, object> DeserializeDictionary(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, SerializerOptions)
               ?? new Dictionary<string, object>();
    }

    private async Task<Dictionary<string, NotificationContentRow>> LoadNotificationContentByIdAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken
    )
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .Select(n => new NotificationContentRow(
                n.Id,
                n.ContentSubject,
                n.ContentBody,
                n.ContentTemplateDataJson))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id);
    }

    private async Task<Dictionary<string, string>> LoadRecipientAddressesByIdAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken
    )
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .Select(n => new
            {
                n.Id,
                n.RecipientAddress
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.RecipientAddress);
    }

    private static List<NotificationHistoryEntryDto> BuildHistory(
        NotificationProjectionDb projection,
        IReadOnlyList<DeliveryAttemptDto> attempts
    )
    {
        var history = new List<NotificationHistoryEntryDto>
        {
            new()
            {
                Type = "Created",
                On = new DateTimeOffset(projection.CreatedAt, TimeSpan.Zero)
            }
        };

        if (projection.QueuedAt.HasValue)
        {
            history.Add(new NotificationHistoryEntryDto
            {
                Type = "Queued",
                On = new DateTimeOffset(projection.QueuedAt.Value, TimeSpan.Zero)
            });
        }

        if (projection.DeliveredAt.HasValue)
        {
            history.Add(new NotificationHistoryEntryDto
            {
                Type = "Delivered",
                On = new DateTimeOffset(projection.DeliveredAt.Value, TimeSpan.Zero)
            });
        }

        foreach (var attempt in attempts.OrderBy(a => a.AttemptedOn))
        {
            if (attempt.Status == "Failed")
            {
                history.Add(new NotificationHistoryEntryDto
                {
                    Type = "Failed",
                    On = attempt.AttemptedOn,
                    Bag = new { attempt.FailureReason, attempt.AttemptNumber }
                });
            }
            else if (attempt.Status == "Skipped")
            {
                history.Add(new NotificationHistoryEntryDto
                {
                    Type = "Bounced",
                    On = attempt.AttemptedOn,
                    Bag = new { attempt.FailureReason, attempt.AttemptNumber }
                });
            }
        }

        if (projection.CancelledAt.HasValue)
        {
            history.Add(new NotificationHistoryEntryDto
            {
                Type = "Cancelled",
                On = new DateTimeOffset(projection.CancelledAt.Value, TimeSpan.Zero)
            });
        }

        if (projection.ReadAt.HasValue)
        {
            history.Add(new NotificationHistoryEntryDto
            {
                Type = "Read",
                On = new DateTimeOffset(projection.ReadAt.Value, TimeSpan.Zero)
            });
        }

        return history
            .OrderBy(entry => entry.On)
            .ToList();
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

    private sealed record NotificationContentRow(
        string Id,
        string Title,
        string Body,
        string MetadataJson
    );

    private sealed record PendingProjectionRow(
        string Id,
        string TenantId,
        string? RecordsetId,
        int? RecordId,
        int TriggerType,
        int Channel,
        string? RecipientUserId,
        int Status,
        int AttemptCount,
        DateTime? ScheduledFor
    );
}