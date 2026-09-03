namespace Loren.Core.Actions;

public sealed record ActionResult(
    string ActionName,
    bool Success,
    IReadOnlyDictionary<string, string> Data,
    string? Error = null);
