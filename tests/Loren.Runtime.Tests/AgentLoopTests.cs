using Loren.Core.Actions;
using Loren.Core.Brains;
using Xunit;

namespace Loren.Runtime.Tests;

public sealed class AgentLoopTests
{
    private static readonly IReadOnlyList<ActionDefinition> Actions =
    [
        new("get_project_status", "Read project status", true),
    ];

    [Fact]
    public async Task ReturnsFinalResponseWithoutCallingGateway()
    {
        SequenceBrain brain = new(BrainTurnResult.Final("done"));
        RecordingGateway gateway = new();
        AgentLoop loop = new(brain, gateway, new AgentLoopOptions());

        AgentRunResult result = await loop.RunAsync(
            BrainContext.FromUser("hello"),
            Actions,
            CancellationToken.None);

        Assert.Equal("done", result.FinalOutput);
        Assert.NotEqual(Guid.Empty, result.RunId.Value);
        Assert.Equal(1, result.Turns);
        Assert.Equal(0, result.ActionCount);
        Assert.Empty(gateway.Executions);
    }

    [Fact]
    public async Task RoutesActionThroughGatewayWithLorenOwnedCorrelationIds()
    {
        ActionRequest action = new(
            "get_project_status",
            new Dictionary<string, string> { ["project"] = "Loren" });

        SequenceBrain brain = new(
            BrainTurnResult.Request(action),
            BrainTurnResult.Final("Loren is healthy."));
        RecordingGateway gateway = new();
        AgentLoop loop = new(brain, gateway, new AgentLoopOptions());

        AgentRunResult result = await loop.RunAsync(
            BrainContext.FromUser("check Loren"),
            Actions,
            CancellationToken.None);

        ActionExecutionRequest execution = Assert.Single(gateway.Executions);
        Assert.Equal(result.RunId, execution.RunId);
        Assert.NotEqual(Guid.Empty, execution.ActionId.Value);
        Assert.Equal(1, result.ActionCount);
        Assert.Equal(2, brain.SeenContexts.Count);
        Assert.Contains(brain.SeenContexts[1].Inputs, input => input is BrainActionObservation);
    }

    [Fact]
    public async Task FailsWhenTurnLimitIsExceeded()
    {
        ActionRequest action = new("get_project_status", new Dictionary<string, string>());
        SequenceBrain brain = new(
            BrainTurnResult.Request(action),
            BrainTurnResult.Request(action));
        RecordingGateway gateway = new();
        AgentLoop loop = new(brain, gateway, new AgentLoopOptions(MaxTurns: 2, MaxActions: 2));

        await Assert.ThrowsAsync<AgentLoopLimitException>(() => loop.RunAsync(
            BrainContext.FromUser("loop"),
            Actions,
            CancellationToken.None));
    }

    private sealed class SequenceBrain : IBrain
    {
        private readonly Queue<BrainTurnResult> _results;

        public SequenceBrain(params BrainTurnResult[] results)
        {
            _results = new Queue<BrainTurnResult>(results);
        }

        public List<BrainContext> SeenContexts { get; } = [];

        public Task<BrainTurnResult> ThinkAsync(
            BrainContext context,
            IReadOnlyList<ActionDefinition> availableActions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SeenContexts.Add(context);
            return Task.FromResult(_results.Dequeue());
        }
    }

    private sealed class RecordingGateway : IActionGateway
    {
        public List<ActionExecutionRequest> Executions { get; } = [];

        public Task<ActionResult> ExecuteAsync(
            ActionExecutionRequest execution,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Executions.Add(execution);

            ActionResult result = new(
                execution.Request.Name,
                true,
                new Dictionary<string, string> { ["status"] = "ok" });
            return Task.FromResult(result);
        }
    }
}
