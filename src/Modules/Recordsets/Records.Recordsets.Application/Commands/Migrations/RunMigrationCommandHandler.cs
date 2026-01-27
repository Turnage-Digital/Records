using System.Text.Json;
using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Application.Migrations.Services;
using Records.Recordsets.Contracts.Jobs;
using Records.Recordsets.Domain.Enums;

namespace Records.Recordsets.Application.Commands.Migrations;

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