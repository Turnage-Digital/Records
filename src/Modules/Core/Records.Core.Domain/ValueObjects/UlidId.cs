using System.Text.Json.Serialization;

namespace Records.Core.Domain.ValueObjects;

[JsonConverter(typeof(UlidIdJsonConverter))]
public readonly record struct UlidId
{
    private UlidId(Ulid value)
    {
        Value = value;
    }

    public Ulid Value { get; }

    public static UlidId NewUlid()
    {
        return new UlidId(Ulid.NewUlid());
    }

    public static bool TryParse(string? value, out UlidId ulid)
    {
        if (!string.IsNullOrWhiteSpace(value) && Ulid.TryParse(value, out var parsed))
        {
            ulid = new UlidId(parsed);
            return true;
        }

        ulid = default;
        return false;
    }

    public static UlidId Parse(string value)
    {
        if (TryParse(value, out var ulid))
        {
            return ulid;
        }

        throw new FormatException($"Value '{value}' is not a valid ULID.");
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator string(UlidId id)
    {
        return id.ToString();
    }

    public static implicit operator Ulid(UlidId id)
    {
        return id.Value;
    }
}
