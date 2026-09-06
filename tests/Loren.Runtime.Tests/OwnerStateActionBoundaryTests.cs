using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Runtime;
using Xunit;

namespace Loren.Runtime.Tests;

public sealed class OwnerStateActionBoundaryTests
{
    [Fact]
    public async Task AuthenticatedOwnerStateWriteExecutesWithoutExternalApprovalEvenInReadOnlyMode()
    {
        ActionDefinition definition = new(
            "owner.test_write",
            "Test authenticated owner-state write.",
            ActionAccessClass.OwnerStateWrite);
        RecordingTrustedExecutor executor = new(definition.Name);
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: true)),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest(definition.Name, new Dictionary<string, string>()),
            OwnerContext: new AuthenticatedOwnerContext("owner"));

        ActionResult result = await gateway.ExecuteAsync(
            execution,
            TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(1, executor.TrustedCalls);
        Assert.DoesNotContain(
            audit.Events,
            item => item.Kind == AuditEventKind.ApprovalEvaluated);
    }

    [Fact]
    public async Task OwnerStateReadFailsClosedWithoutAuthenticatedOwnerContextEvenWithReadPolicy()
    {
        ActionDefinition definition = new(
            "owner.test_read",
            "Test authenticated owner-state read.",
            ActionAccessClass.OwnerStateRead);
        RecordingTrustedExecutor executor = new(definition.Name);
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new ReadOnlyActionPolicy(),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest(definition.Name, new Dictionary<string, string>()));

        ActionResult result = await gateway.ExecuteAsync(
            execution,
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(0, executor.TrustedCalls);
        Assert.Contains("owner context", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            audit.Events,
            item => item.Kind == AuditEventKind.ApprovalEvaluated);
    }

    [Fact]
    public async Task OwnerStateActionsStillRequireTrustedExecutorRegistration()
    {
        ActionDefinition definition = new(
            "owner.test_write",
            "Test authenticated owner-state write.",
            ActionAccessClass.OwnerStateWrite);
        LegacyExecutor executor = new(definition.Name);
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: true)),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest(definition.Name, new Dictionary<string, string>()),
            OwnerContext: new AuthenticatedOwnerContext("owner"));

        ActionResult result = await gateway.ExecuteAsync(
            execution,
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(0, executor.Calls);
        Assert.Contains("trusted execution context", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalWriteStillRequiresCanonicalAuthorizationAndIsNotReclassifiedAsOwnerState()
    {
        ActionDefinition definition = new(
            "external.test_write",
            "Test external write.",
            ActionAccessClass.ReversibleWrite);
        RecordingTrustedExecutor executor = new(definition.Name);
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: true)),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest(definition.Name, new Dictionary<string, string>()),
            OwnerContext: new AuthenticatedOwnerContext("owner"));

        ActionResult result = await gateway.ExecuteAsync(
            execution,
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(0, executor.TrustedCalls);
        Assert.Contains("canonical authorization", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingTrustedExecutor(string actionName) : ITrustedActionExecutor
    {
        public string ActionName { get; } = actionName;

        public int TrustedCalls { get; private set; }

        public Task<ActionResult> ExecuteAsync(
            ActionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ActionResult(
                request.Name,
                false,
                new Dictionary<string, string>(),
                "Direct execution is not supported."));

        public Task<ActionResult> ExecuteTrustedAsync(
            ActionExecutionRequest execution,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrustedCalls++;
            return Task.FromResult(new ActionResult(
                execution.Request.Name,
                true,
                new Dictionary<string, string>()));
        }
    }

    private sealed class LegacyExecutor(string actionName) : IActionExecutor
    {
        public string ActionName { get; } = actionName;

        public int Calls { get; private set; }

        public Task<ActionResult> ExecuteAsync(
            ActionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(new ActionResult(
                request.Name,
                true,
                new Dictionary<string, string>()));
        }
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public List<AuditEvent> Events { get; } = [];

        public Task AppendAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
