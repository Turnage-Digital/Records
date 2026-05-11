using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Records.Agents.Infrastructure.Sql.Entities;

namespace Records.Agents.Infrastructure.Sql;

public sealed class AgentsDbContext(DbContextOptions<AgentsDbContext> options)
    : DbContext(options)
{
    public DbSet<AgentThreadDb> AgentThreads => Set<AgentThreadDb>();
    public DbSet<AgentThreadProjectionDb> AgentThreadProjections => Set<AgentThreadProjectionDb>();
    public DbSet<AgentTurnDb> AgentTurns => Set<AgentTurnDb>();
    public DbSet<AgentToolCallDb> AgentToolCalls => Set<AgentToolCallDb>();
    public DbSet<AgentArtifactDb> AgentArtifacts => Set<AgentArtifactDb>();
    public DbSet<AgentProposalDb> AgentProposals => Set<AgentProposalDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureAgentThreads(modelBuilder.Entity<AgentThreadDb>());
        ConfigureAgentThreadProjections(modelBuilder.Entity<AgentThreadProjectionDb>());
        ConfigureAgentTurns(modelBuilder.Entity<AgentTurnDb>());
        ConfigureAgentToolCalls(modelBuilder.Entity<AgentToolCallDb>());
        ConfigureAgentArtifacts(modelBuilder.Entity<AgentArtifactDb>());
        ConfigureAgentProposals(modelBuilder.Entity<AgentProposalDb>());
    }

    private static void ConfigureAgentThreads(EntityTypeBuilder<AgentThreadDb> builder)
    {
        builder.ToTable("AgentThreads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26);
        builder.Property(x => x.UserId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(64);
        builder.Property(x => x.BackendId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.TenantId, x.UpdatedAt });
    }

    private static void ConfigureAgentThreadProjections(EntityTypeBuilder<AgentThreadProjectionDb> builder)
    {
        builder.ToTable("AgentThreadProjections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26);
        builder.Property(x => x.UserId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(64);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.TenantId, x.UpdatedAt });
    }

    private static void ConfigureAgentTurns(EntityTypeBuilder<AgentTurnDb> builder)
    {
        builder.ToTable("AgentTurns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26);
        builder.Property(x => x.ThreadId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Content).HasColumnType("longtext").IsRequired();
        builder.Property(x => x.PastedText).HasColumnType("longtext");
        builder.HasIndex(x => new { x.ThreadId, x.CreatedAt });
    }

    private static void ConfigureAgentToolCalls(EntityTypeBuilder<AgentToolCallDb> builder)
    {
        builder.ToTable("AgentToolCalls");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.ThreadId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.TurnId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ArgumentsJson).HasColumnType("longtext").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Summary).HasColumnType("longtext");
        builder.Property(x => x.Error).HasColumnType("longtext");
        builder.HasIndex(x => new { x.ThreadId, x.TurnId, x.StartedAt });
    }

    private static void ConfigureAgentArtifacts(EntityTypeBuilder<AgentArtifactDb> builder)
    {
        builder.ToTable("AgentArtifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26);
        builder.Property(x => x.ThreadId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Kind).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("longtext").IsRequired();
        builder.HasIndex(x => new { x.ThreadId, x.IsCurrent });
    }

    private static void ConfigureAgentProposals(EntityTypeBuilder<AgentProposalDb> builder)
    {
        builder.ToTable("AgentProposals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(26);
        builder.Property(x => x.ThreadId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("longtext").IsRequired();
        builder.HasIndex(x => new { x.ThreadId, x.IsCurrent });
    }
}
