using System.Net;
using System.Text;
using Loren.Core.Actions;
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

public sealed class ConversationalCreateBranchProposalTests
{
    private const string Sha = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData("ambiguous")]
    [InlineData("foreign")]
    [InlineData("non-github")]
    [InlineData("archived")]
    [InlineData("unsafe-branch")]
    [InlineData("unsafe-source")]
    [InlineData("read-failure")]
    [InlineData("missing-owner")]
    public async Task RejectionMatrixReturnsSafeFailureWithoutPersistingProposal(string scenario)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-proposal-reject-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await using CanonicalStateDbContext context = new(new DbContextOptionsBuilder<CanonicalStateDbContext>().UseSqlite($"Data Source={Path.Combine(directory, "loren.db")};Pooling=False").Options);
            await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ProjectId projectId = ProjectId.New();
            RepositoryId primaryId = RepositoryId.New();
            string provider = scenario == "non-github" ? "gitlab" : "github";
            CanonicalRepository primary = new(primaryId, projectId, "Loren", new RepositoryLocator(provider, "rua-den", "loren"), now, now);
            List<CanonicalRepository> repositories = [primary];
            RepositoryId foreignId = RepositoryId.New();
            if (scenario == "ambiguous") repositories.Add(new CanonicalRepository(RepositoryId.New(), projectId, "Other", new RepositoryLocator("github", "rua-den", "other"), now, now));
            ProjectSnapshot snapshot = new(new Project(projectId, "Loren", ["loren"], now, now), repositories);
            SqliteProjectCatalog catalog = new(context);
            await catalog.SaveAsync(snapshot, TestContext.Current.CancellationToken);
            if (scenario == "foreign")
            {
                ProjectId otherProjectId = ProjectId.New();
                await catalog.SaveAsync(new ProjectSnapshot(new Project(otherProjectId, "Other", ["other"], now, now), [new CanonicalRepository(foreignId, otherProjectId, "Other", new RepositoryLocator("github", "other", "repo"), now, now)]), TestContext.Current.CancellationToken);
            }
            SqliteCreateBranchProposalStore store = new(context);
            CurrentRunProposalCollector collector = new();
            MatrixReadHandler handler = new(scenario);
            GitHubCreateBranchProposalExecutor proposalExecutor = new(catalog, new GitHubRepositoryReadClient(new HttpClient(handler)), store, collector);
            ActionGateway gateway = new([GitHubActions.ProposeCreateBranch], [proposalExecutor], new GateDActionPolicy(new FixedWriteSafetyState(false)), new InMemoryAuditSink());
            Dictionary<string, string> arguments = new() { ["branch"] = scenario == "unsafe-branch" ? "bad..branch" : "fix-login" };
            if (scenario == "unsafe-source") arguments["source_ref"] = "refs/tags/v1";
            if (scenario == "foreign" || scenario == "ambiguous") arguments["repository_id"] = scenario == "foreign" ? foreignId.ToString() : string.Empty;
            AuthenticatedOwnerContext? owner = scenario == "missing-owner" ? null : new AuthenticatedOwnerContext("owner", projectId);
            ActionResult result = await gateway.ExecuteAsync(new ActionExecutionRequest(RunId.New(), ActionId.New(), new ActionRequest(GitHubActions.ProposeCreateBranch.Name, arguments), OwnerContext: owner), TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Empty(collector.Drain());
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            await using System.Data.Common.DbCommand command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM CreateBranchProposals";
            Assert.Equal(0L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
            Assert.DoesNotContain(handler.Requests, request => request.Method == HttpMethod.Post);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DefaultAndExplicitSourceRefsPersistAuthoritativeOrderedProposalsWithoutWriteDependencies()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-proposal-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string database = Path.Combine(directory, "loren.db");
            await using CanonicalStateDbContext context = new(new DbContextOptionsBuilder<CanonicalStateDbContext>().UseSqlite($"Data Source={database};Pooling=False").Options);
            await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ProjectId projectId = ProjectId.New();
            RepositoryId repositoryId = RepositoryId.New();
            CanonicalRepository repository = new(repositoryId, projectId, "Loren", new RepositoryLocator("github", "rua-den", "loren"), now, now);
            ProjectSnapshot snapshot = new(new Project(projectId, "Loren", ["loren"], now, now), [repository]);
            SqliteProjectCatalog catalog = new(context);
            await catalog.SaveAsync(snapshot, TestContext.Current.CancellationToken);
            SqliteCreateBranchProposalStore store = new(context);
            CurrentRunProposalCollector collector = new();
            FakeReadHandler handler = new();
            GitHubCreateBranchProposalExecutor executor = new(catalog, new GitHubRepositoryReadClient(new HttpClient(handler)), store, collector);
            collector.Start();
            AuthenticatedOwnerContext owner = new("owner", projectId);

            ActionResult first = await executor.ExecuteTrustedAsync(
                new ActionExecutionRequest(RunId.New(), ActionId.New(), new ActionRequest(GitHubActions.ProposeCreateBranch.Name, new Dictionary<string, string>
                {
                    ["branch"] = "fix-login",
                    ["source_sha"] = "fake-sha-from-model",
                    ["approval_id"] = "fake-approval",
                    ["credential"] = "fake-secret",
                    ["source_ref"] = "",
                }), OwnerContext: owner), TestContext.Current.CancellationToken);
            ActionResult second = await executor.ExecuteTrustedAsync(
                new ActionExecutionRequest(RunId.New(), ActionId.New(), new ActionRequest(GitHubActions.ProposeCreateBranch.Name, new Dictionary<string, string>
                {
                    ["branch"] = "fix-login-2",
                    ["source_ref"] = "refs/heads/release",
                }), OwnerContext: owner), TestContext.Current.CancellationToken);

            Assert.True(first.Success);
            Assert.True(second.Success);
            IReadOnlyList<CreateBranchProposalId> ids = collector.Drain();
            Assert.Equal(2, ids.Count);
            CreateBranchProposal savedFirst = (await store.GetAsync(ids[0], TestContext.Current.CancellationToken))!;
            CreateBranchProposal savedSecond = (await store.GetAsync(ids[1], TestContext.Current.CancellationToken))!;
            Assert.Equal("refs/heads/main", savedFirst.SourceRef);
            Assert.Equal("refs/heads/release", savedSecond.SourceRef);
            Assert.Equal(Sha, savedFirst.SourceSha);
            Assert.Equal(now.Date, savedFirst.CreatedAt.Date);
            Assert.Equal(savedFirst.CreatedAt.AddMinutes(5), savedFirst.ExpiresAt);
            Assert.DoesNotContain("fake", savedFirst.SourceSha, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", string.Join('|', first.Data.Values), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(4, handler.RequestCount);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task MissingOwnerProjectAndDefaultBranchAreRejectedBeforePersistence()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-proposal-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string database = Path.Combine(directory, "loren.db");
            await using CanonicalStateDbContext context = new(new DbContextOptionsBuilder<CanonicalStateDbContext>().UseSqlite($"Data Source={database};Pooling=False").Options);
            await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
            SqliteCreateBranchProposalStore store = new(context);
            GitHubCreateBranchProposalExecutor executor = new(new SqliteProjectCatalog(context), new GitHubRepositoryReadClient(new HttpClient(new FakeReadHandler())), store, new CurrentRunProposalCollector());
            ActionResult missingProject = await executor.ExecuteTrustedAsync(new ActionExecutionRequest(RunId.New(), ActionId.New(), new ActionRequest(GitHubActions.ProposeCreateBranch.Name, new Dictionary<string, string> { ["branch"] = "fix" }), OwnerContext: new AuthenticatedOwnerContext("owner")), TestContext.Current.CancellationToken);
            Assert.False(missingProject.Success);
            Assert.Contains("project", missingProject.Error, StringComparison.OrdinalIgnoreCase);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            ProjectId projectId = ProjectId.New();
            RepositoryId repositoryId = RepositoryId.New();
            CanonicalRepository repository = new(repositoryId, projectId, "Loren", new RepositoryLocator("github", "rua-den", "loren"), now, now);
            await new SqliteProjectCatalog(context).SaveAsync(new ProjectSnapshot(new Project(projectId, "Loren", ["loren"], now, now), [repository]), TestContext.Current.CancellationToken);
            CurrentRunProposalCollector collector = new();
            GitHubCreateBranchProposalExecutor branchExecutor = new(new SqliteProjectCatalog(context), new GitHubRepositoryReadClient(new HttpClient(new FakeReadHandler())), store, collector);
            ActionResult sameAsDefault = await branchExecutor.ExecuteTrustedAsync(new ActionExecutionRequest(RunId.New(), ActionId.New(), new ActionRequest(GitHubActions.ProposeCreateBranch.Name, new Dictionary<string, string> { ["branch"] = "main", ["source_ref"] = "refs/heads/release" }), OwnerContext: new AuthenticatedOwnerContext("owner", projectId)), TestContext.Current.CancellationToken);
            Assert.False(sameAsDefault.Success);
            Assert.Contains("default", sameAsDefault.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(collector.Drain());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FakeReadHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            string body = request.RequestUri!.AbsolutePath.EndsWith("/git/ref/heads/main", StringComparison.Ordinal)
                || request.RequestUri.AbsolutePath.EndsWith("/git/ref/heads/release", StringComparison.Ordinal)
                ? $"{{\"ref\":\"refs/heads/{(request.RequestUri.AbsolutePath.EndsWith("release", StringComparison.Ordinal) ? "release" : "main")}\",\"object\":{{\"type\":\"commit\",\"sha\":\"{Sha}\"}}}}"
                : "{\"full_name\":\"rua-den/loren\",\"default_branch\":\"main\",\"private\":false,\"archived\":false,\"open_issues_count\":0,\"pushed_at\":\"2026-01-01T00:00:00Z\",\"html_url\":\"https://github.com/rua-den/loren\"}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class MatrixReadHandler(string scenario) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (scenario == "read-failure")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            if (scenario == "archived")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"full_name\":\"rua-den/loren\",\"default_branch\":\"main\",\"private\":false,\"archived\":true,\"open_issues_count\":0,\"pushed_at\":\"2026-01-01T00:00:00Z\",\"html_url\":\"https://github.com/rua-den/loren\"}", Encoding.UTF8, "application/json") });
            bool release = request.RequestUri!.AbsolutePath.EndsWith("release", StringComparison.Ordinal);
            string body = request.RequestUri.AbsolutePath.Contains("/git/ref/heads/", StringComparison.Ordinal)
                ? $"{{\"ref\":\"refs/heads/{(release ? "release" : "main")}\",\"object\":{{\"type\":\"commit\",\"sha\":\"{Sha}\"}}}}"
                : "{\"full_name\":\"rua-den/loren\",\"default_branch\":\"main\",\"private\":false,\"archived\":false,\"open_issues_count\":0,\"pushed_at\":\"2026-01-01T00:00:00Z\",\"html_url\":\"https://github.com/rua-den/loren\"}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
