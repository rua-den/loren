namespace Loren.Core.Actions;

public interface IActionExecutor
{
    string ActionName { get; }

    Task<ActionResult> ExecuteAsync(
        ActionRequest request,
        CancellationToken cancellationToken);
}
