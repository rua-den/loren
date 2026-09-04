using Loren.Core.Memories;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Loren.Web;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class TrustedMemoryForgetTests
{
    [Fact]
    public async Task ForgettingCurrentCorrectionPurgesWholeChainAndStaysForgottenAfterRestart()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory();
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset now = new(2026, 9, 4, 11, 0, 0, TimeSpan.Zero);
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        MemoryRecordId originalId = MemoryRecordId.New();
        MemoryRecordId firstCorrectionId = MemoryRecordId.New();
        MemoryRecordId currentCorrectionId = MemoryRecordId.New();
        MemoryRecordId unrelatedId = MemoryRecordId.New();

        try
        {
            await using (CanonicalStateDbContext firstContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(firstContext, cancellationToken);
                SqliteProjectCatalog catalog = new(firstContext);
                await catalog.SaveAsync(
                    CreateProject(projectId, repositoryId, now),
                    cancellationToken);

                SqliteMemoryStore store = new(firstContext);
                await store.AddAsync(
                    Memory(
                        originalId,
                        MemorySourceClass.OwnerExplicit,
                        "FORGOTTEN_CHAIN_V1 deploy can run automatically.",
                        projectId,
                        repositoryId,
                        "owner:authenticated",
                        now),
                    cancellationToken);

                await store.CorrectAsync(
                    originalId,
                    Memory(
                        firstCorrectionId,
                        MemorySourceClass.OwnerCorrection,
                        "FORGOTTEN_CHAIN_V2 deploy needs review.",
                        projectId,
                        repositoryId,
                        "owner:authenticated",
                        now.AddMinutes(1)),
                    cancellationToken);

                await store.CorrectAsync(
                    firstCorrectionId,
                    Memory(
                        currentCorrectionId,
                        MemorySourceClass.OwnerCorrection,
                        "FORGOTTEN_CHAIN_CURRENT deploy needs explicit owner approval.",
                        projectId,
                        repositoryId,
                        "owner:authenticated",
                        now.AddMinutes(2)),
                    cancellationToken);

                await store.AddAsync(
                    Memory(
                        unrelatedId,
                        MemorySourceClass.OwnerExplicit,
                        "UNRELATED_MEMORY wedding-online is the wedding site.",
                        projectId,
                        repositoryId,
                        "owner:authenticated",
                        now.AddMinutes(3)),
                    cancellationToken);

                await store.ForgetAsync(currentCorrectionId, cancellationToken);

                Assert.Null(await store.GetAsync(originalId, cancellationToken));
                Assert.Null(await store.GetAsync(firstCorrectionId, cancellationToken));
                Assert.Null(await store.GetAsync(currentCorrectionId, cancellationToken));

                MemoryRecord current = Assert.Single(
                    await store.ListCurrentForProjectAsync(projectId, cancellationToken));
                Assert.Equal(unrelatedId, current.Id);
            }

            await using CanonicalStateDbContext restartedContext = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(restartedContext, cancellationToken);
            SqliteMemoryStore restartedStore = new(restartedContext);

            Assert.Null(await restartedStore.GetAsync(originalId, cancellationToken));
            Assert.Null(await restartedStore.GetAsync(firstCorrectionId, cancellationToken));
            Assert.Null(await restartedStore.GetAsync(currentCorrectionId, cancellationToken));

            MemoryRecord surviving = Assert.Single(
                await restartedStore.ListCurrentForProjectAsync(projectId, cancellationToken));
            Assert.Equal(unrelatedId, surviving.Id);

            LorenMemoryContextBuilder memoryBuilder = new(
                restartedStore,
                new LorenMemoryContextOptions());
            PreparedMemoryContext prepared = await memoryBuilder.BuildAsync(
                projectId,
                cancellationToken);

            LorenMemoryContext included = Assert.Single(prepared.Included);
            Assert.Contains("UNRELATED_MEMORY", included.Content, StringComparison.Ordinal);
            Assert.DoesNotContain("FORGOTTEN_CHAIN", prepared.SystemContext, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ForgettingSupersededRecordFailsWithoutDeletingHistoryOrCurrentTruth()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory();
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset now = new(2026, 9, 4, 11, 10, 0, TimeSpan.Zero);
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        MemoryRecordId originalId = MemoryRecordId.New();
        MemoryRecordId correctionId = MemoryRecordId.New();

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            SqliteProjectCatalog catalog = new(context);
            await catalog.SaveAsync(
                CreateProject(projectId, repositoryId, now),
                cancellationToken);

            SqliteMemoryStore store = new(context);
            await store.AddAsync(
                Memory(
                    originalId,
                    MemorySourceClass.OwnerExplicit,
                    "OLD_MEMORY",
                    projectId,
                    repositoryId,
                    "owner:authenticated",
                    now),
                cancellationToken);
            await store.CorrectAsync(
                originalId,
                Memory(
                    correctionId,
                    MemorySourceClass.OwnerCorrection,
                    "CURRENT_MEMORY",
                    projectId,
                    repositoryId,
                    "owner:authenticated",
                    now.AddMinutes(1)),
                cancellationToken);

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.ForgetAsync(originalId, cancellationToken));
            Assert.Contains("not current", error.Message, StringComparison.OrdinalIgnoreCase);

            MemoryRecord old = Assert.IsType<MemoryRecord>(
                await store.GetAsync(originalId, cancellationToken));
            MemoryRecord current = Assert.IsType<MemoryRecord>(
                await store.GetAsync(correctionId, cancellationToken));
            Assert.Equal(correctionId, old.SupersededById);
            Assert.Null(current.SupersededById);
            Assert.Equal(correctionId, Assert.Single(
                await store.ListCurrentForProjectAsync(projectId, cancellationToken)).Id);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ForgettingUnknownMemoryFailsClosed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory();
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            SqliteMemoryStore store = new(context);

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.ForgetAsync(MemoryRecordId.New(), cancellationToken));
            Assert.Contains("does not exist", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static ProjectSnapshot CreateProject(
        ProjectId projectId,
        RepositoryId repositoryId,
        DateTimeOffset now) => new(
        new Project(
            projectId,
            "Wedding Online",
            ["wedding-online"],
            now,
            now),
        [
            new CanonicalRepository(
                repositoryId,
                projectId,
                "Wedding Online GitHub",
                new RepositoryLocator("github", "rua-den", "wedding-online"),
                now,
                now),
        ]);

    private static MemoryRecord Memory(
        MemoryRecordId id,
        MemorySourceClass sourceClass,
        string content,
        ProjectId projectId,
        RepositoryId repositoryId,
        string sourceReference,
        DateTimeOffset timestamp) => new(
        id,
        sourceClass,
        content,
        projectId,
        repositoryId,
        sourceReference,
        null,
        timestamp,
        timestamp);

    private static CanonicalStateDbContext CreateContext(string connectionString)
    {
        DbContextOptions<CanonicalStateDbContext> options =
            new DbContextOptionsBuilder<CanonicalStateDbContext>()
                .UseSqlite(connectionString)
                .Options;
        return new CanonicalStateDbContext(options);
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"loren-m4-forget-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
