using Loren.Core.Memories;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class TrustedMemoryStoreTests
{
    [Fact]
    public async Task OwnerExplicitMemorySurvivesRestartWithAuthorityScopeAndCanonicalId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-m4-memory-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";

        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        MemoryRecordId memoryRecordId = MemoryRecordId.New();
        DateTimeOffset now = new(2026, 9, 4, 9, 55, 0, TimeSpan.Zero);

        ProjectSnapshot projectSnapshot = new(
            new Project(
                projectId,
                "Wedding Online",
                ["wedding project", "web đám cưới", "wedding-online"],
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

        MemoryRecord memory = new(
            memoryRecordId,
            MemorySourceClass.OwnerExplicit,
            "Production deploy requires owner approval.",
            projectId,
            repositoryId,
            "owner:authenticated",
            null,
            now,
            now);

        try
        {
            await using (CanonicalStateDbContext firstContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(firstContext, cancellationToken);
                await new SqliteProjectCatalog(firstContext)
                    .SaveAsync(projectSnapshot, cancellationToken);
                await new SqliteMemoryStore(firstContext)
                    .AddAsync(memory, cancellationToken);

                string[] appliedMigrations = (await firstContext.Database
                        .GetAppliedMigrationsAsync(cancellationToken))
                    .ToArray();

                Assert.Contains("202609040001_InitialCanonicalState", appliedMigrations);
                Assert.Contains("202609040002_AddMemoryRecords", appliedMigrations);
            }

            await using CanonicalStateDbContext restartedContext = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(restartedContext, cancellationToken);
            SqliteMemoryStore restartedStore = new(restartedContext);

            MemoryRecord? loaded = await restartedStore.GetAsync(
                memoryRecordId,
                cancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal(memoryRecordId, loaded.Id);
            Assert.Equal(MemorySourceClass.OwnerExplicit, loaded.SourceClass);
            Assert.Equal("Production deploy requires owner approval.", loaded.Content);
            Assert.Equal(projectId, loaded.ProjectId);
            Assert.Equal(repositoryId, loaded.RepositoryId);
            Assert.Equal("owner:authenticated", loaded.SourceReference);
            Assert.Null(loaded.SupersededById);
            Assert.True(loaded.IsCurrent);
            Assert.Equal(now, loaded.CreatedAt);
            Assert.Equal(now, loaded.UpdatedAt);

            IReadOnlyList<MemoryRecord> current = await restartedStore
                .ListCurrentForProjectAsync(projectId, cancellationToken);

            MemoryRecord currentMemory = Assert.Single(current);
            Assert.Equal(memoryRecordId, currentMemory.Id);
            Assert.Equal(MemorySourceClass.OwnerExplicit, currentMemory.SourceClass);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RepositoryScopeMustBelongToProjectScope()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-m4-memory-scope-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset now = new(2026, 9, 4, 9, 55, 0, TimeSpan.Zero);

        ProjectId firstProjectId = ProjectId.New();
        ProjectId secondProjectId = ProjectId.New();
        RepositoryId secondRepositoryId = RepositoryId.New();

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            SqliteProjectCatalog catalog = new(context);

            await catalog.SaveAsync(
                new ProjectSnapshot(
                    new Project(
                        firstProjectId,
                        "First Project",
                        ["first-project"],
                        now,
                        now),
                    []),
                cancellationToken);

            await catalog.SaveAsync(
                new ProjectSnapshot(
                    new Project(
                        secondProjectId,
                        "Second Project",
                        ["second-project"],
                        now,
                        now),
                    [
                        new CanonicalRepository(
                            secondRepositoryId,
                            secondProjectId,
                            "Second Repository",
                            new RepositoryLocator("github", "rua-den", "second-project"),
                            now,
                            now),
                    ]),
                cancellationToken);

            MemoryRecord invalidScope = new(
                MemoryRecordId.New(),
                MemorySourceClass.OwnerExplicit,
                "This record intentionally has a mismatched repository scope.",
                firstProjectId,
                secondRepositoryId,
                "owner:authenticated",
                null,
                now,
                now);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new SqliteMemoryStore(context).AddAsync(invalidScope, cancellationToken));

            Assert.Contains("does not belong", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static CanonicalStateDbContext CreateContext(string connectionString)
    {
        DbContextOptions<CanonicalStateDbContext> options =
            new DbContextOptionsBuilder<CanonicalStateDbContext>()
                .UseSqlite(connectionString)
                .Options;

        return new CanonicalStateDbContext(options);
    }
}
