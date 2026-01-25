using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Records.Users.Domain.Entities;

namespace Records.Users.Infrastructure.Sql;

public class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : IdentityDbContext<User>(options);
