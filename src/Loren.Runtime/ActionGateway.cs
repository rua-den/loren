using Loren.Core.Actions;
using Loren.Core.Audit;

namespace Loren.Runtime;

public sealed class ActionGateway : IActionGateway
{
    private readonly Dictionary<string, ActionDefinition> _definitions;
    private readonly Dictionary<string, IActionExecutor> _executors;
    private readonly IActionPolicy _policy;
    private readonly IAuditSink _audit;

    public ActionGateway(
        IEnumerable<ActionDefinition> definitions,
        IEnumerable<IActionExecutor> executors,
        IActionPolicy policy,
        IAuditSink audit)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(executors);
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));

        _definitions = definitions.ToDictionary(
            definition => definition.Name,
            StringComparer.Ordinal);
        _executors = executors.ToDictionary(
            executor => executor.ActionName,
            StringComparer.Ordinal);
    }

    public async Task<ActionResult> ExecuteAsync(
        ActionExecutionRequest execution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(execution.Request);
        cancellationToken.ThrowIfCancellationRequested();

        ActionRequest request = execution.Request;
        await AppendAuditAsync(
            execution,
            AuditEventKind.ActionRequested,
            "requested",
            null,
            cancellationToken);

        if (!_definitions.TryGetValue(request.Name, out ActionDefinition? definition))
        {
            const string reason = "Action is not registered.";
            await AppendAuditAsync(
                execution,
                AuditEventKind.PolicyEvaluated,
                "deny",
                reason,
                cancellationToken);
            await AppendAuditAsync(
                execution,
                AuditEventKind.ActionCompleted,
                "denied",
                reason,
                cancellationToken);

            return new ActionResult(
                request.Name,
                false,
                new Dictionary<string, string> { ["policy"] = "deny" },
                reason);
        }

        PolicyDecision decision;
        try
        {
            decision = await _policy.EvaluateAsync(
                definition,
                request,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AppendCancellationAuditAsync(
                execution,
                AuditEventKind.PolicyEvaluated,
                "Policy evaluation was cancelled.");
            await AppendCancellationAuditAsync(
                execution,
                AuditEventKind.ActionCompleted,
                "Action was cancelled before execution.");
            throw;
        }
        catch (Exception exception)
        {
            string reason = $"Policy evaluation failed with {exception.GetType().Name}.";
            await AppendAuditAsync(
                execution,
                AuditEventKind.PolicyEvaluated,
                "error",
                reason,
                cancellationToken);
            await AppendAuditAsync(
                execution,
                AuditEventKind.ActionCompleted,
                "failed",
                reason,
                cancellationToken);

            return new ActionResult(
                request.Name,
                false,
                new Dictionary<string, string> { ["policy"] = "error" },
                reason);
        }

        await AppendAuditAsync(
            execution,
            AuditEventKind.PolicyEvaluated,
            decision.Kind.ToString().ToLowerInvariant(),
            decision.Reason,
            cancellationToken);

        if (decision.Kind is not PolicyDecisionKind.Allow)
        {
            await AppendAuditAsync(
                execution,
                AuditEventKind.ActionCompleted,
                "denied",
                decision.Reason,
                cancellationToken);

            return new ActionResult(
                request.Name,
                false,
                new Dictionary<string, string>
                {
                    ["policy"] = decision.Kind.ToString().ToLowerInvariant(),
                },
                decision.Reason);
        }

        if (!_executors.TryGetValue(request.Name, out IActionExecutor? executor))
        {
            const string reason = "No executor is registered for the action.";
            await AppendAuditAsync(
                execution,
                AuditEventKind.ActionCompleted,
                "failed",
                reason,
                cancellationToken);

            return new ActionResult(
                request.Name,
                false,
                new Dictionary<string, string>(),
                reason);
        }

        ActionResult result;
        try
        {
            result = await executor.ExecuteAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AppendCancellationAuditAsync(
                execution,
                AuditEventKind.ActionCompleted,
                "Action execution was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            result = new ActionResult(
                request.Name,
                false,
                new Dictionary<string, string>(),
                $"Executor failed with {exception.GetType().Name}.");
        }

        await AppendAuditAsync(
            execution,
            AuditEventKind.ActionCompleted,
            result.Success ? "succeeded" : "failed",
            result.Error,
            cancellationToken);

        return result;
    }

    private Task AppendCancellationAuditAsync(
        ActionExecutionRequest execution,
        AuditEventKind kind,
        string detail) =>
        AppendAuditAsync(
            execution,
            kind,
            "cancelled",
            detail,
            CancellationToken.None);

    private Task AppendAuditAsync(
        ActionExecutionRequest execution,
        AuditEventKind kind,
        string outcome,
        string? detail,
        CancellationToken cancellationToken)
    {
        AuditEvent auditEvent = new(
            DateTimeOffset.UtcNow,
            execution.RunId,
            execution.ActionId,
            kind,
            execution.Request.Name,
            outcome,
            detail);

        return _audit.AppendAsync(auditEvent, cancellationToken);
    }
}
