namespace Records.Core.Infrastructure.Sql;

public sealed record ProjectionReplayResult(
    int Processed,
    DateTimeOffset? LastUpdatedAt,
    string? LastEntityId
);