namespace Loren.Core.Actions;

public enum ActionParameterType
{
    Text,
    WholeNumber,
    DecimalNumber,
    Flag,
}

public sealed record ActionParameterDefinition(
    string Name,
    string Description,
    ActionParameterType Type,
    bool IsRequired);

public sealed record ActionDefinition
{
    public ActionDefinition(
        string name,
        string description,
        bool isReadOnly,
        IReadOnlyList<ActionParameterDefinition>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Name = name;
        Description = description;
        IsReadOnly = isReadOnly;
        Parameters = parameters?.ToArray() ?? [];
    }

    public string Name { get; }

    public string Description { get; }

    public bool IsReadOnly { get; }

    public IReadOnlyList<ActionParameterDefinition> Parameters { get; }
}
