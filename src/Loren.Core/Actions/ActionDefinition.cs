namespace Loren.Core.Actions;

public sealed record ActionDefinition(
    string Name,
    string Description,
    bool IsReadOnly);
