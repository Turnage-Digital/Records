using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        ConfigureIdentityTables(modelBuilder);
        ConfigureUserRoleMemberships(modelBuilder.Entity<UserRoleMembershipDb>());
        ConfigureUserProjections(modelBuilder.Entity<UserProjectionDb>());
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("AspNetUsers");
        modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens");
    }

    private static void ConfigureUserRoleMemberships(EntityTypeBuilder<UserRoleMembershipDb> builder)
    {
        builder.ToTable("UserRoleMemberships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Role).HasConversion<int>().IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(26);
        builder.Property(x => x.GrantedBy).HasMaxLength(26).IsRequired();
        builder.Property(x => x.GrantedAt).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Role, x.TenantId }).IsUnique();
    }

    private static void ConfigureUserProjections(EntityTypeBuilder<UserProjectionDb> builder)
    {
        builder.ToTable("UserProjections");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(256);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.LastUpdatedAt).IsRequired();
    }
}
