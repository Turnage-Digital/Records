using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Infrastructure.Sql;

internal static class AgentJsonSerializer
{
    internal static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        options.Converters.Add(new UlidIdJsonConverter());
        return options;
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}
