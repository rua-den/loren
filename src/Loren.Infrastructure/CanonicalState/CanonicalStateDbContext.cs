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
