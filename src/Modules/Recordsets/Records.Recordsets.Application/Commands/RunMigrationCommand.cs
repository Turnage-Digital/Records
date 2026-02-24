using System.Text.Json;
using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Contracts;
using Records.Recordsets.Domain;

namespace Records.Recordsets.Application.Commands;

public enum MigrationMode
{
    DryRun,
    Execute
}

public class MigrationResult
{
    public bool IsSafe { get; init; }
    public string[] Messages { get; init; } = [];
    public MigrationPlan? SuggestedPlan { get; init; }
    public UlidId? CorrelationId { get; init; }
}

public class RunMigrationRequest
{
    public MigrationPlan Plan { get; init; } = new();
    public MigrationMode Mode { get; init; } = MigrationMode.DryRun;
    public UlidId RequestedBy { get; init; }
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
}

public record RunMigrationCommand(
    UlidId RecordsetId,
    MigrationPlan Plan,
    MigrationMode Mode,
    UlidId RequestedBy,
    DateTimeOffset RequestedAt
) : IRequest<MigrationResult>;

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
            StringComparer.OrdinalIgnoreCase
        );

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

public class RunMigrationCommandHandler(
    IMigrationValidator validator,
    IRecordsetMigrationJobWriter jobWriter
) : IRequestHandler<RunMigrationCommand, MigrationResult>
{
    public async Task<MigrationResult> Handle(RunMigrationCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request.RecordsetId, request.Plan, cancellationToken);
        if (request.Mode == MigrationMode.DryRun || !validation.IsSafe)
        {
            return validation;
        }

        var correlationId = UlidId.NewUlid();
        var planJson = JsonSerializer.Serialize(request.Plan);

        await jobWriter.CreateAsync(new RecordsetMigrationJobWriteModel(
            UlidId.NewUlid(),
            request.RecordsetId,
            correlationId,
            request.RequestedBy,
            planJson,
            DateTime.UtcNow,
            RecordsetMigrationJobStage.Pending), cancellationToken);

        var messages = validation.Messages.Length > 0
            ? validation.Messages
            : ["Migration queued."];

        return new MigrationResult
        {
            IsSafe = true,
            Messages = messages,
            SuggestedPlan = validation.SuggestedPlan,
            CorrelationId = correlationId
        };
    }
}