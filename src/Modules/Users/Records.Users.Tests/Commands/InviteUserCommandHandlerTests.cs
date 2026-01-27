using Records.Core.Domain.ValueObjects;
using Records.Users.Application.Commands.InviteUser;
using Records.Users.Contracts.Projections;
using Records.Users.Domain;
using Records.Users.Domain.Entities;
using Records.Users.Domain.Interfaces;

namespace Records.Users.Tests.Commands;

public class InviteUserCommandHandlerTests
{
    [Test]
    public async Task Handle_CreatesUserAndRoleMemberships()
    {
        var unitOfWork = new FakeUsersUnitOfWork();
        var projectionWriter = new FakeProjectionWriter();
        var handler = new InviteUserCommandHandler(unitOfWork, projectionWriter);

        var roles = new[] { new InviteUserRole(UserRole.TenantAdmin, UlidId.NewUlid()) };
        var userId =
            await handler.Handle(new InviteUserCommand("user@example.com", "User", roles, DateTimeOffset.UtcNow),
                CancellationToken.None);

        Assert.That(userId, Is.Not.EqualTo(default(UlidId)));
        Assert.That(unitOfWork.AddedUser, Is.Not.Null);
        Assert.That(unitOfWork.AddedUser!.Status, Is.EqualTo(UserStatus.Invited));
        Assert.That(unitOfWork.AddedRoleMemberships.Count, Is.EqualTo(1));
        Assert.That(projectionWriter.Upserted, Is.Not.Null);
    }

    private sealed class FakeUsersUnitOfWork : IUsersUnitOfWork
    {
        public User? AddedUser { get; private set; }
        public List<(UlidId UserId, UserRole Role, UlidId? TenantId)> AddedRoleMemberships { get; } = new();

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
            AddedUser = user;
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
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeProjectionWriter : IUserProjectionWriter
    {
        public UserProjectionModel? Upserted { get; private set; }

        public Task UpsertAsync(UserProjectionModel model, CancellationToken cancellationToken)
        {
            Upserted = model;
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(UlidId userId, UserStatus status, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateProfileAsync(
            UlidId userId,
            string email,
            string? displayName,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }
    }
}