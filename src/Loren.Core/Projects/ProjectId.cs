namespace Loren.Core.Projects;

public readonly record struct ProjectId
{
    public ProjectId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Project ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static ProjectId New() => new(Guid.NewGuid());

    public static ProjectId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString("N");
}
