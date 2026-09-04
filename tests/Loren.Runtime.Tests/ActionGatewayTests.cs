using Loren.Core.Actions;
using Loren.Core.Audit;
using Xunit;

namespace Loren.Runtime.Tests;

public sealed class ActionGatewayTests
{
    [Fact]
    public async Task AllowsRegisteredReadOnlyActionAndAuditsCompleteSequence()
    {
        ActionDefinition definition = new("read", "read", true);
        RecordingExecutor executor = new("read");
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new ReadOnlyActionPolicy(),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest("read", new Dictionary<string, string>()));

        ActionResult result = await gateway.ExecuteAsync(execution, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(executor.Requests);
        Assert.Equal(
            [
                AuditEventKind.ActionRequested,
                AuditEventKind.PolicyEvaluated,
                AuditEventKind.ActionCompleted,
            ],
            audit.Events.Select(auditEvent => auditEvent.Kind));
        Assert.All(audit.Events, auditEvent => Assert.Equal(execution.RunId, auditEvent.RunId));
        Assert.All(audit.Events, auditEvent => Assert.Equal(execution.ActionId, auditEvent.ActionId));
    }

    [Fact]
    public async Task DeniesWriteActionBeforeExecutorAndAuditsDenial()
    {
        ActionDefinition definition = new("write", "write", false);
        RecordingExecutor executor = new("write");
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new ReadOnlyActionPolicy(),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest("write", new Dictionary<string, string>()));

        ActionResult result = await gateway.ExecuteAsync(execution, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(executor.Requests);
        Assert.Equal("deny", result.Data["policy"]);
        Assert.Equal("denied", audit.Events[^1].Outcome);
    }

    [Fact]
    public async Task ExecutorCancellationWritesTerminalAuditBeforePropagating()
    {
        ActionDefinition definition = new("read", "read", true);
        CancellingExecutor executor = new("read");
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new ReadOnlyActionPolicy(),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest("read", new Dictionary<string, string>()));
        using CancellationTokenSource cancellation = new();

        Task run = gateway.ExecuteAsync(execution, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(AuditEventKind.ActionCompleted, audit.Events[^1].Kind);
        Assert.Equal("cancelled", audit.Events[^1].Outcome);
    }

    [Fact]
    public async Task PolicyFailureFailsClosedAndAuditsTerminalFailure()
    {
        ActionDefinition definition = new("read", "read", true);
        RecordingExecutor executor = new("read");
        RecordingAuditSink audit = new();
        ActionGateway gateway = new(
            [definition],
            [executor],
            new ThrowingPolicy(),
            audit);
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            new ActionRequest("read", new Dictionary<string, string>()));

        ActionResult result = await gateway.ExecuteAsync(execution, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(executor.Requests);
        Assert.Equal("error", result.Data["policy"]);
        Assert.Equal("failed", audit.Events[^1].Outcome);
    }

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

    private sealed class CancellingExecutor(string actionName) : IActionExecutor
    {
        public string ActionName { get; } = actionName;

        public async Task<ActionResult> ExecuteAsync(
            ActionRequest request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        }
    }

    private sealed class ThrowingPolicy : IActionPolicy
    {
        public Task<PolicyDecision> EvaluateAsync(
            ActionDefinition definition,
            ActionExecutionRequest execution,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("policy detail must not escape");
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
}
