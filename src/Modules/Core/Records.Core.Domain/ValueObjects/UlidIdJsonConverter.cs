using System.Text.Json;
using System.Text.Json.Serialization;

namespace Records.Core.Domain.ValueObjects;

/// <summary>
///     JSON converter that serializes UlidId as a plain string rather than an object.
/// </summary>
public sealed class UlidIdJsonConverter : JsonConverter<UlidId>
{
    public override UlidId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return string.IsNullOrEmpty(value)
                ? default
                : UlidId.Parse(value);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            foreach (var propertyName in new[] { "value", "Value" })
            {
                if (!root.TryGetProperty(propertyName, out var property) ||
                    property.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var value = property.GetString();
                return string.IsNullOrEmpty(value)
                    ? default
                    : UlidId.Parse(value);
            }
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing {nameof(UlidId)}.");
    }

    public override void Write(Utf8JsonWriter writer, UlidId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
