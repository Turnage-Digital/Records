using System.Text.Json;
using Records.Core.Domain.ValueObjects;

namespace Records.Core.Tests.ValueObjects;

[TestFixture]
public class UlidIdTests
{
    [Test]
    public void Parse_Roundtrips_String()
    {
        var original = UlidId.NewUlid();
        var parsed = UlidId.Parse(original.ToString());

        Assert.That(parsed, Is.EqualTo(original));
    }

    [Test]
    public void TryParse_Invalid_Returns_False()
    {
        var success = UlidId.TryParse("not-a-ulid", out _);
        Assert.That(success, Is.False);
    }

    [Test]
    public void JsonSerializer_ShouldSerializeAsString_WhenUsingDefaultOptions()
    {
        var id = UlidId.NewUlid();

        var json = JsonSerializer.Serialize(id);

        Assert.That(json, Is.EqualTo($"\"{id}\""));
    }

    [Test]
    public void JsonSerializer_ShouldDeserializeLegacyObjectShape_WhenValuePropertyIsPresent()
    {
        var id = UlidId.NewUlid();

        var parsed = JsonSerializer.Deserialize<UlidId>($"{{\"value\":\"{id}\"}}");

        Assert.That(parsed, Is.EqualTo(id));
    }
}
