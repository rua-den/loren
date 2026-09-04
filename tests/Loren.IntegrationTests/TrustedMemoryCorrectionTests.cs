using Loren.Core.Memories;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class TrustedMemoryCorrectionTests
{
    [Fact]
    public async Task OwnerCorrectionSupersedesOldMemoryAndSurvivesRestart()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory("loren-m4-correction");
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset originalTime = new(2026, 9, 4, 10, 5, 0, TimeSpan.Zero);
        DateTimeOffset correctionTime = originalTime.AddMinutes(5);
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        MemoryRecordId originalId = MemoryRecordId.New();
        MemoryRecordId correctionId = MemoryRecordId.New();

        try
        {
            await using (CanonicalStateDbContext firstContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(firstContext, cancellationToken);
                await SeedProjectAsync(
                    firstContext,
                    projectId,
                    repositoryId,
                    originalTime,
                    cancellationToken);

                SqliteMemoryStore store = new(firstContext);
                await store.AddAsync(
                    new MemoryRecord(
                        originalId,
                        MemorySourceClass.OwnerExplicit,
                        "Production deploy does not require approval.",
                        projectId,
                        repositoryId,
                        "owner:authenticated",
                        null,
                        originalTime,
                        originalTime),
                    cancellationToken);

                await store.CorrectAsync(
                    originalId,
                    new MemoryRecord(
                        correctionId,
                        MemorySourceClass.OwnerCorrection,
                        "Production deploy requires owner approval.",
                        projectId,
                        repositoryId,
                        "owner:authenticated",
                        null,
                        correctionTime,
                        correctionTime),
                    cancellationToken);

                MemoryRecord? original = await store.GetAsync(originalId, cancellationToken);
                MemoryRecord? correction = await store.GetAsync(correctionId, cancellationToken);
                IReadOnlyList<MemoryRecord> current = await store.ListCurrentForProjectAsync(
                    projectId,
                    cancellationToken);

                Assert.NotNull(original);
                Assert.Equal("Production deploy does not require approval.", original.Content);
                Assert.Equal(correctionId, original.SupersededById);
                Assert.False(original.IsCurrent);
                Assert.Equal(correctionTime, original.UpdatedAt);

                Assert.NotNull(correction);
                Assert.Equal(MemorySourceClass.OwnerCorrection, correction.SourceClass);
                Assert.Equal("Production deploy requires owner approval.", correction.Content);
                Assert.True(correction.IsCurrent);

                MemoryRecord currentMemory = Assert.Single(current);
                Assert.Equal(correctionId, currentMemory.Id);
            }

            await using CanonicalStateDbContext restartedContext = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(restartedContext, cancellationToken);
            SqliteMemoryStore restartedStore = new(restartedContext);

            MemoryRecord? restartedOriginal = await restartedStore.GetAsync(
                originalId,
                cancellationToken);
            MemoryRecord? restartedCorrection = await restartedStore.GetAsync(
                correctionId,
                cancellationToken);
            IReadOnlyList<MemoryRecord> restartedCurrent = await restartedStore
                .ListCurrentForProjectAsync(projectId, cancellationToken);

            Assert.NotNull(restartedOriginal);
            Assert.Equal(correctionId, restartedOriginal.SupersededById);
            Assert.Equal("Production deploy does not require approval.", restartedOriginal.Content);

            Assert.NotNull(restartedCorrection);
            Assert.Equal(MemorySourceClass.OwnerCorrection, restartedCorrection.SourceClass);
            Assert.Equal("owner:authenticated", restartedCorrection.SourceReference);
            Assert.True(restartedCorrection.IsCurrent);

            MemoryRecord currentAfterRestart = Assert.Single(restartedCurrent);
            Assert.Equal(correctionId, currentAfterRestart.Id);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CorrectionRejectsNonOwnerCorrectionAuthorityWithoutMutation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory("loren-m4-correction-authority");
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset now = new(2026, 9, 4, 10, 5, 0, TimeSpan.Zero);
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        MemoryRecordId originalId = MemoryRecordId.New();
        MemoryRecordId invalidReplacementId = MemoryRecordId.New();

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            await SeedProjectAsync(context, projectId, repositoryId, now, cancellationToken);
            SqliteMemoryStore store = new(context);
            await store.AddAsync(
                OwnerMemory(originalId, projectId, repositoryId, now),
                cancellationToken);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.CorrectAsync(
                    originalId,
                    new MemoryRecord(
                        invalidReplacementId,
                        MemorySourceClass.ModelInference,
                        "Model guesses that approval is not required.",
                        projectId,
                        repositoryId,
                        "model:test",
                        null,
                        now.AddMinutes(1),
                        now.AddMinutes(1)),
                    cancellationToken));

            Assert.Contains("OWNER_CORRECTION", exception.Message, StringComparison.Ordinal);
            Assert.Null(await store.GetAsync(invalidReplacementId, cancellationToken));
            MemoryRecord? original = await store.GetAsync(originalId, cancellationToken);
            Assert.NotNull(original);
            Assert.True(original.IsCurrent);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CorrectionRejectsScopeChangeWithoutPartialInsert()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory("loren-m4-correction-scope");
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset now = new(2026, 9, 4, 10, 5, 0, TimeSpan.Zero);
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        MemoryRecordId originalId = MemoryRecordId.New();
        MemoryRecordId invalidReplacementId = MemoryRecordId.New();

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            await SeedProjectAsync(context, projectId, repositoryId, now, cancellationToken);
            SqliteMemoryStore store = new(context);
            await store.AddAsync(
                OwnerMemory(originalId, projectId, repositoryId, now),
                cancellationToken);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.CorrectAsync(
                    originalId,
                    new MemoryRecord(
                        invalidReplacementId,
                        MemorySourceClass.OwnerCorrection,
                        "Correction with intentionally changed repository scope.",
                        projectId,
                        null,
                        "owner:authenticated",
                        null,
                        now.AddMinutes(1),
                        now.AddMinutes(1)),
                    cancellationToken));

            Assert.Contains("same Project/Repository scope", exception.Message, StringComparison.Ordinal);
            Assert.Null(await store.GetAsync(invalidReplacementId, cancellationToken));
            MemoryRecord? original = await store.GetAsync(originalId, cancellationToken);
            Assert.NotNull(original);
            Assert.True(original.IsCurrent);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AlreadySupersededMemoryCannotBeCorrectedAgain()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory("loren-m4-correction-current");
        string connectionString = $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset now = new(2026, 9, 4, 10, 5, 0, TimeSpan.Zero);
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        MemoryRecordId originalId = MemoryRecordId.New();
        MemoryRecordId firstCorrectionId = MemoryRecordId.New();
        MemoryRecordId secondCorrectionId = MemoryRecordId.New();

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            await SeedProjectAsync(context, projectId, repositoryId, now, cancellationToken);
            SqliteMemoryStore store = new(context);
            await store.AddAsync(
                OwnerMemory(originalId, projectId, repositoryId, now),
                cancellationToken);

            await store.CorrectAsync(
                originalId,
                CorrectionMemory(
                    firstCorrectionId,
                    projectId,
                    repositoryId,
                    "First correction.",
                    now.AddMinutes(1)),
                cancellationToken);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.CorrectAsync(
                    originalId,
                    CorrectionMemory(
                        secondCorrectionId,
                        projectId,
                        repositoryId,
                        "Invalid second correction against stale record.",
                        now.AddMinutes(2)),
                    cancellationToken));

            Assert.Contains("already superseded", exception.Message, StringComparison.Ordinal);
            Assert.Null(await store.GetAsync(secondCorrectionId, cancellationToken));

            IReadOnlyList<MemoryRecord> current = await store.ListCurrentForProjectAsync(
                projectId,
                cancellationToken);
            MemoryRecord currentMemory = Assert.Single(current);
            Assert.Equal(firstCorrectionId, currentMemory.Id);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static MemoryRecord OwnerMemory(
        MemoryRecordId id,
        ProjectId projectId,
        RepositoryId repositoryId,
        DateTimeOffset timestamp) => new(
            id,
            MemorySourceClass.OwnerExplicit,
            "Original owner memory.",
            projectId,
            repositoryId,
            "owner:authenticated",
            null,
            timestamp,
            timestamp);

    private static MemoryRecord CorrectionMemory(
        MemoryRecordId id,
        ProjectId projectId,
        RepositoryId repositoryId,
        string content,
        DateTimeOffset timestamp) => new(
            id,
            MemorySourceClass.OwnerCorrection,
            content,
            projectId,
            repositoryId,
            "owner:authenticated",
            null,
            timestamp,
            timestamp);

    private static async Task SeedProjectAsync(
        CanonicalStateDbContext context,
        ProjectId projectId,
        RepositoryId repositoryId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await new SqliteProjectCatalog(context).SaveAsync(
            new ProjectSnapshot(
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
                ]),
            cancellationToken);
    }

    private static CanonicalStateDbContext CreateContext(string connectionString)
    {
        DbContextOptions<CanonicalStateDbContext> options =
            new DbContextOptionsBuilder<CanonicalStateDbContext>()
                .UseSqlite(connectionString)
                .Options;

        return new CanonicalStateDbContext(options);
    }

    private static string CreateTempDirectory(string prefix)
    {
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}
