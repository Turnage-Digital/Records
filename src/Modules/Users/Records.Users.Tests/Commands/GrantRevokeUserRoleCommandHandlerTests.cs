using Records.Core.Domain.ValueObjects;
using Records.Users.Application.Commands.GrantUserRole;
using Records.Users.Application.Commands.RevokeUserRole;
using Records.Users.Contracts;
using Records.Users.Domain;
using Records.Users.Domain.Entities;
using Records.Users.Domain.Interfaces;

namespace Records.Users.Tests.Commands;

public class GrantRevokeUserRoleCommandHandlerTests
{
    [Test]
    public async Task Handle_GrantRole_AddsMembership()
    {
        var unitOfWork = new FakeUsersUnitOfWork();
        var accessQueries = new FakeAccessQueries(true);
        var handler = new GrantUserRoleCommandHandler(unitOfWork, accessQueries);

        var adminId = UlidId.NewUlid();
        await handler.Handle(
            new GrantUserRoleCommand(UlidId.NewUlid(), UserRole.Operations, UlidId.NewUlid(), adminId,
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.That(unitOfWork.AddedRoleMemberships.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_RevokeRole_RemovesMembership()
    {
        var unitOfWork = new FakeUsersUnitOfWork();
        var accessQueries = new FakeAccessQueries(true);
        var handler = new RevokeUserRoleCommandHandler(unitOfWork, accessQueries);

        var adminId = UlidId.NewUlid();
        await handler.Handle(new RevokeUserRoleCommand(UlidId.NewUlid(), UserRole.Operations, UlidId.NewUlid(),
                adminId),
            CancellationToken.None);

        Assert.That(unitOfWork.RemovedRoleMemberships.Count, Is.EqualTo(1));
    }

    private sealed class FakeUsersUnitOfWork : IUsersUnitOfWork
    {
        public List<(UlidId UserId, UserRole Role, UlidId? TenantId)> AddedRoleMemberships { get; } = new();
        public List<(UlidId UserId, UserRole Role, UlidId? TenantId)> RemovedRoleMemberships { get; } = new();

        public Task<User?> GetUserByIdAsync(UlidId userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public Task AddUserAsync(User user, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddRoleMembershipAsync(
            UlidId userId,
            UserRole role,
            UlidId? tenantId,
            UlidId grantedBy,
            DateTimeOffset grantedAt,
            CancellationToken cancellationToken
        )
        {
            AddedRoleMemberships.Add((userId, role, tenantId));
            return Task.CompletedTask;
        }

        public Task RemoveRoleMembershipAsync(
            UlidId userId,
            UserRole role,
            UlidId? tenantId,
            CancellationToken cancellationToken
        )
        {
            RemovedRoleMemberships.Add((userId, role, tenantId));
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeAccessQueries(bool isGlobalAdmin) : IUserAccessQueries
    {
        public Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(isGlobalAdmin);
        }

        public Task<bool> IsTenantAdminAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsTenantAdminAsync(UlidId userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsOperationsAsync(UlidId userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsOperationsAsync(UlidId userId, UlidId tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}
