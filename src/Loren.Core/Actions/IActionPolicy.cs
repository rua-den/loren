namespace Loren.Core.Actions;

public interface IActionPolicy
{
    Task<PolicyDecision> EvaluateAsync(
        ActionDefinition definition,
        ActionRequest request,
        CancellationToken cancellationToken);
}
