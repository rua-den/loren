namespace Loren.Core.Actions;

public interface IActionExecutor
{
    string ActionName { get; }

    Task<ActionResult> ExecuteAsync(
        ActionRequest request,
        CancellationToken cancellationToken);
}

public interface ITrustedActionExecutor : IActionExecutor
{
    Task<ActionResult> ExecuteTrustedAsync(
        ActionExecutionRequest execution,
        CancellationToken cancellationToken);
}
