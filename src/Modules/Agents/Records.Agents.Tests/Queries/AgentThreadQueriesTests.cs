using Microsoft.EntityFrameworkCore;
using Records.Agents.Infrastructure.Sql;
using Records.Core.Contracts;
using Records.Core.Domain.ValueObjects;

namespace Records.Agents.Tests.Queries;

public sealed class AgentThreadQueriesTests
{
    [Test]
    public void CreateCurrentUserThreadQuery_ShouldTranslate_ForTenantScopedThreads()
    {
        var currentUserId = UlidId.NewUlid();
        var tenantId = UlidId.NewUlid().ToString();

        using var dbContext = CreateDbContext();
        var queries = new AgentThreadQueries(
            dbContext,
            new FakeCurrentUserAccess(currentUserId),
            new FakeTenantContext(tenantId));

        var sql = queries.CreateCurrentUserThreadQuery(currentUserId.ToString()).ToQueryString();

        Assert.That(sql, Does.Contain("`UserId`"));
        Assert.That(sql, Does.Contain("`TenantId`"));
        Assert.That(sql, Does.Contain("WHERE"));
        Assert.That(sql, Does.Contain("AND"));
    }

    [Test]
    public void CreateCurrentUserThreadQuery_ShouldTranslate_ForGlobalThreadsWithBlankTenantFallback()
    {
        var currentUserId = UlidId.NewUlid();

        using var dbContext = CreateDbContext();
        var queries = new AgentThreadQueries(
            dbContext,
            new FakeCurrentUserAccess(currentUserId),
            new FakeTenantContext(null));

        var sql = queries.CreateCurrentUserThreadQuery(currentUserId.ToString()).ToQueryString();

        Assert.That(sql, Does.Contain("`UserId`"));
        Assert.That(sql, Does.Contain("`TenantId` IS NULL"));
        Assert.That(sql, Does.Contain("trim(`a`.`TenantId`) = ''").IgnoreCase);
    }

    private static AgentsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseMySql(
                "Server=127.0.0.1;Port=3306;Database=records_tests;User=records;Password=records;SslMode=None",
                new MySqlServerVersion(new Version(8, 0, 34)))
            .Options;

        return new AgentsDbContext(options);
    }

    private sealed class FakeCurrentUserAccess(UlidId userId) : ICurrentUserAccess
    {
        public bool TryGetCurrentUserId(out UlidId currentUserId)
        {
            currentUserId = userId;
            return true;
        }

        public UlidId GetCurrentUserIdOrThrow()
        {
            return userId;
        }

        public Task<bool> IsGlobalAdminAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CanAccessOpsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> CanManageTenantAsync(UlidId tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CanOperateTenantAsync(UlidId tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class FakeTenantContext(string? tenantId) : ITenantContext
    {
        public string? TenantId => tenantId;
        public string? ActorId => null;
    }
}
