namespace Loren.Core.Actions;

public interface IActionGateway
{
    Task<ActionResult> ExecuteAsync(
        ActionRequest request,
        CancellationToken cancellationToken);
}
