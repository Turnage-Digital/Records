using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Commands.Migrations;
using Records.Recordsets.Domain.Interfaces;
using Records.Recordsets.Domain.Services.Migrations;

namespace Records.Recordsets.Application.Migrations.Services;

public interface IMigrationValidator
{
    Task<MigrationResult> ValidateAsync(UlidId recordsetId, MigrationPlan plan, CancellationToken ct);
}

public class MigrationValidator(IRecordsetsUnitOfWork unitOfWork) : IMigrationValidator
{
    public async Task<MigrationResult> ValidateAsync(UlidId recordsetId, MigrationPlan plan, CancellationToken ct)
    {
        var messages = new List<string>();
        var recordset = await unitOfWork.GetRecordsetByIdAsync(recordsetId, ct)
                        ?? throw new InvalidOperationException("Recordset not found");

        var byKey = recordset.Columns.ToDictionary(
            c => string.IsNullOrWhiteSpace(c.StorageKey) ? c.Property : c.StorageKey!,
            c => c,
            StringComparer.OrdinalIgnoreCase);

        if (plan.ChangeColumnTypes is { Length: > 0 })
        {
            foreach (var op in plan.ChangeColumnTypes)
            {
                if (!byKey.TryGetValue(op.StorageKey, out var col))
                {
                    messages.Add($"ChangeColumnType: key '{op.StorageKey}' not found");
                    continue;
                }

                if (col.Type == op.TargetType)
                {
                    messages.Add($"ChangeColumnType: key '{op.StorageKey}' already has type {op.TargetType}");
                }
            }
        }

        if (plan.RemoveStatuses is { Length: > 0 })
        {
            var statuses = recordset.Statuses.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var op in plan.RemoveStatuses)
            {
                if (!statuses.Contains(op.Name))
                {
                    messages.Add($"RemoveStatus: status '{op.Name}' not found");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(op.Replacement) && !statuses.Contains(op.Replacement))
                {
                    messages.Add($"RemoveStatus: mapping target '{op.Replacement}' not found");
                }
            }
        }

        return new MigrationResult
        {
            IsSafe = messages.Count == 0,
            Messages = messages.ToArray(),
            SuggestedPlan = plan
        };
    }
}