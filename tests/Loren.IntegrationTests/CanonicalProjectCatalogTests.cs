using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class CanonicalProjectCatalogTests
{
    [Fact]
    public async Task ProjectAndRepositorySurviveRestartAndResolveAllAliases()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-m3-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string databasePath = Path.Combine(tempDirectory, "loren.db");
        string connectionString = $"Data Source={databasePath}";

        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        DateTimeOffset now = new(2026, 9, 4, 5, 30, 0, TimeSpan.Zero);

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

        ProjectSnapshot snapshot = new(project, [repository]);

        try
        {
            await using (CanonicalStateDbContext firstContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(firstContext, cancellationToken);
                SqliteProjectCatalog firstCatalog = new(firstContext);
                await firstCatalog.SaveAsync(snapshot, cancellationToken);

                string[] appliedMigrations = (await firstContext.Database
                        .GetAppliedMigrationsAsync(cancellationToken))
                    .ToArray();

                Assert.Contains("202609040001_InitialCanonicalState", appliedMigrations);
            }

            await using CanonicalStateDbContext restartedContext = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(restartedContext, cancellationToken);
            SqliteProjectCatalog restartedCatalog = new(restartedContext);

            ProjectSnapshot? byWeddingProject = await restartedCatalog.FindByAliasAsync(
                "  Wedding   Project  ",
                cancellationToken);
            ProjectSnapshot? byVietnameseAlias = await restartedCatalog.FindByAliasAsync(
                "WEB ĐÁM CƯỚI",
                cancellationToken);
            ProjectSnapshot? byRepositoryAlias = await restartedCatalog.FindByAliasAsync(
                "wedding-online",
                cancellationToken);

            Assert.NotNull(byWeddingProject);
            Assert.NotNull(byVietnameseAlias);
            Assert.NotNull(byRepositoryAlias);

            Assert.Equal(projectId, byWeddingProject.Project.Id);
            Assert.Equal(projectId, byVietnameseAlias.Project.Id);
            Assert.Equal(projectId, byRepositoryAlias.Project.Id);
            Assert.Equal("Wedding Online", byRepositoryAlias.Project.Name);

            CanonicalRepository persistedRepository = Assert.Single(
                byRepositoryAlias.Repositories);
            Assert.Equal(repositoryId, persistedRepository.Id);
            Assert.Equal(projectId, persistedRepository.ProjectId);
            Assert.Equal("github", persistedRepository.Locator.Provider);
            Assert.Equal("rua-den/wedding-online", persistedRepository.Locator.FullName);

            Assert.Null(
                await restartedCatalog.FindByAliasAsync(
                    "unknown project",
                    cancellationToken));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AliasCannotSilentlyBelongToTwoProjects()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-m3-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string databasePath = Path.Combine(tempDirectory, "loren.db");
        string connectionString = $"Data Source={databasePath}";
        DateTimeOffset now = new(2026, 9, 4, 5, 30, 0, TimeSpan.Zero);

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            SqliteProjectCatalog catalog = new(context);

            ProjectId firstProjectId = ProjectId.New();
            await catalog.SaveAsync(
                new ProjectSnapshot(
                    new Project(
                        firstProjectId,
                        "First",
                        ["shared alias"],
                        now,
                        now),
                    []),
                cancellationToken);

            ProjectId secondProjectId = ProjectId.New();
            ProjectSnapshot conflictingSnapshot = new(
                new Project(
                    secondProjectId,
                    "Second",
                    ["SHARED   ALIAS"],
                    now,
                    now),
                []);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => catalog.SaveAsync(conflictingSnapshot, cancellationToken));
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
