using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.CanonicalState;

public sealed class CanonicalStateDbContext : DbContext
{
    public CanonicalStateDbContext(DbContextOptions<CanonicalStateDbContext> options)
        : base(options)
    {
    }

    internal DbSet<ProjectRow> Projects => Set<ProjectRow>();

    internal DbSet<ProjectAliasRow> ProjectAliases => Set<ProjectAliasRow>();

    internal DbSet<RepositoryRow> Repositories => Set<RepositoryRow>();

    internal DbSet<MemoryRecordRow> MemoryRecords => Set<MemoryRecordRow>();

    internal DbSet<ActionApprovalRow> ActionApprovals => Set<ActionApprovalRow>();

    internal DbSet<OrganizationItemRow> OrganizationItems => Set<OrganizationItemRow>();

    internal DbSet<CreateBranchProposalRow> CreateBranchProposals => Set<CreateBranchProposalRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CanonicalStateModel.Configure(modelBuilder);
    }
}

internal static class CanonicalStateModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<ProjectRow>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(200).IsRequired();
            entity.Property(project => project.CreatedAt).IsRequired();
            entity.Property(project => project.UpdatedAt).IsRequired();

            entity
                .HasMany(project => project.Aliases)
                .WithOne(alias => alias.Project)
                .HasForeignKey(alias => alias.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(project => project.Repositories)
                .WithOne(repository => repository.Project)
                .HasForeignKey(repository => repository.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectAliasRow>(entity =>
        {
            entity.ToTable("ProjectAliases");
            entity.HasKey(alias => alias.NormalizedAlias);
            entity.Property(alias => alias.NormalizedAlias).HasMaxLength(200);
            entity.Property(alias => alias.Alias).HasMaxLength(200).IsRequired();
            entity.HasIndex(alias => alias.ProjectId);
        });

        modelBuilder.Entity<RepositoryRow>(entity =>
        {
            entity.ToTable("Repositories");
            entity.HasKey(repository => repository.Id);
            entity.Property(repository => repository.Name).HasMaxLength(200).IsRequired();
            entity.Property(repository => repository.Provider).HasMaxLength(100).IsRequired();
            entity.Property(repository => repository.ExternalNamespace).HasMaxLength(200).IsRequired();
            entity.Property(repository => repository.ExternalName).HasMaxLength(200).IsRequired();
            entity.Property(repository => repository.CreatedAt).IsRequired();
            entity.Property(repository => repository.UpdatedAt).IsRequired();
            entity.HasIndex(repository => repository.ProjectId);
            entity
                .HasIndex(repository => new
                {
                    repository.Provider,
                    repository.ExternalNamespace,
                    repository.ExternalName,
                })
                .IsUnique();
        });

        modelBuilder.Entity<MemoryRecordRow>(entity =>
        {
            entity.ToTable("MemoryRecords");
            entity.HasKey(memory => memory.Id);
            entity.Property(memory => memory.SourceClass).HasMaxLength(64).IsRequired();
            entity.Property(memory => memory.Content).IsRequired();
            entity.Property(memory => memory.SourceReference).HasMaxLength(1000);
            entity.Property(memory => memory.CreatedAt).IsRequired();
            entity.Property(memory => memory.UpdatedAt).IsRequired();
            entity.HasIndex(memory => new { memory.ProjectId, memory.SupersededById });
            entity.HasIndex(memory => memory.RepositoryId);
            entity.HasIndex(memory => memory.SupersededById);

            entity
                .HasOne<ProjectRow>()
                .WithMany()
                .HasForeignKey(memory => memory.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<RepositoryRow>()
                .WithMany()
                .HasForeignKey(memory => memory.RepositoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<MemoryRecordRow>()
                .WithMany()
                .HasForeignKey(memory => memory.SupersededById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActionApprovalRow>(entity =>
        {
            entity.ToTable("ActionApprovals");
            entity.HasKey(approval => approval.Id);
            entity.Property(approval => approval.OwnerPrincipalReference).HasMaxLength(256).IsRequired();
            entity.Property(approval => approval.ActionName).HasMaxLength(200).IsRequired();
            entity.Property(approval => approval.IntentFingerprint).HasMaxLength(128).IsRequired();
            entity.Property(approval => approval.ApprovedAtUnixMs).IsRequired();
            entity.Property(approval => approval.ExpiresAtUnixMs).IsRequired();
            entity.Property(approval => approval.ConsumedAtUnixMs).IsRequired(false);
            entity.Property(approval => approval.RevokedAtUnixMs).IsRequired(false);
            entity.HasIndex(approval => approval.ProjectId);
            entity.HasIndex(approval => approval.RepositoryId);
            entity.HasIndex(approval => new { approval.ProjectId, approval.RepositoryId, approval.ConsumedAtUnixMs });

            entity
                .HasOne<ProjectRow>()
                .WithMany()
                .HasForeignKey(approval => approval.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<RepositoryRow>()
                .WithMany()
                .HasForeignKey(approval => approval.RepositoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationItemRow>(entity =>
        {
            entity.ToTable("OrganizationItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Kind).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(500);
            entity.Property(item => item.Content).IsRequired();
            entity.Property(item => item.TaskStatus).HasMaxLength(32);
            entity.Property(item => item.SourceReference).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.CreatedAtUnixMs).IsRequired();
            entity.Property(item => item.UpdatedAtUnixMs).IsRequired();
            entity.Property(item => item.CompletedAtUnixMs).IsRequired(false);
            entity.HasIndex(item => item.ProjectId);
            entity.HasIndex(item => item.Kind);
            entity.HasIndex(item => item.TaskStatus);
            entity.HasIndex(item => item.UpdatedAtUnixMs);
            entity.HasIndex(item => new { item.ProjectId, item.Kind, item.TaskStatus });

            entity
                .HasOne<ProjectRow>()
                .WithMany()
                .HasForeignKey(item => item.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CreateBranchProposalRow>(entity =>
        {
            entity.ToTable("CreateBranchProposals");
            entity.HasKey(proposal => proposal.Id);
            entity.Property(proposal => proposal.OwnerPrincipalReference).HasMaxLength(256).IsRequired();
            entity.Property(proposal => proposal.RepositoryProvider).HasMaxLength(100).IsRequired();
            entity.Property(proposal => proposal.RepositoryNamespace).HasMaxLength(200).IsRequired();
            entity.Property(proposal => proposal.RepositoryName).HasMaxLength(200).IsRequired();
            entity.Property(proposal => proposal.Branch).HasMaxLength(255).IsRequired();
            entity.Property(proposal => proposal.SourceRef).HasMaxLength(255).IsRequired();
            entity.Property(proposal => proposal.SourceSha).HasMaxLength(40).IsRequired();
            entity.Property(proposal => proposal.IntentFingerprint).HasMaxLength(128).IsRequired();
            entity.Property(proposal => proposal.Status).HasMaxLength(32).IsRequired();
            entity.Property(proposal => proposal.CreatedAtUnixMs).IsRequired();
            entity.Property(proposal => proposal.ExpiresAtUnixMs).IsRequired();
            entity.Property(proposal => proposal.DecidedAtUnixMs).IsRequired(false);
            entity.HasIndex(proposal => new { proposal.OwnerPrincipalReference, proposal.Status, proposal.ExpiresAtUnixMs });
            entity.HasIndex(proposal => new { proposal.ProjectId, proposal.RepositoryId });
            entity.HasIndex(proposal => proposal.RepositoryId);
            entity.HasOne<ProjectRow>().WithMany().HasForeignKey(proposal => proposal.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RepositoryRow>().WithMany().HasForeignKey(proposal => proposal.RepositoryId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

internal sealed class ProjectRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<ProjectAliasRow> Aliases { get; } = [];

    public List<RepositoryRow> Repositories { get; } = [];
}

internal sealed class ProjectAliasRow
{
    public string NormalizedAlias { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }

    public ProjectRow? Project { get; set; }
}

internal sealed class RepositoryRow
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string ExternalNamespace { get; set; } = string.Empty;

    public string ExternalName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ProjectRow? Project { get; set; }
}

internal sealed class MemoryRecordRow
{
    public Guid Id { get; set; }

    public string SourceClass { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }

    public Guid? RepositoryId { get; set; }

    public string? SourceReference { get; set; }

    public Guid? SupersededById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class ActionApprovalRow
{
    public Guid Id { get; set; }

    public string OwnerPrincipalReference { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }

    public Guid RepositoryId { get; set; }

    public string IntentFingerprint { get; set; } = string.Empty;

    public long ApprovedAtUnixMs { get; set; }

    public long ExpiresAtUnixMs { get; set; }

    public long? ConsumedAtUnixMs { get; set; }

    public long? RevokedAtUnixMs { get; set; }
}

internal sealed class OrganizationItemRow
{
    public Guid Id { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }

    public string? TaskStatus { get; set; }

    public string SourceReference { get; set; } = string.Empty;

    public long CreatedAtUnixMs { get; set; }

    public long UpdatedAtUnixMs { get; set; }

    public long? CompletedAtUnixMs { get; set; }
}

internal sealed class CreateBranchProposalRow
{
    public Guid Id { get; set; }
    public string OwnerPrincipalReference { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid RepositoryId { get; set; }
    public string RepositoryProvider { get; set; } = string.Empty;
    public string RepositoryNamespace { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string SourceRef { get; set; } = string.Empty;
    public string SourceSha { get; set; } = string.Empty;
    public string IntentFingerprint { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long CreatedAtUnixMs { get; set; }
    public long ExpiresAtUnixMs { get; set; }
    public long? DecidedAtUnixMs { get; set; }
}
