using Microsoft.EntityFrameworkCore;
using Records.Agents.Infrastructure.Sql.Entities;

namespace Records.Agents.Infrastructure.Sql;

public sealed class AgentsDbContext(DbContextOptions<AgentsDbContext> options) : DbContext(options)
{
    public DbSet<AgentThreadDb> AgentThreads => Set<AgentThreadDb>();
    public DbSet<AgentThreadProjectionDb> AgentThreadProjections => Set<AgentThreadProjectionDb>();
    public DbSet<AgentTurnDb> AgentTurns => Set<AgentTurnDb>();
    public DbSet<AgentArtifactDb> AgentArtifacts => Set<AgentArtifactDb>();
    public DbSet<AgentProposalDb> AgentProposals => Set<AgentProposalDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var threads = modelBuilder.Entity<AgentThreadDb>();
        threads.ToTable("AgentThreads");
        threads.HasKey(x => x.Id);
        threads.Property(x => x.Id).HasMaxLength(26);
        threads.Property(x => x.UserId).HasMaxLength(26).IsRequired();
        threads.Property(x => x.TenantId).HasMaxLength(64);
        threads.Property(x => x.BackendId).HasMaxLength(128).IsRequired();
        threads.Property(x => x.Title).HasMaxLength(256).IsRequired();
        threads.HasIndex(x => new { x.UserId, x.TenantId, x.UpdatedAt });

        var threadProjections = modelBuilder.Entity<AgentThreadProjectionDb>();
        threadProjections.ToTable("AgentThreadProjections");
        threadProjections.HasKey(x => x.Id);
        threadProjections.Property(x => x.Id).HasMaxLength(26);
        threadProjections.Property(x => x.UserId).HasMaxLength(26).IsRequired();
        threadProjections.Property(x => x.TenantId).HasMaxLength(64);
        threadProjections.Property(x => x.Title).HasMaxLength(256).IsRequired();
        threadProjections.HasIndex(x => new { x.UserId, x.TenantId, x.UpdatedAt });

        var turns = modelBuilder.Entity<AgentTurnDb>();
        turns.ToTable("AgentTurns");
        turns.HasKey(x => x.Id);
        turns.Property(x => x.Id).HasMaxLength(26);
        turns.Property(x => x.ThreadId).HasMaxLength(26).IsRequired();
        turns.Property(x => x.Role).HasMaxLength(32).IsRequired();
        turns.Property(x => x.Content).HasColumnType("longtext").IsRequired();
        turns.Property(x => x.PastedText).HasColumnType("longtext");
        turns.HasIndex(x => new { x.ThreadId, x.CreatedAt });

        var artifacts = modelBuilder.Entity<AgentArtifactDb>();
        artifacts.ToTable("AgentArtifacts");
        artifacts.HasKey(x => x.Id);
        artifacts.Property(x => x.Id).HasMaxLength(26);
        artifacts.Property(x => x.ThreadId).HasMaxLength(26).IsRequired();
        artifacts.Property(x => x.Kind).HasMaxLength(64).IsRequired();
        artifacts.Property(x => x.PayloadJson).HasColumnType("longtext").IsRequired();
        artifacts.HasIndex(x => new { x.ThreadId, x.IsCurrent });

        var proposals = modelBuilder.Entity<AgentProposalDb>();
        proposals.ToTable("AgentProposals");
        proposals.HasKey(x => x.Id);
        proposals.Property(x => x.Id).HasMaxLength(26);
        proposals.Property(x => x.ThreadId).HasMaxLength(26).IsRequired();
        proposals.Property(x => x.PayloadJson).HasColumnType("longtext").IsRequired();
        proposals.HasIndex(x => new { x.ThreadId, x.IsCurrent });
    }
}
