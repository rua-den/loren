using Loren.Core.Actions;

namespace Loren.Core.Brains;

public interface IBrain
{
    Task<BrainTurnResult> ThinkAsync(
        BrainContext context,
        IReadOnlyList<ActionDefinition> availableActions,
        CancellationToken cancellationToken);
}
