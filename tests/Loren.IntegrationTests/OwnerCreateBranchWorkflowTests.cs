using System.Net;
using System.Text;
using Loren.Core.Actions;
using Loren.Core.Credentials;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Infrastructure.CanonicalState;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Loren.Web;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class OwnerCreateBranchWorkflowTests
{
    private const string Sha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task FrozenProposalApprovalUsesAtomicSqliteDecisionAndVerifiedProviderMutation()
    {
        using WorkflowFixture fixture = await WorkflowFixture.CreateAsync();
        CreateBranchProposal proposal = await fixture.AddProposalAsync();

        OwnerActionProposalDecisionResult result = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);

        Assert.Equal("approved", result.Status);
        Assert.True(result.Success, $"{result.Message} execution={result.Execution?.Error}");
        Assert.NotNull(result.Execution);
        Assert.Contains("Đã tạo và kiểm tra", result.Message, StringComparison.Ordinal);
        Assert.Equal(3, fixture.Handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, fixture.Handler.Requests[1].Method);
        Assert.Equal("/repos/rua-den/loren/git/refs", fixture.Handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Contains($"\"sha\":\"{Sha}\"", fixture.Handler.Requests[1].Body, StringComparison.Ordinal);
        Assert.Contains("\"ref\":\"refs/heads/feat/frozen\"", fixture.Handler.Requests[1].Body, StringComparison.Ordinal);
        Assert.Equal(HttpMethod.Get, fixture.Handler.Requests[2].Method);
        Assert.Contains("/git/ref/heads/feat%2Ffrozen", fixture.Handler.Requests[2].RequestUri!.AbsolutePath, StringComparison.Ordinal);

        CreateBranchProposal persisted = (await fixture.Proposals.GetAsync(proposal.Id, TestContext.Current.CancellationToken))!;
        Assert.Equal(CreateBranchProposalStatus.Approved, persisted.Status);
        Assert.NotNull(persisted.DecidedAt);
        await fixture.Context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using (System.Data.Common.DbCommand command = fixture.Context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT ConsumedAtUnixMs FROM ActionApprovals LIMIT 1";
            Assert.NotNull(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        }

        OwnerActionProposalDecisionResult replay = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);
        Assert.False(replay.Success);
        Assert.Equal(3, fixture.Handler.Requests.Count);
    }

    [Fact]
    public async Task WrongOwnerAndCancelHaveNoProviderSideEffects()
    {
        using WorkflowFixture fixture = await WorkflowFixture.CreateAsync();
        CreateBranchProposal proposal = await fixture.AddProposalAsync();

        OwnerActionProposalDecisionResult wrongOwner = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "intruder", TestContext.Current.CancellationToken);
        Assert.Equal("owner_mismatch", wrongOwner.Status);
        Assert.Empty(fixture.Handler.Requests);

        OwnerActionProposalDecisionResult cancelled = await fixture.Service
            .CancelProposalAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.True(cancelled.Success);
        Assert.Empty(fixture.Handler.Requests);
        Assert.Equal(CreateBranchProposalStatus.Cancelled,
            (await fixture.Proposals.GetAsync(proposal.Id, TestContext.Current.CancellationToken))!.Status);

        OwnerActionProposalDecisionResult replay = await fixture.Service
            .CancelProposalAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);
        Assert.Equal("alreadydecided", replay.Status);
    }

    [Fact]
    public async Task ExpiredProposalCannotCreateApprovalOrProviderCall()
    {
        using WorkflowFixture fixture = await WorkflowFixture.CreateAsync();
        CreateBranchProposal proposal = await fixture.AddProposalAsync(TimeSpan.FromMinutes(-6));

        OwnerActionProposalDecisionResult result = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);

        Assert.Equal("expired", result.Status);
        Assert.Empty(fixture.Handler.Requests);
        await fixture.Context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using (System.Data.Common.DbCommand command = fixture.Context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM ActionApprovals";
            Assert.Equal(0L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        }
        Assert.Equal(CreateBranchProposalStatus.Pending,
            (await fixture.Proposals.GetAsync(proposal.Id, TestContext.Current.CancellationToken))!.Status);
    }

    [Fact]
    public async Task CanonicalRepositoryRebindIsRejectedBeforeApprovalOrProviderCall()
    {
        using WorkflowFixture fixture = await WorkflowFixture.CreateAsync();
        CreateBranchProposal proposal = await fixture.AddProposalAsync();
        DateTimeOffset reboundAt = DateTimeOffset.UtcNow;
        ProjectSnapshot rebound = new(
            fixture.Snapshot.Project,
            [new CanonicalRepository(
                fixture.Repository.Id,
                fixture.Snapshot.Project.Id,
                fixture.Repository.Name,
                new RepositoryLocator("github", "attacker", "loren"),
                fixture.Repository.CreatedAt,
                reboundAt)]);
        await fixture.Catalog.SaveAsync(rebound, TestContext.Current.CancellationToken);

        OwnerActionProposalDecisionResult result = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);

        Assert.Equal("mismatch", result.Status);
        Assert.Empty(fixture.Handler.Requests);
        Assert.Equal(CreateBranchProposalStatus.Pending,
            (await fixture.Proposals.GetAsync(proposal.Id, TestContext.Current.CancellationToken))!.Status);
    }

    [Fact]
    public async Task CatalogSavePreservesProposalForeignKeyAcrossProjectAndRepositoryUpdates()
    {
        using WorkflowFixture fixture = await WorkflowFixture.CreateAsync();
        CreateBranchProposal proposal = await fixture.AddProposalAsync();
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow;
        ProjectSnapshot updated = new(
            new Project(
                fixture.Snapshot.Project.Id,
                "Loren renamed",
                ["loren", "loren-renamed"],
                fixture.Snapshot.Project.CreatedAt,
                updatedAt),
            [new CanonicalRepository(
                fixture.Repository.Id,
                fixture.Snapshot.Project.Id,
                "Loren updated",
                fixture.Repository.Locator,
                fixture.Repository.CreatedAt,
                updatedAt)]);

        await fixture.Catalog.SaveAsync(updated, TestContext.Current.CancellationToken);

        CreateBranchProposal? persisted = await fixture.Proposals.GetAsync(
            proposal.Id,
            TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal(CreateBranchProposalStatus.Pending, persisted.Status);
        ProjectSnapshot? loaded = await fixture.Catalog.GetAsync(
            fixture.Snapshot.Project.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal("Loren renamed", loaded!.Project.Name);
        Assert.Equal("Loren updated", Assert.Single(loaded.Repositories).Name);
    }

    [Fact]
    public async Task ProviderReadbackMismatchIsFailureAndCannotRetry()
    {
        using WorkflowFixture fixture = await WorkflowFixture.CreateAsync(verificationSha: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        CreateBranchProposal proposal = await fixture.AddProposalAsync();

        OwnerActionProposalDecisionResult result = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);

        Assert.Equal("approved", result.Status);
        Assert.False(result.Success);
        Assert.Contains("Không thể hoàn tất", result.Message, StringComparison.Ordinal);
        Assert.Equal(3, fixture.Handler.Requests.Count);
        Assert.Contains(fixture.Audit.Snapshot(), item => item.Outcome == "failed");
        OwnerActionProposalDecisionResult replay = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);
        Assert.Equal("alreadydecided", replay.Status);
        Assert.Equal(3, fixture.Handler.Requests.Count);
    }

    [Theory]
    [InlineData(CredentialResolutionStatus.Missing, "missing")]
    [InlineData(CredentialResolutionStatus.Revoked, "revoked")]
    public async Task CredentialBoundaryFailureConsumesApprovalWithoutProviderCall(CredentialResolutionStatus status, string expectedStatus)
    {
        using WorkflowFixture fixture = await WorkflowFixture.CreateAsync(credentialStatus: status);
        CreateBranchProposal proposal = await fixture.AddProposalAsync();

        OwnerActionProposalDecisionResult result = await fixture.Service
            .ApproveProposalAndCreateBranchAsync(proposal.Id.ToString(), "owner", TestContext.Current.CancellationToken);

        Assert.Equal("approved", result.Status);
        Assert.False(result.Success);
        Assert.Equal(expectedStatus, result.Execution!.Data["credential_status"]);
        Assert.Empty(fixture.Handler.Requests);
        Assert.Contains(fixture.Audit.Snapshot(), item => item.Outcome == "failed");
    }

    private sealed class WorkflowFixture : IDisposable
    {
        private readonly string _directory;
        private readonly CanonicalStateDbContext _context;
        public SqliteCreateBranchProposalStore Proposals { get; }
        public SqliteActionApprovalStore Approvals { get; }
        public CanonicalStateDbContext Context => _context;
        public SqliteProjectCatalog Catalog { get; }
        public RecordingHandler Handler { get; }
        public InMemoryAuditSink Audit { get; }
        public LorenOwnerGitHubWriteService Service { get; }
        public ProjectSnapshot Snapshot { get; }
        public CanonicalRepository Repository { get; }

        private WorkflowFixture(string directory, CanonicalStateDbContext context, SqliteProjectCatalog catalog, SqliteCreateBranchProposalStore proposals, SqliteActionApprovalStore approvals, RecordingHandler handler, InMemoryAuditSink audit, LorenOwnerGitHubWriteService service, ProjectSnapshot snapshot, CanonicalRepository repository)
        {
            _directory = directory;
            _context = context;
            Proposals = proposals;
            Approvals = approvals;
            Catalog = catalog;
            Handler = handler;
            Audit = audit;
            Service = service;
            Snapshot = snapshot;
            Repository = repository;
        }

        public static async Task<WorkflowFixture> CreateAsync(string verificationSha = Sha, CredentialResolutionStatus credentialStatus = CredentialResolutionStatus.Resolved)
        {
            string directory = Path.Combine(Path.GetTempPath(), $"loren-m6a5-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string database = Path.Combine(directory, "loren.db");
            CanonicalStateDbContext context = new(new DbContextOptionsBuilder<CanonicalStateDbContext>().UseSqlite($"Data Source={database};Pooling=False").Options);
            await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
            DateTimeOffset now = new(2026, 9, 8, 1, 0, 0, TimeSpan.Zero);
            ProjectId projectId = ProjectId.New();
            RepositoryId repositoryId = RepositoryId.New();
            CanonicalRepository repository = new(repositoryId, projectId, "Loren", new RepositoryLocator("github", "rua-den", "loren"), now, now);
            ProjectSnapshot snapshot = new(new Project(projectId, "Loren", ["loren"], now, now), [repository]);
            SqliteProjectCatalog catalog = new(context);
            await catalog.SaveAsync(snapshot, TestContext.Current.CancellationToken);
            SqliteCreateBranchProposalStore proposals = new(context);
            SqliteActionApprovalStore approvals = new(context);
            RecordingHandler handler = new(
                Json(HttpStatusCode.OK, "{\"full_name\":\"rua-den/loren\",\"default_branch\":\"main\",\"private\":false,\"archived\":false,\"open_issues_count\":0,\"pushed_at\":\"2026-01-01T00:00:00Z\",\"html_url\":\"https://github.com/rua-den/loren\"}"),
                Json(HttpStatusCode.Created, "{}"),
                Json(HttpStatusCode.OK, $"{{\"object\":{{\"sha\":\"{verificationSha}\"}}}}"));
            InMemoryAuditSink audit = new();
            ActionGateway gateway = new([GitHubActions.CreateBranch], [new GitHubCreateBranchActionExecutor(new FixedCredentials(credentialStatus), new GitHubCreateBranchClient(new HttpClient(handler)))], new GateDActionPolicy(new FixedWriteSafetyState(false)), audit, approvals);
            LorenOwnerGitHubWriteService service = new(catalog, approvals, gateway, audit, proposals);
            return new(directory, context, catalog, proposals, approvals, handler, audit, service, snapshot, repository);
        }

        public async Task<CreateBranchProposal> AddProposalAsync(TimeSpan? age = null)
        {
            DateTimeOffset created = DateTimeOffset.UtcNow.Add(age ?? TimeSpan.Zero);
            Dictionary<string, string> target = new(StringComparer.Ordinal) { [GitHubCreateBranchActionExecutor.BranchTargetKey] = "feat/frozen", [GitHubCreateBranchActionExecutor.SourceShaTargetKey] = Sha };
            ActionRequest request = new(GitHubActions.CreateBranch.Name, target);
            ActionAuthorizationContext authorization = new(Snapshot.Project.Id, Repository.Id, Repository.Locator, "owner", target);
            CreateBranchProposal proposal = new(CreateBranchProposalId.New(), "owner", Snapshot.Project.Id, Repository.Id, Repository.Locator, "feat/frozen", "refs/heads/main", Sha, ActionIntentFingerprint.Compute(GitHubActions.CreateBranch, request, authorization), created, created.AddMinutes(5));
            await Proposals.AddAsync(proposal, TestContext.Current.CancellationToken);
            return proposal;
        }

        public void Dispose()
        {
            _context.Dispose();
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }

    private sealed class FixedCredentials(CredentialResolutionStatus status) : IActionCredentialResolver
    {
        public Task<CredentialResolution> ResolveAsync(CredentialResolutionRequest request, CancellationToken cancellationToken = default) => Task.FromResult(status switch
        {
            CredentialResolutionStatus.Missing => CredentialResolution.Missing(request),
            CredentialResolutionStatus.Revoked => CredentialResolution.Revoked(request),
            _ => CredentialResolution.Resolved(new CredentialLease(request.Purpose, request.Reference, "write-secret")),
        });
    }

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<RecordedRequest> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
            return _responses.Dequeue();
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri? RequestUri, string Body);
}
