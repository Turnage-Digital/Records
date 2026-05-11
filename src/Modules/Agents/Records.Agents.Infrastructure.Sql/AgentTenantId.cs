namespace Records.Agents.Infrastructure.Sql;

internal static class AgentTenantId
{
    public static string? Normalize(string? tenantId)
    {
        return string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;
    }
}
