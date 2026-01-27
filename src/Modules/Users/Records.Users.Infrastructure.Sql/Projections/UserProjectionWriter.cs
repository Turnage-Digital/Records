using Microsoft.EntityFrameworkCore;
using Records.Core.Domain.ValueObjects;
using Records.Users.Contracts.Projections;
using Records.Users.Domain;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql.Projections;

public sealed class UserProjectionWriter(UsersDbContext dbContext) : IUserProjectionWriter
{
    public async Task UpsertAsync(UserProjectionModel model, CancellationToken cancellationToken)
    {
        var userKey = model.UserId.ToString();
        var record = await dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.UserId == userKey, cancellationToken);

        if (record is null)
        {
            record = new UserProjectionDb
            {
                UserId = userKey,
                Email = model.Email,
                DisplayName = model.DisplayName,
                LastUpdatedAt = model.LastUpdatedAt,
                Status = model.Status
            };
            dbContext.UserProjections.Add(record);
        }
        else
        {
            record.Email = model.Email;
            record.DisplayName = model.DisplayName;
            record.LastUpdatedAt = model.LastUpdatedAt;
            record.Status = model.Status;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(UlidId userId, UserStatus status, CancellationToken cancellationToken)
    {
        var userKey = userId.ToString();
        var record = await dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.UserId == userKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Status = status;
        record.LastUpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateProfileAsync(
        UlidId userId,
        string email,
        string? displayName,
        CancellationToken cancellationToken
    )
    {
        var userKey = userId.ToString();
        var record = await dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.UserId == userKey, cancellationToken);

        if (record is null)
        {
            return;
        }

        record.Email = email;
        record.DisplayName = displayName;
        record.LastUpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}