namespace Records.App.Server.Services;

public sealed class DevelopmentSeedOptions
{
    public bool Enabled { get; set; }
    public string GlobalAdminEmail { get; set; } = "admin@records.local";
    public string GlobalAdminPassword { get; set; } = "ChangeMe123!";
    public string GlobalAdminDisplayName { get; set; } = "Records Admin";
    public string? TenantName { get; set; } = "Demo Tenant";
}
