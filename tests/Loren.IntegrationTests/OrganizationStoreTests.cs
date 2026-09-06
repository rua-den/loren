using Loren.Core.Organization;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class OrganizationStoreTests
{
    [Fact]
    public async Task NotesDecisionsAndTasksSurviveRestartAndTaskStatusTransitions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-m6a4-org-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string connectionString =
            $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset createdAt = new(2026, 9, 7, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset completedAt = createdAt.AddMinutes(10);
        DateTimeOffset reopenedAt = completedAt.AddMinutes(5);
        ProjectId projectId = ProjectId.New();
        OrganizationItemId noteId = OrganizationItemId.New();
        OrganizationItemId decisionId = OrganizationItemId.New();
        OrganizationItemId taskId = OrganizationItemId.New();

        try
        {
            await using (CanonicalStateDbContext firstContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(firstContext, cancellationToken);
                await new SqliteProjectCatalog(firstContext).SaveAsync(
                    new ProjectSnapshot(
                        new Project(
                            projectId,
                            "Loren",
                            ["loren"],
                            createdAt,
                            createdAt),
                        []),
                    cancellationToken);

                SqliteOrganizationStore store = new(firstContext);
                await store.AddAsync(
                    new OrganizationItem(
                        noteId,
                        OrganizationItemKind.Note,
                        "Architecture note",
                        "Keep the assistant conversation-first.",
                        projectId,
                        null,
                        "owner:owner",
                        createdAt,
                        createdAt),
                    cancellationToken);
                await store.AddAsync(
                    new OrganizationItem(
                        decisionId,
                        OrganizationItemKind.Decision,
                        "Product direction",
                        "Research before broad write expansion.",
                        projectId,
                        null,
                        "owner:owner",
                        createdAt,
                        createdAt),
                    cancellationToken);
                await store.AddAsync(
                    new OrganizationItem(
                        taskId,
                        OrganizationItemKind.Task,
                        "Review auth flow",
                        "Review the owner authentication flow.",
                        projectId,
                        OrganizationTaskStatus.Open,
                        "owner:owner",
                        createdAt,
                        createdAt),
                    cancellationToken);

                OrganizationItem completed = await store.SetTaskStatusAsync(
                    taskId,
                    OrganizationTaskStatus.Completed,
                    completedAt,
                    cancellationToken);
                Assert.Equal(OrganizationTaskStatus.Completed, completed.TaskStatus);
                Assert.Equal(completedAt, completed.CompletedAt);

                OrganizationItem reopened = await store.SetTaskStatusAsync(
                    taskId,
                    OrganizationTaskStatus.Open,
                    reopenedAt,
                    cancellationToken);
                Assert.Equal(OrganizationTaskStatus.Open, reopened.TaskStatus);
                Assert.Null(reopened.CompletedAt);

                string[] migrations = (await firstContext.Database
                        .GetAppliedMigrationsAsync(cancellationToken))
                    .ToArray();
                Assert.Contains("202609070001_AddOrganizationItems", migrations);
            }

            await using CanonicalStateDbContext restartedContext = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(restartedContext, cancellationToken);
            SqliteOrganizationStore restartedStore = new(restartedContext);

            IReadOnlyList<OrganizationItem> items = await restartedStore.ListAsync(
                projectId,
                limit: 20,
                cancellationToken: cancellationToken);
            Assert.Equal(3, items.Count);

            OrganizationItem task = Assert.Single(items, item => item.Id == taskId);
            Assert.Equal(OrganizationItemKind.Task, task.Kind);
            Assert.Equal(OrganizationTaskStatus.Open, task.TaskStatus);
            Assert.Equal(reopenedAt, task.UpdatedAt);
            Assert.Null(task.CompletedAt);

            OrganizationItem note = Assert.Single(items, item => item.Id == noteId);
            Assert.Equal(OrganizationItemKind.Note, note.Kind);
            Assert.Equal("Keep the assistant conversation-first.", note.Content);

            OrganizationItem decision = Assert.Single(items, item => item.Id == decisionId);
            Assert.Equal(OrganizationItemKind.Decision, decision.Kind);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ProjectScopedItemRequiresExistingCanonicalProject()
    {
        string connectionString = $"Data Source=file:org-{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using CanonicalStateDbContext context = CreateContext(connectionString);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OrganizationItem item = new(
            OrganizationItemId.New(),
            OrganizationItemKind.Note,
            null,
            "Scoped to a project that does not exist.",
            ProjectId.New(),
            null,
            "owner:owner",
            now,
            now);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SqliteOrganizationStore(context).AddAsync(
                item,
                TestContext.Current.CancellationToken));

        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
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
