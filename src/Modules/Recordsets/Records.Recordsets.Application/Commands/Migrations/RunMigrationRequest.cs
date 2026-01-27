using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Services.Migrations;

namespace Records.Recordsets.Application.Commands.Migrations;

public class RunMigrationRequest
{
    public MigrationPlan Plan { get; init; } = new();
    public MigrationMode Mode { get; init; } = MigrationMode.DryRun;
    public UlidId RequestedBy { get; init; }
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
}