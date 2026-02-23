using Records.Core.Domain.ValueObjects;
using Records.Users.Application.Commands;
using Records.Users.Contracts.Projections;
using Records.Users.Domain;

namespace Records.Users.Tests.Commands;

public class SuspendUserCommandHandlerTests
{
    [Test]
    public async Task Handle_SetsSuspendedStatus()
    {
        var userId = UlidId.NewUlid();
        var user = new User { Id = userId.ToString(), Status = UserStatus.Active };
        var unitOfWork = new FakeUsersUnitOfWork(user);
        var projectionWriter = new FakeProjectionWriter();
        var handler = new SuspendUserCommandHandler(unitOfWork, projectionWriter);

        await handler.Handle(new SuspendUserCommand(userId, "policy", DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.That(user.Status, Is.EqualTo(UserStatus.Suspended));
        Assert.That(projectionWriter.UpdatedStatus, Is.EqualTo(UserStatus.Suspended));
    }

    private sealed class FakeUsersUnitOfWork : IUsersUnitOfWork
    {
        private readonly User? user;

        public FakeUsersUnitOfWork(User? user)
        {
            this.user = user;
        }

        public Task<User?> GetUserByIdAsync(UlidId userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(user);
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
        public UserStatus? UpdatedStatus { get; private set; }

        public Task UpsertAsync(UserProjectionModel model, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(UlidId userId, UserStatus status, CancellationToken cancellationToken)
        {
            UpdatedStatus = status;
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