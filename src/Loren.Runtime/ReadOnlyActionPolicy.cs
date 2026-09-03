using Loren.Core.Actions;

namespace Loren.Runtime;

public sealed class ReadOnlyActionPolicy : IActionPolicy
{
    public Task<PolicyDecision> EvaluateAsync(
        ActionDefinition definition,
        ActionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        PolicyDecision decision = definition.IsReadOnly
            ? PolicyDecision.Allow("M2 read-only action is allowed.")
            : PolicyDecision.Deny("M2 permits read-only actions only.");

        return Task.FromResult(decision);
    }
}
