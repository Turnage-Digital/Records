using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Records.Users.Domain.Entities;
using Records.Users.Infrastructure.Sql.Entities;

namespace Records.Users.Infrastructure.Sql;

public class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<UserRoleMembershipDb> UserRoleMemberships => Set<UserRoleMembershipDb>();
    public DbSet<UserProjectionDb> UserProjections => Set<UserProjectionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRoleMembershipDb>(entity =>
        {
            entity.ToTable("user_role_memberships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.Role).HasConversion<int>().IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(26);
            entity.Property(x => x.GrantedBy).HasMaxLength(26).IsRequired();
            entity.Property(x => x.GrantedAt).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Role, x.TenantId }).IsUnique();
        });

        modelBuilder.Entity<UserProjectionDb>(entity =>
        {
            entity.ToTable("user_projections");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasMaxLength(26).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.LastUpdatedAt).IsRequired();
        });
    }
}