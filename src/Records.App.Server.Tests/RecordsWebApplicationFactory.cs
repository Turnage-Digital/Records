using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Records.Agents.Infrastructure.Sql;
using Records.Core.Infrastructure.Sql;
using Records.Notifications.Infrastructure.Sql;
using Records.Recordsets.Infrastructure.Sql;
using Records.Tenants.Infrastructure.Sql;
using Records.Users.Infrastructure.Sql;

namespace Records.App.Server.Tests;

public sealed class RecordsWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "server=localhost;port=3306;database=records_test;user=records;password=records;SslMode=None";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var databaseId = Guid.NewGuid().ToString("N");

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            ReplaceEfProvidersForTest(services, databaseId);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    private static void ReplaceEfProvidersForTest(IServiceCollection services, string databaseId)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var assemblyName = services[i].ServiceType.Assembly.GetName().Name;
            if (assemblyName == "Microsoft.EntityFrameworkCore" ||
                assemblyName == "Pomelo.EntityFrameworkCore.MySql")
            {
                services.RemoveAt(i);
            }
        }

        services.RemoveAll<DbContextOptions<CoreDbContext>>();
        services.RemoveAll<DbContextOptions<UsersDbContext>>();
        services.RemoveAll<DbContextOptions<TenantsDbContext>>();
        services.RemoveAll<DbContextOptions<AgentsDbContext>>();
        services.RemoveAll<DbContextOptions<RecordsetsDbContext>>();
        services.RemoveAll<DbContextOptions<NotificationsDbContext>>();
        services.RemoveAll<CoreDbContext>();
        services.RemoveAll<UsersDbContext>();
        services.RemoveAll<TenantsDbContext>();
        services.RemoveAll<AgentsDbContext>();
        services.RemoveAll<RecordsetsDbContext>();
        services.RemoveAll<NotificationsDbContext>();

        services.AddEntityFrameworkInMemoryDatabase();

        services.AddDbContext<CoreDbContext>(options =>
            options.UseInMemoryDatabase($"records-core-{databaseId}"));
        services.AddDbContext<UsersDbContext>(options =>
            options.UseInMemoryDatabase($"records-users-{databaseId}"));
        services.AddDbContext<TenantsDbContext>(options =>
            options.UseInMemoryDatabase($"records-tenants-{databaseId}"));
        services.AddDbContext<AgentsDbContext>(options =>
            options.UseInMemoryDatabase($"records-agents-{databaseId}"));
        services.AddDbContext<RecordsetsDbContext>(options =>
            options.UseInMemoryDatabase($"records-recordsets-{databaseId}"));
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseInMemoryDatabase($"records-notifications-{databaseId}"));
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "RecordsTestAuth";
    public const string UserIdHeader = "X-Test-UserId";
    public const string EmailHeader = "X-Test-Email";
    public const string NameHeader = "X-Test-Name";
    public const string TenantIdHeader = "X-Test-TenantId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing user id."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        var email = Request.Headers.TryGetValue(EmailHeader, out var emailValues)
            ? emailValues.FirstOrDefault()
            : null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var name = Request.Headers.TryGetValue(NameHeader, out var nameValues)
            ? nameValues.FirstOrDefault()
            : null;
        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        var tenantId = Request.Headers.TryGetValue(TenantIdHeader, out var tenantIdValues)
            ? tenantIdValues.FirstOrDefault()
            : null;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            claims.Add(new Claim("tenantId", tenantId));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
