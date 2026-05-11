using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;

namespace Records.App.Server.Tests;

[TestFixture]
public sealed class IdentityAccessEndpointTests
{
    [Test]
    public async Task Session_Returns_Unauthorized_When_Anonymous()
    {
        await using var factory = new RecordsWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/identity/session");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Session_Returns_User_And_Access_For_Operations_User()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);
        using var client = factory.CreateAuthenticatedClient(
            userId,
            "ops@records.test",
            name: "Operations User",
            tenantId: tenantId);

        var response = await client.GetAsync("/identity/session");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<IdentitySessionResponse>();
        Assert.That(payload, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(payload!.User.Id, Is.EqualTo(userId.ToString()));
            Assert.That(payload.User.UserId, Is.EqualTo(userId.ToString()));
            Assert.That(payload.User.Sub, Is.EqualTo(userId.ToString()));
            Assert.That(payload.User.TenantId, Is.EqualTo(tenantId.ToString()));
            Assert.That(payload.User.Email, Is.EqualTo("ops@records.test"));
            Assert.That(payload.User.Name, Is.EqualTo("Operations User"));
            Assert.That(payload.Access.IsGlobalAdmin, Is.False);
            Assert.That(payload.Access.CanAccessOps, Is.True);
        });
    }

    [Test]
    public async Task Access_Returns_Unauthorized_When_Anonymous()
    {
        await using var factory = new RecordsWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/identity/access");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Access_Returns_GlobalAdmin_And_Ops_Flags_For_GlobalAdmin()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var userId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(userId, UserRole.GlobalAdmin);
        using var client = factory.CreateAuthenticatedClient(userId, "global.admin@records.test");

        var response = await client.GetAsync("/identity/access");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<IdentityAccessResponse>();
        Assert.That(payload, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(payload!.IsGlobalAdmin, Is.True);
            Assert.That(payload.CanAccessOps, Is.True);
        });
    }

    [Test]
    public async Task Access_Returns_Ops_True_And_GlobalAdmin_False_For_Operations_User()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var userId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(userId, UserRole.Operations, tenantId);
        using var client = factory.CreateAuthenticatedClient(userId, "ops@records.test");

        var response = await client.GetAsync("/identity/access");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<IdentityAccessResponse>();
        Assert.That(payload, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(payload!.IsGlobalAdmin, Is.False);
            Assert.That(payload.CanAccessOps, Is.True);
        });
    }

    private sealed record IdentityAccessResponse(bool IsGlobalAdmin, bool CanAccessOps);

    private sealed record IdentitySessionResponse(IdentitySessionUserResponse User, IdentityAccessResponse Access);

    private sealed record IdentitySessionUserResponse(
        string? Id,
        string? UserId,
        string? Sub,
        string? TenantId,
        string? UserName,
        string? Email,
        string? Name);
}
