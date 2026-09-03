namespace Loren.Core.Actions;

public sealed record ActionRequest(
    string Name,
    IReadOnlyDictionary<string, string> Arguments);
