using Loren.Core.Projects;
using Microsoft.EntityFrameworkCore;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.Infrastructure.CanonicalState;

public sealed class SqliteProjectCatalog : IProjectCatalog
{
    private readonly CanonicalStateDbContext _dbContext;

    public SqliteProjectCatalog(CanonicalStateDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SaveAsync(
        ProjectSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _dbContext.ChangeTracker.Clear();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            ProjectRow? projectRow = await _dbContext.Projects
                .SingleOrDefaultAsync(
                    project => project.Id == snapshot.Project.Id.Value,
                    cancellationToken);

            if (projectRow is null)
            {
                projectRow = new ProjectRow
                {
                    Id = snapshot.Project.Id.Value,
                };
                _dbContext.Projects.Add(projectRow);
            }

            projectRow.Name = snapshot.Project.Name;
            projectRow.CreatedAt = snapshot.Project.CreatedAt;
            projectRow.UpdatedAt = snapshot.Project.UpdatedAt;

            await _dbContext.ProjectAliases
                .Where(projectAlias => projectAlias.ProjectId == snapshot.Project.Id.Value)
                .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.Repositories
                .Where(repository => repository.ProjectId == snapshot.Project.Id.Value)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (string projectAlias in snapshot.Project.Aliases)
            {
                _dbContext.ProjectAliases.Add(
                    new ProjectAliasRow
                    {
                        Alias = projectAlias,
                        NormalizedAlias = ProjectAlias.Normalize(projectAlias),
                        ProjectId = snapshot.Project.Id.Value,
                    });
            }

            foreach (CanonicalRepository repository in snapshot.Repositories)
            {
                _dbContext.Repositories.Add(
                    new RepositoryRow
                    {
                        Id = repository.Id.Value,
                        ProjectId = repository.ProjectId.Value,
                        Name = repository.Name,
                        Provider = repository.Locator.Provider,
                        ExternalNamespace = repository.Locator.ExternalNamespace,
                        ExternalName = repository.Locator.ExternalName,
                        CreatedAt = repository.CreatedAt,
                        UpdatedAt = repository.UpdatedAt,
                    });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<ProjectSnapshot?> GetAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default)
    {
        if (projectId.Value == Guid.Empty)
        {
            throw new ArgumentException("Project ID cannot be empty.", nameof(projectId));
        }

        ProjectRow? projectRow = await _dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Aliases)
            .Include(project => project.Repositories)
            .SingleOrDefaultAsync(project => project.Id == projectId.Value, cancellationToken);

        return projectRow is null ? null : Map(projectRow);
    }

    public async Task<ProjectSnapshot?> FindByAliasAsync(
        string projectAlias,
        CancellationToken cancellationToken = default)
    {
        string normalizedAlias = ProjectAlias.Normalize(projectAlias);

        Guid? projectId = await _dbContext.ProjectAliases
            .AsNoTracking()
            .Where(candidate => candidate.NormalizedAlias == normalizedAlias)
            .Select(candidate => (Guid?)candidate.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);

        return projectId is null
            ? null
            : await GetAsync(new ProjectId(projectId.Value), cancellationToken);
    }

    private static ProjectSnapshot Map(ProjectRow row)
    {
        Project project = new(
            new ProjectId(row.Id),
            row.Name,
            row.Aliases.Select(projectAlias => projectAlias.Alias),
            row.CreatedAt,
            row.UpdatedAt);

        CanonicalRepository[] repositories = row.Repositories
            .OrderBy(repository => repository.Provider, StringComparer.Ordinal)
            .ThenBy(repository => repository.ExternalNamespace, StringComparer.Ordinal)
            .ThenBy(repository => repository.ExternalName, StringComparer.Ordinal)
            .Select(
                repository => new CanonicalRepository(
                    new RepositoryId(repository.Id),
                    new ProjectId(repository.ProjectId),
                    repository.Name,
                    new RepositoryLocator(
                        repository.Provider,
                        repository.ExternalNamespace,
                        repository.ExternalName),
                    repository.CreatedAt,
                    repository.UpdatedAt))
            .ToArray();

        return new ProjectSnapshot(project, repositories);
    }
}

public static class CanonicalStateDatabase
{
    public static Task MigrateAsync(
        CanonicalStateDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        return dbContext.Database.MigrateAsync(cancellationToken);
    }
}
