using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Contracts.Dtos;
using Records.Tenants.Domain;
using Records.Users.Domain;

namespace Records.App.Server.Tests;

[TestFixture]
public sealed class TenantsEndpointTests
{
    [Test]
    public async Task List_Returns_Unauthorized_When_Anonymous()
    {
        await using var factory = new RecordsWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/tenants");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task List_Returns_Forbidden_When_User_Is_Not_GlobalAdmin()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var userId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, UlidId.NewUlid());
        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test");

        var response = await client.GetAsync("/api/tenants");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GlobalAdmin_Can_Create_List_And_Disable_Tenant()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var userId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(userId, UserRole.GlobalAdmin);
        using var client = factory.CreateAuthenticatedClient(userId, "global.admin@records.test");

        var createResponse = await client.PostAsJsonAsync("/api/tenants", new CreateTenantRequest("Acme Records"));
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await createResponse.Content.ReadFromJsonAsync<TenantSummaryDto>(EndpointTestSupport.JsonOptions);
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Status, Is.EqualTo(TenantStatus.Active));
        Assert.That(created.Name, Is.EqualTo("Acme Records"));

        var listResponse = await client.GetAsync("/api/tenants");
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var tenants =
            await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<TenantSummaryDto>>(EndpointTestSupport
                .JsonOptions);
        Assert.That(tenants, Is.Not.Null);
        Assert.That(tenants!.Any(t => t.TenantId == created.TenantId), Is.True);

        var disableResponse = await client.PostAsync($"/api/tenants/{created.TenantId}/disable", null);
        Assert.That(disableResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await client.GetAsync($"/api/tenants/{created.TenantId}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var disabled = await getResponse.Content.ReadFromJsonAsync<TenantSummaryDto>(EndpointTestSupport.JsonOptions);
        Assert.That(disabled, Is.Not.Null);
        Assert.That(disabled!.Status, Is.EqualTo(TenantStatus.Disabled));
    }

    private sealed record CreateTenantRequest(string Name);
}