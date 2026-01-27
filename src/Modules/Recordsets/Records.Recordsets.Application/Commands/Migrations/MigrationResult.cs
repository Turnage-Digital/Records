using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Services.Migrations;

namespace Records.Recordsets.Application.Commands.Migrations;

public class MigrationResult
{
    public bool IsSafe { get; init; }
    public string[] Messages { get; init; } = [];
    public MigrationPlan? SuggestedPlan { get; init; }
    public UlidId? CorrelationId { get; init; }
}