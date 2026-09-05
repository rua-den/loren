using Loren.Core.Actions;

namespace Loren.Runtime;

public sealed class ReadOnlyActionPolicy : IActionPolicy
{
    public Task<PolicyDecision> EvaluateAsync(
        ActionDefinition definition,
        ActionExecutionRequest execution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(execution);
        cancellationToken.ThrowIfCancellationRequested();

        PolicyDecision decision = definition.IsReadOnly
            ? PolicyDecision.Allow("Read-only action is allowed.")
            : PolicyDecision.Deny("Read-only policy permits read actions only.");

        return Task.FromResult(decision);
    }
}
