using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Notifications.Domain;
using Records.Notifications.Infrastructure.Sql.Mappers;

namespace Records.Notifications.Infrastructure.Sql;

public sealed class NotificationRuleRepository(NotificationsDbContext context) : INotificationRuleRepository
{
    public async Task<NotificationRule?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var db = await context.NotificationRules
            .FirstOrDefaultAsync(r => r.Id == id.ToString() && !r.IsDeleted, cancellationToken);

        return db is null ? null : NotificationRuleMapper.ToDomain(db);
    }

    public async Task<IReadOnlyList<NotificationRule>> ListByRecordsetAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken = default
    )
    {
        var rules = await context.NotificationRules
            .Where(r => r.RecordsetId == recordsetId.ToString() && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        return rules.Select(NotificationRuleMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<NotificationRule>> ListActiveByRecordsetAsync(
        UlidId recordsetId,
        CancellationToken cancellationToken = default
    )
    {
        var rules = await context.NotificationRules
            .Where(r => r.RecordsetId == recordsetId.ToString() && r.IsActive && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        return rules.Select(NotificationRuleMapper.ToDomain).ToList();
    }

    public async Task AddAsync(NotificationRule rule, CancellationToken cancellationToken = default)
    {
        var db = NotificationRuleMapper.ToDb(rule);
        await context.NotificationRules.AddAsync(db, cancellationToken);
    }

    public async Task UpdateAsync(NotificationRule rule, CancellationToken cancellationToken = default)
    {
        var db = await context.NotificationRules
            .FirstOrDefaultAsync(r => r.Id == rule.Id.ToString(), cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"Notification rule '{rule.Id}' not found.");
        }

        NotificationRuleMapper.UpdateDb(db, rule);
    }
}