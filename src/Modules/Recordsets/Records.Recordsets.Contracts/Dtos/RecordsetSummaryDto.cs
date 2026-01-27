using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordsetSummaryDto(
    UlidId RecordsetId,
    string Name,
    int ItemCount,
    DateTimeOffset UpdatedAt
);