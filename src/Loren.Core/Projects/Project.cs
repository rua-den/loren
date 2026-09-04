namespace Loren.Core.Projects;

public sealed record Project
{
    public Project(
        ProjectId id,
        string name,
        IEnumerable<string> aliases,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Project ID cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(aliases);

        string[] normalizedAliases = aliases
            .Select(ProjectAlias.Normalize)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (normalizedAliases.Length == 0)
        {
            throw new ArgumentException("Project must have at least one alias.", nameof(aliases));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("Project updated time cannot precede creation time.", nameof(updatedAt));
        }

        Id = id;
        Name = name.Trim();
        Aliases = normalizedAliases;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public ProjectId Id { get; }

    public string Name { get; }

    public IReadOnlyList<string> Aliases { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }
}
