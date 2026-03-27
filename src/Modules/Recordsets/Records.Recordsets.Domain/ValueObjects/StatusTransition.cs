namespace Records.Recordsets.Domain.ValueObjects;

public sealed record StatusTransition
{
    public string From { get; init; } = string.Empty;
    public string[] AllowedNext { get; init; } = Array.Empty<string>();
}