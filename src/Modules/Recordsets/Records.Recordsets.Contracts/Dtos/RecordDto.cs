using Records.Core.Domain.ValueObjects;

namespace Records.Recordsets.Contracts.Dtos;

public sealed record RecordDto(
    int Id,
    UlidId RecordsetId,
    string BagJson
);
