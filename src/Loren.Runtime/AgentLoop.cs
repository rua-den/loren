using Loren.Core.Actions;
using Loren.Core.Brains;

namespace Loren.Runtime;

public sealed class AgentLoop
{
    private readonly IBrain _brain;
    private readonly IActionGateway _gateway;
    private readonly AgentLoopOptions _options;

    public AgentLoop(IBrain brain, IActionGateway gateway, AgentLoopOptions options)
    {
        _brain = brain ?? throw new ArgumentNullException(nameof(brain));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    public async Task<AgentRunResult> RunAsync(
        BrainContext initialContext,
        IReadOnlyList<ActionDefinition> availableActions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initialContext);
        ArgumentNullException.ThrowIfNull(availableActions);

        RunId runId = RunId.New();
        BrainContext context = initialContext;
        int actionCount = 0;

        for (int turn = 1; turn <= _options.MaxTurns; turn++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BrainTurnResult brainTurn = await _brain.ThinkAsync(
                context,
                availableActions,
                cancellationToken);

            if (brainTurn.IsFinal)
            {
                return new AgentRunResult(
                    brainTurn.FinalOutput!,
                    runId,
                    turn,
                    actionCount);
            }

            ActionRequest request = brainTurn.ActionRequest
                ?? throw new InvalidOperationException("Brain turn contained neither final output nor an action request.");

            actionCount++;
            if (actionCount > _options.MaxActions)
            {
                throw new AgentLoopLimitException(
                    $"Agent run exceeded the action limit of {_options.MaxActions}.");
            }

            ActionExecutionRequest execution = new(
                runId,
                ActionId.New(),
                request);
            ActionResult result = await _gateway.ExecuteAsync(execution, cancellationToken);
            context = context.Append(new BrainActionObservation(request, result));
        }

        throw new AgentLoopLimitException(
            $"Agent run exceeded the turn limit of {_options.MaxTurns}.");
    }
}

public sealed record AgentRunResult(
    string FinalOutput,
    RunId RunId,
    int Turns,
    int ActionCount);

public sealed record AgentLoopOptions(
    int MaxTurns = 6,
    int MaxActions = 4)
{
    internal void Validate()
    {
        if (MaxTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxTurns));
        }

        if (MaxActions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxActions));
        }
    }
}

public sealed class AgentLoopLimitException(string message) : InvalidOperationException(message);
