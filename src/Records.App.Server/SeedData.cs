using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Records.App.Server.Services;
using Records.Core.Domain.ValueObjects;
using Records.Tenants.Domain;
using Records.Tenants.Infrastructure.Sql;
using Records.Tenants.Infrastructure.Sql.Entities;
using Records.Users.Domain;
using Records.Users.Domain.Entities;
using Records.Users.Infrastructure.Sql;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.App.Server;

public sealed class SeedData(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    IOptions<DevelopmentSeedOptions> seedOptions,
    ILogger<SeedData> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var options = seedOptions.Value;
        if (!options.Enabled)
        {
            logger.LogDebug("SeedData is disabled");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.GlobalAdminEmail) ||
            string.IsNullOrWhiteSpace(options.GlobalAdminPassword))
        {
            throw new InvalidOperationException(
                "DevelopmentSeed settings require GlobalAdminEmail and GlobalAdminPassword.");
        }

        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var usersDbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var tenantsDbContext = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();

        var admin = await EnsureGlobalAdminAsync(
            options,
            userManager,
            usersDbContext,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.TenantName))
        {
            await EnsureTenantAsync(options.TenantName.Trim(), tenantsDbContext, cancellationToken);
        }

        logger.LogInformation("Development seed bootstrap complete for user {UserId}", admin.Id);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<User> EnsureGlobalAdminAsync(
        DevelopmentSeedOptions options,
        UserManager<User> userManager,
        UsersDbContext usersDbContext,
        CancellationToken cancellationToken
    )
    {
        var email = options.GlobalAdminEmail.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new User
            {
                Id = UlidId.NewUlid().ToString(),
                Email = email,
                NormalizedEmail = normalizedEmail,
                UserName = email,
                NormalizedUserName = normalizedEmail,
                DisplayName = options.GlobalAdminDisplayName?.Trim(),
                EmailConfirmed = true,
                Status = UserStatus.Active
            };

            var createResult = await userManager.CreateAsync(admin, options.GlobalAdminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(x => $"{x.Code}: {x.Description}"));
                throw new InvalidOperationException($"Failed creating seed global admin user: {errors}");
            }

            logger.LogInformation("Created seed global admin user {UserId}", admin.Id);
        }

        if (!UlidId.TryParse(admin.Id, out _))
        {
            throw new InvalidOperationException(
                $"Seed global admin user id '{admin.Id}' is not a ULID. " +
                "Remove the user and rerun the seed bootstrap.");
        }

        var roleExists = await usersDbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == admin.Id &&
                     x.Role == UserRole.GlobalAdmin &&
                     x.TenantId == null,
                cancellationToken);
        if (!roleExists)
        {
            usersDbContext.UserRoleMemberships.Add(new UserRoleMembershipDb
            {
                UserId = admin.Id,
                Role = UserRole.GlobalAdmin,
                TenantId = null,
                GrantedBy = admin.Id,
                GrantedAt = DateTimeOffset.UtcNow
            });
            await usersDbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Granted GlobalAdmin role to seed user {UserId}", admin.Id);
        }

        var projection = await usersDbContext.UserProjections
            .SingleOrDefaultAsync(x => x.UserId == admin.Id, cancellationToken);
        if (projection is null)
        {
            usersDbContext.UserProjections.Add(new UserProjectionDb
            {
                UserId = admin.Id,
                Email = admin.Email ?? email,
                DisplayName = admin.DisplayName,
                Status = admin.Status,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            projection.Email = admin.Email ?? email;
            projection.DisplayName = admin.DisplayName;
            projection.Status = admin.Status;
            projection.LastUpdatedAt = DateTimeOffset.UtcNow;
        }

        await usersDbContext.SaveChangesAsync(cancellationToken);
        return admin;
    }

    private async Task EnsureTenantAsync(
        string tenantName,
        TenantsDbContext tenantsDbContext,
        CancellationToken cancellationToken
    )
    {
        var existingTenant = await tenantsDbContext.Tenants
            .SingleOrDefaultAsync(x => x.Name == tenantName, cancellationToken);

        if (existingTenant is null)
        {
            existingTenant = new TenantDb
            {
                Id = UlidId.NewUlid().ToString(),
                Name = tenantName,
                Status = TenantStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };
            tenantsDbContext.Tenants.Add(existingTenant);
            await tenantsDbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created seed tenant {TenantId}", existingTenant.Id);
        }

        var projection = await tenantsDbContext.TenantProjections
            .SingleOrDefaultAsync(x => x.TenantId == existingTenant.Id, cancellationToken);
        if (projection is null)
        {
            tenantsDbContext.TenantProjections.Add(new TenantProjectionDb
            {
                TenantId = existingTenant.Id,
                Name = existingTenant.Name,
                Status = existingTenant.Status,
                CreatedAt = existingTenant.CreatedAt
            });
        }
        else
        {
            projection.Name = existingTenant.Name;
            projection.Status = existingTenant.Status;
        }

        await tenantsDbContext.SaveChangesAsync(cancellationToken);
    }
}
