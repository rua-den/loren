using Loren.Core.Actions;

namespace Loren.Runtime;

public sealed class GateDActionPolicy(IWriteSafetyState writeSafetyState) : IActionPolicy
{
    private readonly IWriteSafetyState _writeSafetyState =
        writeSafetyState ?? throw new ArgumentNullException(nameof(writeSafetyState));

    public Task<PolicyDecision> EvaluateAsync(
        ActionDefinition definition,
        ActionExecutionRequest execution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(execution);
        cancellationToken.ThrowIfCancellationRequested();

        PolicyDecision decision = definition.AccessClass switch
        {
            ActionAccessClass.Read => PolicyDecision.Allow("Read action is allowed."),
            ActionAccessClass.PrivilegedWrite => PolicyDecision.Deny(
                "Privileged writes are outside the v0.1 write surface."),
            _ when execution.AuthorizationContext is null => PolicyDecision.Deny(
                "Write action is missing trusted canonical authorization context."),
            _ when _writeSafetyState.IsReadOnly => PolicyDecision.Deny(
                "Global read-only mode blocks external writes."),
            _ => PolicyDecision.RequireApproval(
                "External write requires exact one-time owner approval."),
        };

        return Task.FromResult(decision);
    }
}

public sealed class FixedWriteSafetyState(bool isReadOnly) : IWriteSafetyState
{
    public bool IsReadOnly { get; } = isReadOnly;
}
