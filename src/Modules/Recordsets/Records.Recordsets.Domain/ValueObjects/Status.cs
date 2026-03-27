namespace Records.Recordsets.Domain.ValueObjects;

public sealed record Status
{
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
}