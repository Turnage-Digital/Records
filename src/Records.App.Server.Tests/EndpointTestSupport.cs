using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Records.Core.Domain.ValueObjects;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.App.Server.Tests;

internal static class EndpointTestSupport
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new UlidIdJsonConverter() }
    };

    internal static HttpClient CreateAuthenticatedClient(
        this RecordsWebApplicationFactory factory,
        UlidId userId,
        string? email = null,
        string? name = null
    )
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());

        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.Remove(TestAuthHandler.EmailHeader);
            client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            client.DefaultRequestHeaders.Remove(TestAuthHandler.NameHeader);
            client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeader, name);
        }

        return client;
    }

    internal static async Task SeedRoleMembershipAsync(
        this RecordsWebApplicationFactory factory,
        UlidId userId,
        UserRole role,
        UlidId? tenantId = null,
        UlidId? grantedBy = null
    )
    {
        var now = DateTimeOffset.UtcNow;
        var grantedById = (grantedBy ?? userId).ToString();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        db.UserRoleMemberships.Add(new UserRoleMembershipDb
        {
            UserId = userId.ToString(),
            Role = role,
            TenantId = tenantId?.ToString(),
            GrantedBy = grantedById,
            GrantedAt = now
        });

        await db.SaveChangesAsync();
    }

    internal static async Task SeedUserProjectionAsync(
        this RecordsWebApplicationFactory factory,
        UlidId userId,
        string email,
        UserStatus status = UserStatus.Active,
        string? displayName = null
    )
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        db.UserProjections.Add(new UserProjectionDb
        {
            UserId = userId.ToString(),
            Email = email,
            DisplayName = displayName,
            Status = status,
            LastUpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    internal static async Task<int> CountRoleMembershipsAsync(
        this RecordsWebApplicationFactory factory,
        UlidId userId,
        UserRole role,
        UlidId? tenantId = null
    )
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var userKey = userId.ToString();
        var tenantKey = tenantId?.ToString();
        return await db.UserRoleMemberships
            .AsNoTracking()
            .CountAsync(x =>
                x.UserId == userKey &&
                x.Role == role &&
                x.TenantId == tenantKey);
    }
}
