using Loren.Core.Actions;

namespace Loren.Core.Brains;

public sealed record BrainTurnResult
{
    private BrainTurnResult(string? finalOutput, ActionRequest? actionRequest)
    {
        FinalOutput = finalOutput;
        ActionRequest = actionRequest;
    }

    public string? FinalOutput { get; }

    public ActionRequest? ActionRequest { get; }

    public bool IsFinal => FinalOutput is not null;

    public static BrainTurnResult Final(string output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        return new BrainTurnResult(output, null);
    }

    public static BrainTurnResult Request(ActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new BrainTurnResult(null, request);
    }
}
