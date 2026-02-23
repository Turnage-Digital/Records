using Microsoft.AspNetCore.Mvc.Testing;

namespace Records.App.Server.Tests;

public sealed class HealthCheckTests
{
    [Test]
    public async Task Health_Endpoint_Returns_Ok()
    {
        await using var factory = new RecordsWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");

        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [Test]
    public async Task Identity_Logout_Endpoint_IsReachable()
    {
        await using var factory = new RecordsWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            BaseAddress = new Uri("https://localhost")
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/identity/logout");
        request.Content = new StringContent(string.Empty);

        var response = await client.SendAsync(request);

        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
}