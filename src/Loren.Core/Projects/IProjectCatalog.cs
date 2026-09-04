namespace Loren.Core.Projects;

public interface IProjectCatalog
{
    Task SaveAsync(
        ProjectSnapshot snapshot,
        CancellationToken cancellationToken = default);

    Task<ProjectSnapshot?> GetAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default);

    Task<ProjectSnapshot?> FindByAliasAsync(
        string projectAlias,
        CancellationToken cancellationToken = default);
}
