using System.Collections.Frozen;

namespace Loren.Core.Actions;

public sealed record ActionRequest
{
    public ActionRequest(
        string name,
        IReadOnlyDictionary<string, string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(arguments);

        Name = name;
        Arguments = arguments.ToFrozenDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> Arguments { get; }
}
