using MediatR;
using Records.Core.Domain.ValueObjects;
using Records.Recordsets.Domain.Services.Migrations;

namespace Records.Recordsets.Application.Commands.Migrations;

public record RunMigrationCommand(
    UlidId RecordsetId,
    MigrationPlan Plan,
    MigrationMode Mode,
    UlidId RequestedBy,
    DateTimeOffset RequestedAt
) : IRequest<MigrationResult>;