namespace Loren.Core.Projects;

public sealed record Repository
{
    public Repository(
        RepositoryId id,
        ProjectId projectId,
        string name,
        RepositoryLocator locator,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Repository ID cannot be empty.", nameof(id));
        }

        if (projectId.Value == Guid.Empty)
        {
            throw new ArgumentException("Project ID cannot be empty.", nameof(projectId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(locator);

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("Repository updated time cannot precede creation time.", nameof(updatedAt));
        }

        Id = id;
        ProjectId = projectId;
        Name = name.Trim();
        Locator = locator;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public RepositoryId Id { get; }

    public ProjectId ProjectId { get; }

    public string Name { get; }

    public RepositoryLocator Locator { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }
}
