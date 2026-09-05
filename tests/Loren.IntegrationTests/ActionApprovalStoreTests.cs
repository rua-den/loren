using Loren.Core.Actions;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class ActionApprovalStoreTests
{
    [Fact]
    public async Task ApprovalSurvivesRestartAndCanBeConsumedOnlyOnce()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory("loren-m5-approval");
        string connectionString = ConnectionString(tempDirectory);
        DateTimeOffset approvedAt = new(2026, 9, 4, 17, 0, 0, TimeSpan.Zero);
        DateTimeOffset consumeAt = approvedAt.AddMinutes(5);
        ApprovalFixture fixture = CreateFixture(approvedAt);

        try
        {
            await using (CanonicalStateDbContext firstContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(firstContext, cancellationToken);
                await SeedProjectAsync(firstContext, fixture, cancellationToken);
                await new SqliteActionApprovalStore(firstContext).AddAsync(
                    fixture.Approval,
                    cancellationToken);

                string[] migrations = (await firstContext.Database
                        .GetAppliedMigrationsAsync(cancellationToken))
                    .ToArray();
                Assert.Contains("202609040003_AddActionApprovals", migrations);
            }

            await using (CanonicalStateDbContext restartedContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(restartedContext, cancellationToken);
                SqliteActionApprovalStore store = new(restartedContext);

                ActionApproval? loaded = await store.GetAsync(
                    fixture.Approval.Id,
                    cancellationToken);
                Assert.NotNull(loaded);
                Assert.Equal(fixture.Approval.IntentFingerprint, loaded.IntentFingerprint);
                Assert.Null(loaded.ConsumedAt);

                ApprovalConsumptionResult consumed = await store.ConsumeAsync(
                    Consumption(fixture, consumeAt),
                    cancellationToken);
                Assert.Equal(ApprovalConsumptionStatus.Consumed, consumed.Status);
            }

            await using CanonicalStateDbContext secondRestart = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(secondRestart, cancellationToken);
            SqliteActionApprovalStore restartedStore = new(secondRestart);

            ApprovalConsumptionResult replay = await restartedStore.ConsumeAsync(
                Consumption(fixture, consumeAt.AddMinutes(1)),
                cancellationToken);
            ActionApproval? consumedApproval = await restartedStore.GetAsync(
                fixture.Approval.Id,
                cancellationToken);

            Assert.Equal(ApprovalConsumptionStatus.AlreadyConsumed, replay.Status);
            Assert.NotNull(consumedApproval);
            Assert.Equal(consumeAt, consumedApproval.ConsumedAt);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExpiredRevokedAndMismatchedApprovalsFailClosedWithoutConsumption()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory("loren-m5-approval-failures");
        string connectionString = ConnectionString(tempDirectory);
        DateTimeOffset now = new(2026, 9, 4, 18, 0, 0, TimeSpan.Zero);
        ApprovalFixture fixture = CreateFixture(now.AddMinutes(-10));

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            await SeedProjectAsync(context, fixture, cancellationToken);
            SqliteActionApprovalStore store = new(context);

            ActionApproval expired = Approval(
                fixture,
                ApprovalId.New(),
                "EXPIRED",
                now.AddMinutes(-10),
                now.AddMinutes(-1));
            ActionApproval revoked = Approval(
                fixture,
                ApprovalId.New(),
                "REVOKED",
                now.AddMinutes(-10),
                now.AddMinutes(10));
            ActionApproval mismatch = Approval(
                fixture,
                ApprovalId.New(),
                "MATCHED",
                now.AddMinutes(-10),
                now.AddMinutes(10));

            await store.AddAsync(expired, cancellationToken);
            await store.AddAsync(revoked, cancellationToken);
            await store.AddAsync(mismatch, cancellationToken);
            await store.RevokeAsync(revoked.Id, now.AddMinutes(-2), cancellationToken);

            ApprovalConsumptionResult expiredResult = await store.ConsumeAsync(
                Consumption(fixture, expired.Id, "EXPIRED", now),
                cancellationToken);
            ApprovalConsumptionResult revokedResult = await store.ConsumeAsync(
                Consumption(fixture, revoked.Id, "REVOKED", now),
                cancellationToken);
            ApprovalConsumptionResult mismatchResult = await store.ConsumeAsync(
                Consumption(fixture, mismatch.Id, "DIFFERENT", now),
                cancellationToken);

            Assert.Equal(ApprovalConsumptionStatus.Expired, expiredResult.Status);
            Assert.Equal(ApprovalConsumptionStatus.Revoked, revokedResult.Status);
            Assert.Equal(ApprovalConsumptionStatus.Mismatch, mismatchResult.Status);
            Assert.Null((await store.GetAsync(expired.Id, cancellationToken))!.ConsumedAt);
            Assert.Null((await store.GetAsync(revoked.Id, cancellationToken))!.ConsumedAt);
            Assert.Null((await store.GetAsync(mismatch.Id, cancellationToken))!.ConsumedAt);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ConcurrentConsumptionHasExactlyOneWinner()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = CreateTempDirectory("loren-m5-approval-race");
        string connectionString = ConnectionString(tempDirectory);
        DateTimeOffset approvedAt = new(2026, 9, 4, 19, 0, 0, TimeSpan.Zero);
        ApprovalFixture fixture = CreateFixture(approvedAt);

        try
        {
            await using (CanonicalStateDbContext seedContext = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(seedContext, cancellationToken);
                await SeedProjectAsync(seedContext, fixture, cancellationToken);
                await new SqliteActionApprovalStore(seedContext).AddAsync(
                    fixture.Approval,
                    cancellationToken);
            }

            await using CanonicalStateDbContext firstContext = CreateContext(connectionString);
            await using CanonicalStateDbContext secondContext = CreateContext(connectionString);
            SqliteActionApprovalStore firstStore = new(firstContext);
            SqliteActionApprovalStore secondStore = new(secondContext);
            ApprovalConsumptionRequest request = Consumption(
                fixture,
                approvedAt.AddMinutes(1));

            ApprovalConsumptionResult[] results = await Task.WhenAll(
                firstStore.ConsumeAsync(request, cancellationToken),
                secondStore.ConsumeAsync(request, cancellationToken));

            Assert.Single(
                results,
                result => result.Status is ApprovalConsumptionStatus.Consumed);
            Assert.Single(
                results,
                result => result.Status is ApprovalConsumptionStatus.AlreadyConsumed);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static ApprovalFixture CreateFixture(DateTimeOffset approvedAt)
    {
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        ActionApproval approval = new(
            ApprovalId.New(),
            "owner:session-1",
            "github.update_file",
            projectId,
            repositoryId,
            "FINGERPRINT",
            approvedAt,
            approvedAt.AddHours(1));

        return new ApprovalFixture(projectId, repositoryId, approval);
    }

    private static ActionApproval Approval(
        ApprovalFixture fixture,
        ApprovalId approvalId,
        string fingerprint,
        DateTimeOffset approvedAt,
        DateTimeOffset expiresAt) => new(
        approvalId,
        fixture.Approval.OwnerPrincipalReference,
        fixture.Approval.ActionName,
        fixture.ProjectId,
        fixture.RepositoryId,
        fingerprint,
        approvedAt,
        expiresAt);

    private static ApprovalConsumptionRequest Consumption(
        ApprovalFixture fixture,
        DateTimeOffset consumedAt) =>
        Consumption(
            fixture,
            fixture.Approval.Id,
            fixture.Approval.IntentFingerprint,
            consumedAt);

    private static ApprovalConsumptionRequest Consumption(
        ApprovalFixture fixture,
        ApprovalId approvalId,
        string fingerprint,
        DateTimeOffset consumedAt) => new(
        approvalId,
        fixture.Approval.OwnerPrincipalReference,
        fixture.Approval.ActionName,
        fixture.ProjectId,
        fixture.RepositoryId,
        fingerprint,
        consumedAt);

    private static async Task SeedProjectAsync(
        CanonicalStateDbContext context,
        ApprovalFixture fixture,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = fixture.Approval.ApprovedAt;
        await new SqliteProjectCatalog(context).SaveAsync(
            new ProjectSnapshot(
                new Project(
                    fixture.ProjectId,
                    "Loren",
                    ["loren"],
                    now,
                    now),
                [
                    new CanonicalRepository(
                        fixture.RepositoryId,
                        fixture.ProjectId,
                        "Loren GitHub",
                        new RepositoryLocator("github", "rua-den", "loren"),
                        now,
                        now),
                ]),
            cancellationToken);
    }

    private static string ConnectionString(string tempDirectory) =>
        $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";

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
        string path = Path.Combine(
            Path.GetTempPath(),
            $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed record ApprovalFixture(
        ProjectId ProjectId,
        RepositoryId RepositoryId,
        ActionApproval Approval);
}
