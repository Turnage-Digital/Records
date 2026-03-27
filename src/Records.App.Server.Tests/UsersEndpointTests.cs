using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Dtos;
using Records.Users.Domain;

namespace Records.App.Server.Tests;

[TestFixture]
public sealed class UsersEndpointTests
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

        var response = await client.GetAsync("/api/users");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task List_Returns_Forbidden_For_Operations_User()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(actorId, UserRole.Operations, UlidId.NewUlid());
        using var client = factory.CreateAuthenticatedClient(actorId, "ops@records.test");

        var response = await client.GetAsync("/api/users");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GlobalAdmin_Can_Invite_User_And_List_Users()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(actorId, UserRole.GlobalAdmin);
        using var client = factory.CreateAuthenticatedClient(actorId, "global.admin@records.test");

        var tenantId = UlidId.NewUlid();
        var inviteResponse = await client.PostAsJsonAsync(
            "/api/users/invite",
            new InviteUserRequest(
                "invitee@records.test",
                "Invitee User",
                [new InviteUserRoleRequest(UserRole.Operations, tenantId.ToString())],
                DateTimeOffset.UtcNow));

        Assert.That(inviteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var invited = await inviteResponse.Content.ReadFromJsonAsync<UserSummaryDto>(EndpointTestSupport.JsonOptions);
        Assert.That(invited, Is.Not.Null);
        Assert.That(invited!.Email, Is.EqualTo("invitee@records.test"));
        Assert.That(invited.Status, Is.EqualTo(UserStatus.Invited));

        var listResponse = await client.GetAsync("/api/users");
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var users =
            await listResponse.Content
                .ReadFromJsonAsync<IReadOnlyList<UserSummaryDto>>(EndpointTestSupport.JsonOptions);
        Assert.That(users, Is.Not.Null);
        Assert.That(users!.Any(x => x.UserId == invited.UserId), Is.True);
    }

    [Test]
    public async Task TenantAdmin_Cannot_Invite_GlobalAdmin()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(actorId, UserRole.TenantAdmin, tenantId);
        using var client = factory.CreateAuthenticatedClient(actorId, "tenant.admin@records.test");

        var response = await client.PostAsJsonAsync(
            "/api/users/invite",
            new InviteUserRequest(
                "forbidden-global@records.test",
                "Forbidden Global",
                [new InviteUserRoleRequest(UserRole.GlobalAdmin, null)],
                DateTimeOffset.UtcNow));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task TenantAdmin_Can_Invite_Operations_For_Managed_Tenant()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        var managedTenantId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(actorId, UserRole.TenantAdmin, managedTenantId);
        using var client = factory.CreateAuthenticatedClient(actorId, "tenant.admin@records.test");

        var response = await client.PostAsJsonAsync(
            "/api/users/invite",
            new InviteUserRequest(
                "ops.new@records.test",
                "Ops New",
                [new InviteUserRoleRequest(UserRole.Operations, managedTenantId.ToString())],
                DateTimeOffset.UtcNow));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var invited = await response.Content.ReadFromJsonAsync<UserSummaryDto>(EndpointTestSupport.JsonOptions);
        Assert.That(invited, Is.Not.Null);
        Assert.That(invited!.Email, Is.EqualTo("ops.new@records.test"));
    }

    [Test]
    public async Task TenantAdmin_Cannot_Invite_Operations_For_Unmanaged_Tenant()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        var managedTenantId = UlidId.NewUlid();
        var unmanagedTenantId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(actorId, UserRole.TenantAdmin, managedTenantId);
        using var client = factory.CreateAuthenticatedClient(actorId, "tenant.admin@records.test");

        var response = await client.PostAsJsonAsync(
            "/api/users/invite",
            new InviteUserRequest(
                "ops.denied@records.test",
                "Ops Denied",
                [new InviteUserRoleRequest(UserRole.Operations, unmanagedTenantId.ToString())],
                DateTimeOffset.UtcNow));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task TenantAdmin_Can_Grant_Operations_For_Managed_Tenant()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        var managedTenantId = UlidId.NewUlid();
        var targetUserId = UlidId.NewUlid();

        await factory.SeedRoleMembershipAsync(actorId, UserRole.TenantAdmin, managedTenantId);
        await factory.SeedUserProjectionAsync(targetUserId, "target@records.test");

        using var client = factory.CreateAuthenticatedClient(actorId, "tenant.admin@records.test");

        var grantResponse = await client.PostAsJsonAsync(
            $"/api/users/{targetUserId}/roles/grant",
            new GrantUserRoleRequest(
                targetUserId.ToString(),
                UserRole.Operations,
                managedTenantId.ToString(),
                actorId.ToString(),
                DateTimeOffset.UtcNow));

        Assert.That(grantResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var membershipCount =
            await factory.CountRoleMembershipsAsync(targetUserId, UserRole.Operations, managedTenantId);
        Assert.That(membershipCount, Is.EqualTo(1));
    }

    [Test]
    public async Task TenantAdmin_Cannot_Grant_GlobalAdmin()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        var managedTenantId = UlidId.NewUlid();
        var targetUserId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(actorId, UserRole.TenantAdmin, managedTenantId);
        await factory.SeedUserProjectionAsync(targetUserId, "target-global@records.test");
        using var client = factory.CreateAuthenticatedClient(actorId, "tenant.admin@records.test");

        var response = await client.PostAsJsonAsync(
            $"/api/users/{targetUserId}/roles/grant",
            new GrantUserRoleRequest(
                targetUserId.ToString(),
                UserRole.GlobalAdmin,
                null,
                actorId.ToString(),
                DateTimeOffset.UtcNow));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GlobalAdmin_Can_Read_User_Role_Memberships()
    {
        await using var factory = new RecordsWebApplicationFactory();
        var actorId = UlidId.NewUlid();
        await factory.SeedRoleMembershipAsync(actorId, UserRole.GlobalAdmin);
        using var client = factory.CreateAuthenticatedClient(actorId, "global.admin@records.test");

        var tenantId = UlidId.NewUlid();
        var inviteResponse = await client.PostAsJsonAsync(
            "/api/users/invite",
            new InviteUserRequest(
                "roles.user@records.test",
                "Roles User",
                [new InviteUserRoleRequest(UserRole.Operations, tenantId.ToString())],
                DateTimeOffset.UtcNow));
        Assert.That(inviteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var invited = await inviteResponse.Content.ReadFromJsonAsync<UserSummaryDto>(EndpointTestSupport.JsonOptions);
        Assert.That(invited, Is.Not.Null);

        var rolesResponse = await client.GetAsync($"/api/users/{invited!.UserId}/roles");
        Assert.That(rolesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var roles =
            await rolesResponse.Content.ReadFromJsonAsync<IReadOnlyList<UserRoleMembershipDto>>(EndpointTestSupport
                .JsonOptions);
        Assert.That(roles, Is.Not.Null);
        Assert.That(roles!.Any(x => x.Role == UserRole.Operations && x.TenantId == tenantId), Is.True);
    }

    private sealed record InviteUserRequest(
        string Email,
        string? DisplayName,
        IReadOnlyCollection<InviteUserRoleRequest> Roles,
        DateTimeOffset InvitedAt
    );

    private sealed record InviteUserRoleRequest(UserRole Role, string? TenantId);

    private sealed record GrantUserRoleRequest(
        string UserId,
        UserRole Role,
        string? TenantId,
        string GrantedBy,
        DateTimeOffset GrantedAt
    );
}