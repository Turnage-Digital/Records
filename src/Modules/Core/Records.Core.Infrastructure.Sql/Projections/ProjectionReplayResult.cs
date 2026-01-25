namespace Records.Core.Infrastructure.Sql.Projections;

public sealed record ProjectionReplayResult(
    int Processed,
    DateTimeOffset? LastUpdatedAt,
    string? LastEntityId
);