using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Domain;

public sealed class AgentThread
{
    public AgentThread(
        UlidId id,
        string userId,
        string? tenantId,
        string backendId,
        string title,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt
    )
    {
        Id = id;
        UserId = userId;
        TenantId = tenantId;
        BackendId = backendId;
        Title = title;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public UlidId Id { get; }
    public string UserId { get; }
    public string? TenantId { get; }
    public string BackendId { get; }
    public string Title { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static AgentThread Create(
        UlidId id,
        string userId,
        string? tenantId,
        string backendId,
        string? title,
        DateTimeOffset now
    )
    {
        var trimmedTitle = string.IsNullOrWhiteSpace(title) ? "New conversation" : title.Trim();
        return new AgentThread(id, userId, tenantId, backendId, trimmedTitle, now, now);
    }

    public void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
    }

    public void UpdateTitle(string title, DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title.Trim();
        }

        UpdatedAt = now;
    }
}
