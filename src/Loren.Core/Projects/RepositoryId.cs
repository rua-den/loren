namespace Loren.Core.Projects;

public readonly record struct RepositoryId
{
    public RepositoryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Repository ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static RepositoryId New() => new(Guid.NewGuid());

    public static RepositoryId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString("N");
}
