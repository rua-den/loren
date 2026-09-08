using Loren.Core.Actions;
using Loren.Core.Projects;
using Xunit;

namespace Loren.Core.Tests;

public sealed class CreateBranchProposalTests
{
    [Fact]
    public void ConstructorNormalizesOwnerAndShaAndExposesBoundedAction()
    {
        DateTimeOffset created = DateTimeOffset.UtcNow;
        CreateBranchProposal proposal = new(
            CreateBranchProposalId.New(),
            " owner:one ",
            ProjectId.New(),
            RepositoryId.New(),
            new RepositoryLocator("GitHub", "owner", "repo"),
            "feature/fix",
            "refs/heads/main",
            new string('A', 40),
            "fingerprint",
            created,
            created.AddMinutes(5));

        Assert.Equal("owner:one", proposal.OwnerPrincipalReference);
        Assert.Equal(new string('a', 40), proposal.SourceSha);
        Assert.Equal("github.create_branch", proposal.ActionName);
        Assert.Equal(ActionAccessClass.ReversibleWrite, proposal.AccessClass);
        Assert.Equal(CreateBranchProposalStatus.Pending, proposal.Status);
    }

    [Theory]
    [InlineData(" ", "main", "refs/heads/main", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("owner", " feature", "refs/heads/main", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("owner", "feature..x", "refs/heads/main", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("owner", "feature", " refs/heads/main", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("owner", "feature", "refs//heads/main", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("owner", "feature", "refs/heads/main", "not-a-sha")]
    public void ConstructorRejectsInvalidNormalizedValues(
        string owner,
        string branch,
        string sourceRef,
        string sha)
    {
        DateTimeOffset created = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => new CreateBranchProposal(
            CreateBranchProposalId.New(),
            owner,
            ProjectId.New(),
            RepositoryId.New(),
            new RepositoryLocator("github", "owner", "repo"),
            branch,
            sourceRef,
            sha,
            "fingerprint",
            created,
            created.AddMinutes(5)));
    }

    [Fact]
    public void ConstructorRejectsInvalidLifecycleCombinations()
    {
        DateTimeOffset created = DateTimeOffset.UtcNow;
        DateTimeOffset expiry = created.AddMinutes(5);
        CreateBranchProposal Make(
            CreateBranchProposalStatus status,
            DateTimeOffset? decided,
            DateTimeOffset? createdOverride = null,
            DateTimeOffset? expiryOverride = null) => new(
            CreateBranchProposalId.New(), "owner", ProjectId.New(), RepositoryId.New(),
            new RepositoryLocator("github", "owner", "repo"), "feature", "refs/heads/main",
            new string('a', 40), "fingerprint", createdOverride ?? created,
            expiryOverride ?? expiry, status, decided);

        Assert.Throws<ArgumentException>(() => Make(CreateBranchProposalStatus.Pending, created.AddMinutes(1)));
        Assert.Throws<ArgumentException>(() => Make(CreateBranchProposalStatus.Approved, null));
        Assert.Throws<ArgumentException>(() => Make(CreateBranchProposalStatus.Cancelled, null));
        Assert.Throws<ArgumentException>(() => Make(CreateBranchProposalStatus.Approved, created.AddMinutes(-1)));
        Assert.Throws<ArgumentException>(() => Make(CreateBranchProposalStatus.Pending, null, created, created.AddMinutes(4)));
        Assert.Throws<ArgumentException>(() => Make(CreateBranchProposalStatus.Pending, null, created, created.AddMinutes(6)));
    }
}
