using Loren.Core.Actions;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CreateBranchProposalStoreTests
{
    [Fact]
    public async Task ProposalRoundTripsAcrossRestartAndApprovalConsumesAtomically()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-m6a5-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string connection = $"Data Source={Path.Combine(directory, "state.db")};Pooling=False";
        DateTimeOffset created = new(2026, 9, 7, 1, 0, 0, TimeSpan.Zero);
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        CreateBranchProposal proposal = Proposal(projectId, repositoryId, created);
        try
        {
            await using (CanonicalStateDbContext context = Context(connection))
            {
                await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
                await SeedScope(context, projectId, repositoryId, created);
                SqliteCreateBranchProposalStore store = new(context);
                await store.AddAsync(proposal, TestContext.Current.CancellationToken);
                CreateBranchProposalDecisionRequest decision = new(proposal.Id, "owner:one", created.AddMinutes(1));
                ActionApproval approval = new(ApprovalId.New(), "owner:one", proposal.ActionName, projectId, repositoryId,
                    proposal.IntentFingerprint, decision.DecidedAt, decision.DecidedAt.AddMinutes(5));
                CreateBranchProposalDecisionResult result = await store.ApproveAsync(decision, approval, TestContext.Current.CancellationToken);
                Assert.Equal(CreateBranchProposalDecisionStatus.Approved, result.Status);
                ActionApproval? persistedApproval = await new SqliteActionApprovalStore(context).GetAsync(approval.Id, TestContext.Current.CancellationToken);
                Assert.NotNull(persistedApproval);
                Assert.Null(persistedApproval.ConsumedAt);
            }

            await using CanonicalStateDbContext restarted = Context(connection);
            CreateBranchProposal? loaded = await new SqliteCreateBranchProposalStore(restarted).GetAsync(proposal.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            Assert.Equal(CreateBranchProposalStatus.Approved, loaded.Status);
            Assert.Equal(proposal.OwnerPrincipalReference, loaded.OwnerPrincipalReference);
            Assert.Equal(proposal.ProjectId, loaded.ProjectId);
            Assert.Equal(proposal.RepositoryId, loaded.RepositoryId);
            Assert.Equal(proposal.RepositoryLocator, loaded.RepositoryLocator);
            Assert.Equal(proposal.Branch, loaded.Branch);
            Assert.Equal(proposal.SourceRef, loaded.SourceRef);
            Assert.Equal(proposal.SourceSha, loaded.SourceSha);
            Assert.Equal(proposal.IntentFingerprint, loaded.IntentFingerprint);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task WrongOwnerDoesNotRevealProposalAndCancelWinsOnlyOnce()
    {
        string connection = $"Data Source=file:m6a5-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using CanonicalStateDbContext context = Context(connection);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        await SeedScope(context, projectId, repositoryId, created);
        CreateBranchProposal proposal = Proposal(projectId, repositoryId, created);
        SqliteCreateBranchProposalStore store = new(context);
        await store.AddAsync(proposal, TestContext.Current.CancellationToken);
        CreateBranchProposalDecisionRequest wrong = new(proposal.Id, "owner:two", created.AddMinutes(1));
        CreateBranchProposalDecisionResult wrongResult = await store.CancelAsync(wrong, TestContext.Current.CancellationToken);
        Assert.Equal(CreateBranchProposalDecisionStatus.OwnerMismatch, wrongResult.Status);
        Assert.Null(wrongResult.Proposal);
        CreateBranchProposalDecisionResult cancelled = await store.CancelAsync(
            new(proposal.Id, "owner:one", created.AddMinutes(1)), TestContext.Current.CancellationToken);
        Assert.Equal(CreateBranchProposalDecisionStatus.Cancelled, cancelled.Status);
        CreateBranchProposalDecisionResult again = await store.CancelAsync(
            new(proposal.Id, "owner:one", created.AddMinutes(1)), TestContext.Current.CancellationToken);
        Assert.Equal(CreateBranchProposalDecisionStatus.AlreadyDecided, again.Status);
    }

    [Fact]
    public async Task ApprovalMismatchAndExpiredDecisionAreRejected()
    {
        string connection = $"Data Source={Path.Combine(Path.GetTempPath(), $"loren-m6a5-{Guid.NewGuid():N}.db")};Pooling=False";
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        CreateBranchProposal proposal = Proposal(projectId, repositoryId, created);
        await using CanonicalStateDbContext context = Context(connection);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        await SeedScope(context, projectId, repositoryId, created);
        SqliteCreateBranchProposalStore store = new(context);
        await store.AddAsync(proposal, TestContext.Current.CancellationToken);
        CreateBranchProposalDecisionRequest decision = new(proposal.Id, "owner:one", created.AddMinutes(1));
        ActionApproval mismatch = new(ApprovalId.New(), "owner:one", proposal.ActionName, projectId, repositoryId, "different", decision.DecidedAt, decision.DecidedAt.AddMinutes(5));
        Assert.Equal(CreateBranchProposalDecisionStatus.Mismatch, (await store.ApproveAsync(decision, mismatch, TestContext.Current.CancellationToken)).Status);
        CreateBranchProposalDecisionRequest expiredDecision = new(proposal.Id, "owner:one", created.AddMinutes(20));
        ActionApproval valid = new(ApprovalId.New(), "owner:one", proposal.ActionName, projectId, repositoryId, proposal.IntentFingerprint, expiredDecision.DecidedAt, expiredDecision.DecidedAt.AddMinutes(5));
        Assert.Equal(CreateBranchProposalDecisionStatus.Expired, (await store.ApproveAsync(expiredDecision, valid, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task TwoIndependentApproversProduceExactlyOneTerminalDecisionAndApproval()
    {
        string path = Path.Combine(Path.GetTempPath(), $"loren-m6a5-race-{Guid.NewGuid():N}.db");
        string connection = $"Data Source={path};Pooling=False";
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        CreateBranchProposal proposal = Proposal(projectId, repositoryId, created);
        try
        {
            await using (CanonicalStateDbContext seed = Context(connection))
            {
                await CanonicalStateDatabase.MigrateAsync(seed, TestContext.Current.CancellationToken);
                await SeedScope(seed, projectId, repositoryId, created);
                await new SqliteCreateBranchProposalStore(seed).AddAsync(proposal, TestContext.Current.CancellationToken);
            }

            DateTimeOffset decided = created.AddMinutes(1);
            await using CanonicalStateDbContext first = Context(connection);
            await using CanonicalStateDbContext second = Context(connection);
            SqliteCreateBranchProposalStore firstStore = new(first);
            SqliteCreateBranchProposalStore secondStore = new(second);
            ActionApproval firstApproval = Approval(proposal, projectId, repositoryId, decided);
            ActionApproval secondApproval = Approval(proposal, projectId, repositoryId, decided);
            using Barrier barrier = new(2);
            Task<CreateBranchProposalDecisionResult> firstResult = Task.Run(async () => { barrier.SignalAndWait(); return await firstStore.ApproveAsync(new(proposal.Id, "owner:one", decided), firstApproval, TestContext.Current.CancellationToken); });
            Task<CreateBranchProposalDecisionResult> secondResult = Task.Run(async () => { barrier.SignalAndWait(); return await secondStore.ApproveAsync(new(proposal.Id, "owner:one", decided), secondApproval, TestContext.Current.CancellationToken); });
            CreateBranchProposalDecisionResult[] results = await Task.WhenAll(firstResult, secondResult);
            Assert.Single(results, result => result.Status == CreateBranchProposalDecisionStatus.Approved);
            Assert.Single(results, result => result.Status == CreateBranchProposalDecisionStatus.AlreadyDecided);
            await using CanonicalStateDbContext verify = Context(connection);
            Assert.Equal(CreateBranchProposalStatus.Approved, (await new SqliteCreateBranchProposalStore(verify).GetAsync(proposal.Id, TestContext.Current.CancellationToken))!.Status);
            SqliteActionApprovalStore approvals = new(verify);
            Assert.Equal(1, (await approvals.GetAsync(firstApproval.Id, TestContext.Current.CancellationToken) is not null ? 1 : 0)
                + (await approvals.GetAsync(secondApproval.Id, TestContext.Current.CancellationToken) is not null ? 1 : 0));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApproveAndCancelRaceHasOneTerminalDecision()
    {
        string path = Path.Combine(Path.GetTempPath(), $"loren-m6a5-cancel-race-{Guid.NewGuid():N}.db");
        string connection = $"Data Source={path};Pooling=False";
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        CreateBranchProposal proposal = Proposal(projectId, repositoryId, created);
        try
        {
            await using (CanonicalStateDbContext seed = Context(connection))
            {
                await CanonicalStateDatabase.MigrateAsync(seed, TestContext.Current.CancellationToken);
                await SeedScope(seed, projectId, repositoryId, created);
                await new SqliteCreateBranchProposalStore(seed).AddAsync(proposal, TestContext.Current.CancellationToken);
            }

            DateTimeOffset decided = created.AddMinutes(1);
            await using CanonicalStateDbContext first = Context(connection);
            await using CanonicalStateDbContext second = Context(connection);
            ActionApproval approval = Approval(proposal, projectId, repositoryId, decided);
            using Barrier barrier = new(2);
            Task<CreateBranchProposalDecisionResult> approve = Task.Run(async () => { barrier.SignalAndWait(); return await new SqliteCreateBranchProposalStore(first).ApproveAsync(new(proposal.Id, "owner:one", decided), approval, TestContext.Current.CancellationToken); });
            Task<CreateBranchProposalDecisionResult> cancel = Task.Run(async () => { barrier.SignalAndWait(); return await new SqliteCreateBranchProposalStore(second).CancelAsync(new(proposal.Id, "owner:one", decided), TestContext.Current.CancellationToken); });
            CreateBranchProposalDecisionResult[] results = await Task.WhenAll(approve, cancel);
            Assert.Single(results, result => result.Status is CreateBranchProposalDecisionStatus.Approved or CreateBranchProposalDecisionStatus.Cancelled);
            Assert.Single(results, result => result.Status == CreateBranchProposalDecisionStatus.AlreadyDecided);
            await using CanonicalStateDbContext verify = Context(connection);
            CreateBranchProposal persisted = (await new SqliteCreateBranchProposalStore(verify).GetAsync(proposal.Id, TestContext.Current.CancellationToken))!;
            ActionApproval? persistedApproval = await new SqliteActionApprovalStore(verify).GetAsync(approval.Id, TestContext.Current.CancellationToken);
            if (results.Any(result => result.Status == CreateBranchProposalDecisionStatus.Approved))
            {
                Assert.Equal(CreateBranchProposalStatus.Approved, persisted.Status);
                Assert.NotNull(persistedApproval);
            }
            else
            {
                Assert.Equal(CreateBranchProposalStatus.Cancelled, persisted.Status);
                Assert.Null(persistedApproval);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApprovalInsertConflictRollsBackProposalTransition()
    {
        string connection = $"Data Source=file:m6a5-rollback-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using CanonicalStateDbContext context = Context(connection);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        await SeedScope(context, projectId, repositoryId, created);
        CreateBranchProposal proposal = Proposal(projectId, repositoryId, created);
        SqliteCreateBranchProposalStore store = new(context);
        await store.AddAsync(proposal, TestContext.Current.CancellationToken);
        DateTimeOffset decided = created.AddMinutes(1);
        ApprovalId duplicateId = ApprovalId.New();
        ActionApproval existing = new(duplicateId, "owner:one", proposal.ActionName, projectId, repositoryId, proposal.IntentFingerprint, decided, decided.AddMinutes(5));
        await new SqliteActionApprovalStore(context).AddAsync(existing, TestContext.Current.CancellationToken);
        ActionApproval duplicate = new(duplicateId, "owner:one", proposal.ActionName, projectId, repositoryId, proposal.IntentFingerprint, decided, decided.AddMinutes(5));
        await Assert.ThrowsAnyAsync<Exception>(() => store.ApproveAsync(new(proposal.Id, "owner:one", decided), duplicate, TestContext.Current.CancellationToken));
        Assert.Equal(CreateBranchProposalStatus.Pending, (await store.GetAsync(proposal.Id, TestContext.Current.CancellationToken))!.Status);
    }

    [Fact]
    public async Task AddRejectsTerminalProposalsWithoutWritingRows()
    {
        string connection = $"Data Source=file:m6a5-terminal-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using CanonicalStateDbContext context = Context(connection);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        await SeedScope(context, projectId, repositoryId, created);
        SqliteCreateBranchProposalStore store = new(context);
        foreach (CreateBranchProposalStatus status in new[] { CreateBranchProposalStatus.Approved, CreateBranchProposalStatus.Cancelled })
        {
            CreateBranchProposal terminal = new(CreateBranchProposalId.New(), "owner:one", projectId, repositoryId,
                new RepositoryLocator("github", "rua-den", "loren"), "feature/terminal", "refs/heads/main",
                new string('a', 40), "terminal", created, created.AddMinutes(5), status, created.AddMinutes(1));
            await Assert.ThrowsAsync<ArgumentException>(() => store.AddAsync(terminal, TestContext.Current.CancellationToken));
            Assert.Null(await store.GetAsync(terminal.Id, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task UnknownPrecreationAndForeignScopeDecisionsHaveNoSideEffects()
    {
        string connection = $"Data Source=file:m6a5-scope-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using CanonicalStateDbContext context = Context(connection);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        await SeedScope(context, projectId, repositoryId, created);
        SqliteCreateBranchProposalStore store = new(context);
        CreateBranchProposal proposal = Proposal(projectId, repositoryId, created);
        await store.AddAsync(proposal, TestContext.Current.CancellationToken);
        DateTimeOffset precreationAt = created.AddMilliseconds(-1);
        ActionApproval approval = Approval(proposal, projectId, repositoryId, precreationAt);
        CreateBranchProposalDecisionResult unknown = await store.ApproveAsync(
            new(CreateBranchProposalId.New(), "owner:one", created.AddMinutes(1)), approval, TestContext.Current.CancellationToken);
        Assert.Equal(CreateBranchProposalDecisionStatus.Unknown, unknown.Status);
        CreateBranchProposalDecisionResult precreation = await store.ApproveAsync(
            new(proposal.Id, "owner:one", precreationAt), approval, TestContext.Current.CancellationToken);
        Assert.Equal(CreateBranchProposalDecisionStatus.Mismatch, precreation.Status);
        Assert.Contains("predates", precreation.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CreateBranchProposalStatus.Pending, (await store.GetAsync(proposal.Id, TestContext.Current.CancellationToken))!.Status);

        ProjectId secondProjectId = ProjectId.New();
        RepositoryId secondRepositoryId = RepositoryId.New();
        await SeedScope(context, secondProjectId, secondRepositoryId, created, "second", "second");
        CreateBranchProposal missingProject = Proposal(ProjectId.New(), repositoryId, created);
        InvalidOperationException missingException = await Assert.ThrowsAsync<InvalidOperationException>(() => store.AddAsync(missingProject, TestContext.Current.CancellationToken));
        Assert.Contains("does not exist", missingException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await store.GetAsync(missingProject.Id, TestContext.Current.CancellationToken));
        CreateBranchProposal crossProject = Proposal(secondProjectId, repositoryId, created);
        InvalidOperationException crossProjectException = await Assert.ThrowsAsync<InvalidOperationException>(() => store.AddAsync(crossProject, TestContext.Current.CancellationToken));
        Assert.Contains("does not belong", crossProjectException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await store.GetAsync(crossProject.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EveryApprovalBindingMismatchLeavesProposalPendingWithoutApprovalRow()
    {
        string connection = $"Data Source=file:m6a5-bindings-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using CanonicalStateDbContext context = Context(connection);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        DateTimeOffset created = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        await SeedScope(context, projectId, repositoryId, created);
        SqliteCreateBranchProposalStore store = new(context);

        async Task CheckAsync(Func<CreateBranchProposal, DateTimeOffset, ActionApproval> makeApproval, int offset)
        {
            DateTimeOffset proposalCreated = created.AddMinutes(offset);
            CreateBranchProposal proposal = new(CreateBranchProposalId.New(), "owner:one", projectId, repositoryId,
                new RepositoryLocator("github", "rua-den", "loren"), $"feature/binding-{offset}", "refs/heads/main",
                new string('a', 40), "binding", proposalCreated, proposalCreated.AddMinutes(5));
            await store.AddAsync(proposal, TestContext.Current.CancellationToken);
            DateTimeOffset decided = proposalCreated.AddMinutes(1);
            ActionApproval candidate = makeApproval(proposal, decided);
            CreateBranchProposalDecisionResult result = await store.ApproveAsync(
                new(proposal.Id, "owner:one", decided), candidate, TestContext.Current.CancellationToken);
            Assert.Equal(CreateBranchProposalDecisionStatus.Mismatch, result.Status);
            Assert.Equal(CreateBranchProposalStatus.Pending, (await store.GetAsync(proposal.Id, TestContext.Current.CancellationToken))!.Status);
            Assert.Null(await new SqliteActionApprovalStore(context).GetAsync(candidate.Id, TestContext.Current.CancellationToken));
        }

        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), "owner:two", p.ActionName, p.ProjectId, p.RepositoryId, p.IntentFingerprint, at, at.AddMinutes(5)), 1);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, "other.action", p.ProjectId, p.RepositoryId, p.IntentFingerprint, at, at.AddMinutes(5)), 2);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, p.ActionName, ProjectId.New(), p.RepositoryId, p.IntentFingerprint, at, at.AddMinutes(5)), 3);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, p.ActionName, p.ProjectId, RepositoryId.New(), p.IntentFingerprint, at, at.AddMinutes(5)), 4);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, p.ActionName, p.ProjectId, p.RepositoryId, "other", at, at.AddMinutes(5)), 5);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, p.ActionName, p.ProjectId, p.RepositoryId, p.IntentFingerprint, at.AddMinutes(1), at.AddMinutes(6)), 6);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, p.ActionName, p.ProjectId, p.RepositoryId, p.IntentFingerprint, at, at.AddMinutes(6)), 7);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, p.ActionName, p.ProjectId, p.RepositoryId, p.IntentFingerprint, at, at.AddMinutes(5), at), 8);
        await CheckAsync((p, at) => new ActionApproval(ApprovalId.New(), p.OwnerPrincipalReference, p.ActionName, p.ProjectId, p.RepositoryId, p.IntentFingerprint, at, at.AddMinutes(5), null, at), 9);
    }

    private static CreateBranchProposal Proposal(ProjectId projectId, RepositoryId repositoryId, DateTimeOffset created) => new(
        CreateBranchProposalId.New(), "owner:one", projectId, repositoryId,
        new RepositoryLocator("github", "rua-den", "loren"), "feature/fix", "refs/heads/main",
        new string('a', 40), "frozen-fingerprint", created, created.AddMinutes(5));

    private static ActionApproval Approval(CreateBranchProposal proposal, ProjectId projectId, RepositoryId repositoryId, DateTimeOffset at) => new(
        ApprovalId.New(), "owner:one", proposal.ActionName, projectId, repositoryId, proposal.IntentFingerprint, at, at.AddMinutes(5));

    private static async Task SeedScope(
        CanonicalStateDbContext context,
        ProjectId projectId,
        RepositoryId repositoryId,
        DateTimeOffset now,
        string alias = "loren",
        string repositoryName = "loren")
    {
        await new SqliteProjectCatalog(context).SaveAsync(new ProjectSnapshot(
            new Project(projectId, "Loren", [alias], now, now),
            [new Repository(repositoryId, projectId, repositoryName, new RepositoryLocator("github", "rua-den", repositoryName), now, now)]),
            TestContext.Current.CancellationToken);
    }

    private static CanonicalStateDbContext Context(string connection) => new(new DbContextOptionsBuilder<CanonicalStateDbContext>().UseSqlite(connection).Options);
}
