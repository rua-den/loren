using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Infrastructure.CanonicalState;
using Loren.Runtime;
using Loren.Web;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class CanonicalProjectContextTests
{
    [Fact]
    public async Task ConfiguredAliasesPrepareSameCanonicalContextAfterRestart()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory();
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")}";
        ProjectSnapshot snapshot = CreateWeddingSnapshot();

        try
        {
            await using (CanonicalStateDbContext firstContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(firstContext, cancellationToken);
                SqliteProjectCatalog firstCatalog = new(firstContext);
                await firstCatalog.SaveAsync(snapshot, cancellationToken);
            }

            await using CanonicalStateDbContext restartedContext = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(restartedContext, cancellationToken);
            LorenProjectContextBuilder builder = new(new SqliteProjectCatalog(restartedContext));

            foreach (string projectAlias in new[]
                     {
                         "wedding project",
                         "web đám cưới",
                         "wedding-online",
                     })
            {
                PreparedLorenContext prepared = await builder.BuildAsync(
                    "Which repository belongs to this project?",
                    projectAlias,
                    cancellationToken);

                Assert.NotNull(prepared.Project);
                Assert.Equal(snapshot.Project.Id.ToString(), prepared.Project.ProjectId);
                LorenRepositoryContext repository = Assert.Single(prepared.Project.Repositories);
                Assert.Equal("github", repository.Provider);
                Assert.Equal("rua-den/wedding-online", repository.ExternalFullName);
            }
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunServicePassesPreparedContextToBrainAndUnknownAliasFailsBeforeBrain()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory();
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")}";
        ProjectSnapshot snapshot = CreateWeddingSnapshot();

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            SqliteProjectCatalog catalog = new(context);
            await catalog.SaveAsync(snapshot, cancellationToken);

            CapturingBrain brain = new(snapshot.Project.Id.ToString());
            InMemoryAuditSink audit = new();
            ActionGateway gateway = new(
                [],
                [],
                new ReadOnlyActionPolicy(),
                audit);
            AgentLoop loop = new(brain, gateway, new AgentLoopOptions());
            LorenProjectContextBuilder contextBuilder = new(catalog);
            LorenRunService runService = new(loop, audit, contextBuilder);

            LorenRunResult result = await runService.RunAsync(
                "Which repository belongs to this project?",
                "web đám cưới",
                cancellationToken);

            Assert.Equal("Prepared canonical context received.", result.FinalOutput);
            Assert.Equal(1, brain.CallCount);
            Assert.NotNull(result.Project);
            Assert.Equal(snapshot.Project.Id.ToString(), result.Project.ProjectId);
            Assert.Equal("rua-den/wedding-online", Assert.Single(result.Project.Repositories).ExternalFullName);

            await Assert.ThrowsAsync<UnknownProjectAliasException>(
                () => runService.RunAsync(
                    "Do not call the model for an unknown project.",
                    "not configured",
                    cancellationToken));
            Assert.Equal(1, brain.CallCount);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static ProjectSnapshot CreateWeddingSnapshot()
    {
        ProjectId projectId = new(Guid.Parse("7a67fc4e-39e2-4bb4-bf76-3a2e1e5e6ac3"));
        RepositoryId repositoryId = new(Guid.Parse("a92682e9-7655-40d6-b164-0556220113a2"));
        DateTimeOffset now = new(2026, 9, 4, 6, 0, 0, TimeSpan.Zero);

        Project project = new(
            projectId,
            "Wedding Online",
            ["wedding project", "web đám cưới", "wedding-online"],
            now,
            now);
        CanonicalRepository repository = new(
            repositoryId,
            projectId,
            "Wedding Online GitHub",
            new RepositoryLocator("github", "rua-den", "wedding-online"),
            now,
            now);

        return new ProjectSnapshot(project, [repository]);
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"loren-m3-context-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static CanonicalStateDbContext CreateContext(string connectionString)
    {
        DbContextOptions<CanonicalStateDbContext> options =
            new DbContextOptionsBuilder<CanonicalStateDbContext>()
                .UseSqlite(connectionString)
                .Options;
        return new CanonicalStateDbContext(options);
    }

    private sealed class CapturingBrain(string expectedProjectId) : IBrain
    {
        public int CallCount { get; private set; }

        public Task<BrainTurnResult> ThinkAsync(
            BrainContext context,
            IReadOnlyList<ActionDefinition> availableActions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;

            Assert.Equal(2, context.Inputs.Count);
            BrainMessage systemMessage = Assert.IsType<BrainMessage>(context.Inputs[0]);
            Assert.Equal(BrainRole.System, systemMessage.Role);
            Assert.Contains(expectedProjectId, systemMessage.Content, StringComparison.Ordinal);
            Assert.Contains("rua-den/wedding-online", systemMessage.Content, StringComparison.Ordinal);
            Assert.Contains("not live external state", systemMessage.Content, StringComparison.Ordinal);

            BrainMessage userMessage = Assert.IsType<BrainMessage>(context.Inputs[1]);
            Assert.Equal(BrainRole.User, userMessage.Role);
            Assert.Equal("Which repository belongs to this project?", userMessage.Content);
            Assert.Contains(availableActions, action => action.Name == "github.read_repository");

            return Task.FromResult(
                BrainTurnResult.Final("Prepared canonical context received."));
        }
    }
}
