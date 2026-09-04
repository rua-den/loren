namespace Loren.Core.Actions;

public enum ActionParameterType
{
    Text,
    WholeNumber,
    DecimalNumber,
    Flag,
}

public enum ActionAccessClass
{
    Read,
    ReversibleWrite,
    ExternalWrite,
    PrivilegedWrite,
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
        : this(
            name,
            description,
            isReadOnly ? ActionAccessClass.Read : ActionAccessClass.ExternalWrite,
            parameters)
    {
    }

    public ActionDefinition(
        string name,
        string description,
        ActionAccessClass accessClass,
        IReadOnlyList<ActionParameterDefinition>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Name = name;
        Description = description;
        AccessClass = accessClass;
        Parameters = parameters?.ToArray() ?? [];
    }

    public string Name { get; }

    public string Description { get; }

    public ActionAccessClass AccessClass { get; }

    public bool IsReadOnly => AccessClass is ActionAccessClass.Read;

    public IReadOnlyList<ActionParameterDefinition> Parameters { get; }
}
