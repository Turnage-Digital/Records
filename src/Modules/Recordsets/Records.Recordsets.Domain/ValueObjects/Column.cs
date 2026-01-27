using System.Text;
using Records.Recordsets.Domain.Enums;

namespace Records.Recordsets.Domain.ValueObjects;

public sealed record Column
{
    public string? StorageKey { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Property => ComputeProperty(Name);
    public ColumnType Type { get; init; }
    public bool Required { get; init; }
    public string[]? AllowedValues { get; init; }
    public decimal? MinNumber { get; init; }
    public decimal? MaxNumber { get; init; }
    public string? Regex { get; init; }

    public Column WithStorageKey(string storageKey)
    {
        return this with { StorageKey = storageKey };
    }

    private static string ComputeProperty(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var segments = SplitIntoSegments(name);
        if (segments.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append(NormalizeSegment(segments[0], true));

        for (var i = 1; i < segments.Count; i++)
        {
            builder.Append(NormalizeSegment(segments[i], false));
        }

        return builder.ToString();
    }

    private static List<string> SplitIntoSegments(string value)
    {
        var segments = new List<string>();
        var current = new StringBuilder();

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(ch);
            }
            else if (current.Length > 0)
            {
                segments.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            segments.Add(current.ToString());
        }

        return segments;
    }

    private static string NormalizeSegment(string segment, bool firstSegment)
    {
        if (string.IsNullOrEmpty(segment))
        {
            return string.Empty;
        }

        var hasLetters = segment.Any(char.IsLetter);
        if (hasLetters && segment.All(c => !char.IsLetter(c) || char.IsUpper(c)))
        {
            segment = segment.ToLowerInvariant();
        }

        if (!hasLetters)
        {
            return segment;
        }

        if (segment.Length == 1)
        {
            return firstSegment
                ? segment.ToLowerInvariant()
                : segment.ToUpperInvariant();
        }

        var firstChar = firstSegment
            ? char.ToLowerInvariant(segment[0])
            : char.ToUpperInvariant(segment[0]);

        return firstChar + segment[1..];
    }
}