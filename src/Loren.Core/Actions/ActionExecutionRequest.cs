namespace Loren.Core.Actions;

public readonly record struct RunId(Guid Value)
{
    public static RunId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}

public readonly record struct ActionId(Guid Value)
{
    public static ActionId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}

public sealed record ActionExecutionRequest(
    RunId RunId,
    ActionId ActionId,
    ActionRequest Request,
    ActionAuthorizationContext? AuthorizationContext = null,
    ApprovalId? ApprovalId = null);
