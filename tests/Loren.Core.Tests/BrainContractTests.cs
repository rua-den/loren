using Loren.Core.Actions;
using Loren.Core.Brains;
using Xunit;

namespace Loren.Core.Tests;

public sealed class BrainContractTests
{
    [Fact]
    public void ContextAppendDoesNotMutateOriginal()
    {
        BrainContext original = BrainContext.FromUser("hello");
        ActionRequest request = new("read", new Dictionary<string, string>());
        ActionResult result = new("read", true, new Dictionary<string, string>());

        BrainContext updated = original.Append(new BrainActionObservation(request, result));

        Assert.Single(original.Inputs);
        Assert.Equal(2, updated.Inputs.Count);
    }

    [Fact]
    public void FinalTurnRejectsEmptyOutput()
    {
        Assert.Throws<ArgumentException>(() => BrainTurnResult.Final(" "));
    }
}
