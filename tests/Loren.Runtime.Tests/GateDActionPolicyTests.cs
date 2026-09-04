using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Projects;
using Xunit;

namespace Loren.Runtime.Tests;

public sealed class GateDActionPolicyTests
{
    [Fact]
    public async Task ReadActionStillExecutesWithoutApproval()
    {
        ActionDefinition definition = new(
            "github.read_repository",
            "Read repository",
            ActionAccessClass.Read);
        RecordingExecutor executor = new(definition.Name);
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: true)),
            audit);

        ActionResult result = await gateway.ExecuteAsync(
            Execution(definition.Name),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(executor.Requests);
        Assert.DoesNotContain(
            audit.Events,
            auditEvent => auditEvent.Kind is AuditEventKind.ApprovalEvaluated);
    }

    [Fact]
    public async Task GlobalReadOnlyDeniesWriteBeforeApprovalConsumptionAndExecutor()
    {
        ActionDefinition definition = WriteDefinition();
        ActionAuthorizationContext authorization = AuthorizationContext();
        RecordingApprovalStore approvalStore = new();
        RecordingExecutor executor = new(definition.Name);
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: true)),
            new RecordingAuditSink(),
            approvalStore);
        ActionExecutionRequest execution = Execution(
            definition.Name,
            authorization,
            ApprovalId.New());

        ActionResult result = await gateway.ExecuteAsync(execution, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("deny", result.Data["policy"]);
        Assert.Equal(0, approvalStore.ConsumeCalls);
        Assert.Empty(executor.Requests);
    }

    [Fact]
    public async Task WriteWithoutTrustedCanonicalContextFailsBeforeApprovalStore()
    {
        ActionDefinition definition = WriteDefinition();
        RecordingApprovalStore approvalStore = new();
        RecordingExecutor executor = new(definition.Name);
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: false)),
            new RecordingAuditSink(),
            approvalStore);

        ActionResult result = await gateway.ExecuteAsync(
            Execution(definition.Name, approvalId: ApprovalId.New()),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, approvalStore.ConsumeCalls);
        Assert.Empty(executor.Requests);
    }

    [Fact]
    public async Task ExactApprovalIsConsumedOnceAndReplayIsDenied()
    {
        ActionDefinition definition = WriteDefinition();
        ActionAuthorizationContext authorization = AuthorizationContext();
        ApprovalId approvalId = ApprovalId.New();
        ActionExecutionRequest execution = Execution(
            definition.Name,
            authorization,
            approvalId,
            new Dictionary<string, string>
            {
                ["content_digest"] = "ABC",
            });
        string fingerprint = ActionIntentFingerprint.Compute(
            definition,
            execution.Request,
            authorization);
        RecordingApprovalStore approvalStore = new(
            new ActionApproval(
                approvalId,
                authorization.OwnerPrincipalReference,
                definition.Name,
                authorization.ProjectId,
                authorization.RepositoryId,
                fingerprint,
                new DateTimeOffset(2026, 9, 4, 16, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 5, 16, 0, 0, TimeSpan.Zero)));
        RecordingExecutor executor = new(definition.Name);
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: false)),
            audit,
            approvalStore);

        ActionResult first = await gateway.ExecuteAsync(execution, CancellationToken.None);
        ActionResult replay = await gateway.ExecuteAsync(execution, CancellationToken.None);

        Assert.True(first.Success);
        Assert.False(replay.Success);
        Assert.Equal("alreadyconsumed", replay.Data["approval"]);
        Assert.Single(executor.Requests);
        Assert.Equal(2, approvalStore.ConsumeCalls);
        Assert.Contains(
            audit.Events,
            auditEvent => auditEvent.Kind is AuditEventKind.ApprovalEvaluated
                && auditEvent.Outcome == "consumed");
        Assert.Contains(
            audit.Events,
            auditEvent => auditEvent.Kind is AuditEventKind.ApprovalEvaluated
                && auditEvent.Outcome == "alreadyconsumed");
    }

    [Fact]
    public async Task SameApprovalCannotAuthorizeChangedTargetOrArguments()
    {
        ActionDefinition definition = WriteDefinition();
        ActionAuthorizationContext approvedContext = AuthorizationContext();
        ApprovalId approvalId = ApprovalId.New();
        ActionExecutionRequest approvedExecution = Execution(
            definition.Name,
            approvedContext,
            approvalId,
            new Dictionary<string, string>
            {
                ["content_digest"] = "ABC",
            });
        string fingerprint = ActionIntentFingerprint.Compute(
            definition,
            approvedExecution.Request,
            approvedContext);
        RecordingApprovalStore approvalStore = new(
            new ActionApproval(
                approvalId,
                approvedContext.OwnerPrincipalReference,
                definition.Name,
                approvedContext.ProjectId,
                approvedContext.RepositoryId,
                fingerprint,
                new DateTimeOffset(2026, 9, 4, 16, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 5, 16, 0, 0, TimeSpan.Zero)));
        RecordingExecutor executor = new(definition.Name);
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: false)),
            new RecordingAuditSink(),
            approvalStore);
        ActionAuthorizationContext changedTarget = new(
            approvedContext.ProjectId,
            approvedContext.RepositoryId,
            approvedContext.RepositoryLocator,
            approvedContext.OwnerPrincipalReference,
            new Dictionary<string, string>
            {
                ["branch"] = "main",
                ["path"] = "README.md",
            });

        ActionResult changedBranch = await gateway.ExecuteAsync(
            Execution(
                definition.Name,
                changedTarget,
                approvalId,
                approvedExecution.Request.Arguments),
            CancellationToken.None);
        ActionResult changedContent = await gateway.ExecuteAsync(
            Execution(
                definition.Name,
                approvedContext,
                approvalId,
                new Dictionary<string, string>
                {
                    ["content_digest"] = "DEF",
                }),
            CancellationToken.None);

        Assert.False(changedBranch.Success);
        Assert.False(changedContent.Success);
        Assert.Equal("mismatch", changedBranch.Data["approval"]);
        Assert.Equal("mismatch", changedContent.Data["approval"]);
        Assert.Empty(executor.Requests);
    }

    [Fact]
    public async Task ModelVisibleApprovalArgumentCannotReplaceTrustedApprovalField()
    {
        ActionDefinition definition = WriteDefinition();
        ActionAuthorizationContext authorization = AuthorizationContext();
        RecordingApprovalStore approvalStore = new();
        RecordingExecutor executor = new(definition.Name);
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: false)),
            new RecordingAuditSink(),
            approvalStore);
        ActionExecutionRequest execution = Execution(
            definition.Name,
            authorization,
            arguments: new Dictionary<string, string>
            {
                ["approvalId"] = ApprovalId.New().ToString(),
            });

        ActionResult result = await gateway.ExecuteAsync(execution, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("missing", result.Data["approval"]);
        Assert.Equal(0, approvalStore.ConsumeCalls);
        Assert.Empty(executor.Requests);
    }

    [Fact]
    public async Task GatewayStillRequiresApprovalWhenWritePolicyReturnsAllow()
    {
        ActionDefinition definition = WriteDefinition();
        ActionAuthorizationContext authorization = AuthorizationContext();
        RecordingApprovalStore approvalStore = new();
        RecordingExecutor executor = new(definition.Name);
        ActionGateway gateway = new(
            [definition],
            [executor],
            new AllowAllPolicy(),
            new RecordingAuditSink(),
            approvalStore);

        ActionResult result = await gateway.ExecuteAsync(
            Execution(definition.Name, authorization),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("missing", result.Data["approval"]);
        Assert.Empty(executor.Requests);
    }

    private static ActionDefinition WriteDefinition() => new(
        "github.update_file",
        "Update a file",
        ActionAccessClass.ExternalWrite);

    private static ActionAuthorizationContext AuthorizationContext()
    {
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        return new ActionAuthorizationContext(
            projectId,
            repositoryId,
            new RepositoryLocator("github", "rua-den", "loren"),
            "owner:session-1",
            new Dictionary<string, string>
            {
                ["branch"] = "feat/approved",
                ["path"] = "README.md",
            });
    }

    private static ActionExecutionRequest Execution(
        string actionName,
        ActionAuthorizationContext? authorizationContext = null,
        ApprovalId? approvalId = null,
        IReadOnlyDictionary<string, string>? arguments = null) => new(
        RunId.New(),
        ActionId.New(),
        new ActionRequest(
            actionName,
            arguments ?? new Dictionary<string, string>()),
        authorizationContext,
        approvalId);

    private sealed class RecordingExecutor(string actionName) : IActionExecutor
    {
        public string ActionName { get; } = actionName;

        public List<ActionRequest> Requests { get; } = [];

        public Task<ActionResult> ExecuteAsync(
            ActionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return Task.FromResult(new ActionResult(
                request.Name,
                true,
                new Dictionary<string, string> { ["status"] = "ok" }));
        }
    }

    private sealed class RecordingApprovalStore(ActionApproval? approval = null) : IActionApprovalStore
    {
        private ActionApproval? _approval = approval;

        public int ConsumeCalls { get; private set; }

        public Task AddAsync(
            ActionApproval newApproval,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _approval = newApproval;
            return Task.CompletedTask;
        }

        public Task<ActionApproval?> GetAsync(
            ApprovalId approvalId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                _approval?.Id == approvalId ? _approval : null);
        }

        public Task<ApprovalConsumptionResult> ConsumeAsync(
            ApprovalConsumptionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConsumeCalls++;

            if (_approval is null || _approval.Id != request.ApprovalId)
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.Unknown,
                    "Approval does not exist."));
            }

            if (_approval.ConsumedAt is not null)
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.AlreadyConsumed,
                    "Approval has already been consumed and cannot be replayed."));
            }

            bool matches = _approval.OwnerPrincipalReference == request.OwnerPrincipalReference
                && _approval.ActionName == request.ActionName
                && _approval.ProjectId == request.ProjectId
                && _approval.RepositoryId == request.RepositoryId
                && _approval.IntentFingerprint == request.IntentFingerprint;

            if (!matches)
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.Mismatch,
                    "Approval does not match the exact normalized action intent."));
            }

            _approval = new ActionApproval(
                _approval.Id,
                _approval.OwnerPrincipalReference,
                _approval.ActionName,
                _approval.ProjectId,
                _approval.RepositoryId,
                _approval.IntentFingerprint,
                _approval.ApprovedAt,
                _approval.ExpiresAt,
                request.ConsumedAt,
                _approval.RevokedAt);

            return Task.FromResult(new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.Consumed,
                "Exact owner approval was consumed for this action intent."));
        }

        public Task RevokeAsync(
            ApprovalId approvalId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public List<AuditEvent> Events { get; } = [];

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class AllowAllPolicy : IActionPolicy
    {
        public Task<PolicyDecision> EvaluateAsync(
            ActionDefinition definition,
            ActionExecutionRequest execution,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(PolicyDecision.Allow("test"));
        }
    }
}
