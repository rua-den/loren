namespace Loren.Core.Actions;

public interface IActionPolicy
{
    Task<PolicyDecision> EvaluateAsync(
        ActionDefinition definition,
        ActionExecutionRequest execution,
        CancellationToken cancellationToken);
}
