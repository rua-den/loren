namespace Loren.Core.Projects;

public sealed record ProjectSnapshot
{
    public ProjectSnapshot(Project project, IEnumerable<Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(repositories);

        Repository[] repositoryArray = repositories.ToArray();
        if (repositoryArray.Any(repository => repository.ProjectId != project.Id))
        {
            throw new ArgumentException(
                "Every repository in a project snapshot must belong to the same project.",
                nameof(repositories));
        }

        Project = project;
        Repositories = repositoryArray;
    }

    public Project Project { get; }

    public IReadOnlyList<Repository> Repositories { get; }
}
