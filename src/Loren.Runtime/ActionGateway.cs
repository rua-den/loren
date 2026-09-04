using Loren.Core.Actions;
using Loren.Core.Audit;

namespace Loren.Runtime;

public sealed class ActionGateway : IActionGateway
{
    private readonly Dictionary<string, ActionDefinition> _definitions;
    private readonly Dictionary<string, IActionExecutor> _executors;
    private readonly IActionPolicy _policy;
    private readonly IAuditSink _audit;
    private readonly IActionApprovalStore? _approvalStore;
    private readonly TimeProvider _timeProvider;

    public ActionGateway(
        IEnumerable<ActionDefinition> definitions,
        IEnumerable<IActionExecutor> executors,
        IActionPolicy policy,
        IAuditSink audit)
        : this(
            definitions,
            executors,
            policy,
            audit,
            null,
            TimeProvider.System)
    {
    }

    public ActionGateway(
        IEnumerable<ActionDefinition> definitions,
        IEnumerable<IActionExecutor> executors,
        IActionPolicy policy,
        IAuditSink audit,
        IActionApprovalStore approvalStore,
        TimeProvider? timeProvider = null)
        : this(
            definitions,
            executors,
            policy,
            audit,
            approvalStore ?? throw new ArgumentNullException(nameof(approvalStore)),
            timeProvider ?? TimeProvider.System)
    {
    }

    private ActionGateway(
        IEnumerable<ActionDefinition> definitions,
        IEnumerable<IActionExecutor> executors,
        IActionPolicy policy,
        IAuditSink audit,
        IActionApprovalStore? approvalStore,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(executors);
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _approvalStore = approvalStore;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

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
                execution,
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

        if (decision.Kind is PolicyDecisionKind.Deny)
        {
            return await DenyAsync(
                execution,
                decision.Reason,
                new Dictionary<string, string> { ["policy"] = "deny" },
                cancellationToken);
        }

        bool requiresApproval =
            definition.AccessClass is not ActionAccessClass.Read
            || decision.Kind is PolicyDecisionKind.RequireApproval;

        if (requiresApproval)
        {
            ActionResult? approvalFailure = await ValidateAndConsumeApprovalAsync(
                definition,
                execution,
                decision,
                cancellationToken);

            if (approvalFailure is not null)
            {
                return approvalFailure;
            }
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

    private async Task<ActionResult?> ValidateAndConsumeApprovalAsync(
        ActionDefinition definition,
        ActionExecutionRequest execution,
        PolicyDecision decision,
        CancellationToken cancellationToken)
    {
        if (_approvalStore is null)
        {
            const string reason = "Write approval store is not configured.";
            await AppendAuditAsync(
                execution,
                AuditEventKind.ApprovalEvaluated,
                "missing",
                reason,
                cancellationToken);
            return await DenyAsync(
                execution,
                reason,
                new Dictionary<string, string>
                {
                    ["policy"] = decision.Kind.ToString().ToLowerInvariant(),
                    ["approval"] = "missing",
                },
                cancellationToken);
        }

        if (execution.AuthorizationContext is null)
        {
            const string reason = "Trusted canonical authorization context is required for approval.";
            await AppendAuditAsync(
                execution,
                AuditEventKind.ApprovalEvaluated,
                "mismatch",
                reason,
                cancellationToken);
            return await DenyAsync(
                execution,
                reason,
                new Dictionary<string, string>
                {
                    ["policy"] = decision.Kind.ToString().ToLowerInvariant(),
                    ["approval"] = "mismatch",
                },
                cancellationToken);
        }

        if (execution.ApprovalId is null)
        {
            const string reason = "Exact owner approval is required for this action.";
            await AppendAuditAsync(
                execution,
                AuditEventKind.ApprovalEvaluated,
                "missing",
                reason,
                cancellationToken);
            return await DenyAsync(
                execution,
                reason,
                new Dictionary<string, string>
                {
                    ["policy"] = decision.Kind.ToString().ToLowerInvariant(),
                    ["approval"] = "missing",
                },
                cancellationToken);
        }

        ActionAuthorizationContext authorizationContext = execution.AuthorizationContext;
        string fingerprint = ActionIntentFingerprint.Compute(
            definition,
            execution.Request,
            authorizationContext);
        ApprovalConsumptionRequest consumptionRequest = new(
            execution.ApprovalId.Value,
            authorizationContext.OwnerPrincipalReference,
            definition.Name,
            authorizationContext.ProjectId,
            authorizationContext.RepositoryId,
            fingerprint,
            _timeProvider.GetUtcNow());

        ApprovalConsumptionResult consumption;
        try
        {
            consumption = await _approvalStore.ConsumeAsync(
                consumptionRequest,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AppendCancellationAuditAsync(
                execution,
                AuditEventKind.ApprovalEvaluated,
                "Approval validation was cancelled.");
            await AppendCancellationAuditAsync(
                execution,
                AuditEventKind.ActionCompleted,
                "Action was cancelled before approval consumption completed.");
            throw;
        }
        catch (Exception exception)
        {
            string reason = $"Approval validation failed with {exception.GetType().Name}.";
            await AppendAuditAsync(
                execution,
                AuditEventKind.ApprovalEvaluated,
                "error",
                reason,
                cancellationToken);
            return await DenyAsync(
                execution,
                reason,
                new Dictionary<string, string>
                {
                    ["policy"] = decision.Kind.ToString().ToLowerInvariant(),
                    ["approval"] = "error",
                },
                cancellationToken);
        }

        string approvalOutcome = consumption.Status.ToString().ToLowerInvariant();
        await AppendAuditAsync(
            execution,
            AuditEventKind.ApprovalEvaluated,
            approvalOutcome,
            $"Approval {execution.ApprovalId.Value} — {consumption.Reason}",
            cancellationToken);

        if (!consumption.IsConsumed)
        {
            return await DenyAsync(
                execution,
                consumption.Reason,
                new Dictionary<string, string>
                {
                    ["policy"] = decision.Kind.ToString().ToLowerInvariant(),
                    ["approval"] = approvalOutcome,
                },
                cancellationToken);
        }

        return null;
    }

    private async Task<ActionResult> DenyAsync(
        ActionExecutionRequest execution,
        string reason,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        await AppendAuditAsync(
            execution,
            AuditEventKind.ActionCompleted,
            "denied",
            reason,
            cancellationToken);

        return new ActionResult(
            execution.Request.Name,
            false,
            data,
            reason);
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
