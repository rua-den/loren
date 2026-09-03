using Loren.Core.Actions;

namespace Loren.Core.Brains;

public sealed record BrainContext(IReadOnlyList<BrainInput> Inputs)
{
    public static BrainContext FromUser(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new BrainContext([new BrainMessage(BrainRole.User, content)]);
    }

    public BrainContext Append(BrainInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        List<BrainInput> inputs = [.. Inputs, input];
        return new BrainContext(inputs);
    }
}

public abstract record BrainInput;

public enum BrainRole
{
    System,
    User,
    Assistant,
}

public sealed record BrainMessage(BrainRole Role, string Content) : BrainInput;

public sealed record BrainActionObservation(
    ActionRequest Request,
    ActionResult Result) : BrainInput;
