namespace Loren.Core.Actions;

public enum PolicyDecisionKind
{
    Allow,
    Deny,
    RequireApproval,
}

public sealed record PolicyDecision(
    PolicyDecisionKind Kind,
    string Reason)
{
    public static PolicyDecision Allow(string reason) =>
        new(PolicyDecisionKind.Allow, reason);

    public static PolicyDecision Deny(string reason) =>
        new(PolicyDecisionKind.Deny, reason);

    public static PolicyDecision RequireApproval(string reason) =>
        new(PolicyDecisionKind.RequireApproval, reason);
}
