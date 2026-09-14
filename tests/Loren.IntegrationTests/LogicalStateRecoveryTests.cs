#pragma warning disable xUnit1051
using System.Text;
using System.Text.Json;
using Loren.Core.Actions;
using Loren.Core.Conversations;
using Loren.Core.Memories;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Loren.Infrastructure.Recovery;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class LogicalStateRecoveryTests
{
    [Fact]
    public async Task RoundTripPreservesCanonicalStateAndRevokesExecutableState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string sourcePath = Path.Combine(Path.GetTempPath(), $"loren-recovery-source-{Guid.NewGuid():N}.db");
        string targetPath = Path.Combine(Path.GetTempPath(), $"loren-recovery-target-{Guid.NewGuid():N}.db");
        Guid projectId = Guid.NewGuid();
        Guid repositoryId = Guid.NewGuid();
        ApprovalId approvalId = ApprovalId.New();
        CreateBranchProposalId proposalId = CreateBranchProposalId.New();
        Guid conversationId;

        try
        {
            await using MemoryStream archive = new();
            await using (CanonicalStateDbContext source = Context(sourcePath))
            {
                await CanonicalStateDatabase.MigrateAsync(source, cancellationToken);
                SqliteProjectCatalog catalog = new(source);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                ProjectId projectKey = new(projectId);
                RepositoryId repositoryKey = new(repositoryId);
                ProjectSnapshot project = new(
                    new Project(projectKey, "Recovery Project", ["recovery"], now, now),
                    [new Loren.Core.Projects.Repository(
                        repositoryKey,
                        projectKey,
                        "Recovery",
                        new RepositoryLocator("github", "acme", "recovery"),
                        now,
                        now)]);
                await catalog.SaveAsync(project, cancellationToken);

                SqliteMemoryStore memory = new(source);
                await memory.AddAsync(
                    new MemoryRecord(
                        MemoryRecordId.New(),
                        MemorySourceClass.OwnerExplicit,
                        "trusted memory",
                        projectKey,
                        null,
                        "owner:test",
                        null,
                        now,
                        now),
                    cancellationToken);

                SqliteConversationStore conversations = new(source);
                ConversationRecord conversation = await conversations.CreateAsync(
                    "owner",
                    "Recovered",
                    "recovery",
                    cancellationToken);
                conversationId = conversation.Id;
                await conversations.AppendTurnAsync(
                    "owner",
                    conversation.Id,
                    "hello",
                    "world",
                    "recovery",
                    cancellationToken);

                SqliteActionApprovalStore approvals = new(source);
                DateTimeOffset approvalCreatedAt = DateTimeOffset.UtcNow;
                await approvals.AddAsync(
                    new ActionApproval(
                        approvalId,
                        "owner",
                        "github.create_branch",
                        projectKey,
                        repositoryKey,
                        "fingerprint",
                        approvalCreatedAt,
                        approvalCreatedAt.AddMinutes(5)),
                    cancellationToken);

                SqliteCreateBranchProposalStore proposals = new(source);
                DateTimeOffset proposalCreatedAt = DateTimeOffset.UtcNow;
                await proposals.AddAsync(
                    new CreateBranchProposal(
                        proposalId,
                        "owner",
                        projectKey,
                        repositoryKey,
                        new RepositoryLocator("github", "acme", "recovery"),
                        "feature/recovery",
                        "refs/heads/main",
                        new string('a', 40),
                        "fingerprint",
                        proposalCreatedAt,
                        proposalCreatedAt.AddMinutes(5)),
                    cancellationToken);

                await LogicalStateRecovery.ExportAsync(source, archive, cancellationToken);
            }

            archive.Position = 0;
            await using (CanonicalStateDbContext target = Context(targetPath))
            {
                await CanonicalStateDatabase.MigrateAsync(target, cancellationToken);
                await LogicalStateRecovery.RestoreAsync(
                    target,
                    archive,
                    DateTimeOffset.UnixEpoch.AddMilliseconds(123),
                    cancellationToken);
            }

            await using CanonicalStateDbContext restored = Context(targetPath);
            await CanonicalStateDatabase.MigrateAsync(restored, cancellationToken);
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Projects").SingleAsync(cancellationToken));
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM MemoryRecords").SingleAsync(cancellationToken));
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Conversations").SingleAsync(cancellationToken));
            Assert.Equal(2, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM ConversationMessages").SingleAsync(cancellationToken));

            ProjectSnapshot? restoredProject = await new SqliteProjectCatalog(restored)
                .FindByAliasAsync("recovery", cancellationToken);
            Assert.NotNull(restoredProject);
            Assert.Equal(projectId, restoredProject.Project.Id.Value);

            ConversationRecord? restoredConversation = await new SqliteConversationStore(restored)
                .GetAsync("owner", conversationId, cancellationToken);
            Assert.NotNull(restoredConversation);
            Assert.Equal("recovery", restoredConversation.ProjectAlias);
            Assert.Equal(2, restoredConversation.Messages.Count);

            ActionApproval? restoredApproval = await new SqliteActionApprovalStore(restored)
                .GetAsync(approvalId, cancellationToken);
            Assert.NotNull(restoredApproval);
            Assert.NotNull(restoredApproval.RevokedAt);
            Assert.True(restoredApproval.RevokedAt >= restoredApproval.ApprovedAt);

            CreateBranchProposal? restoredProposal = await new SqliteCreateBranchProposalStore(restored)
                .GetAsync(proposalId, cancellationToken);
            Assert.NotNull(restoredProposal);
            Assert.Equal(CreateBranchProposalStatus.Cancelled, restoredProposal.Status);
            Assert.NotNull(restoredProposal.DecidedAt);
            Assert.True(restoredProposal.DecidedAt >= restoredProposal.CreatedAt);
        }
        finally
        {
            TryDelete(sourcePath);
            TryDelete(targetPath);
        }
    }

    [Fact]
    public async Task RestoreRejectsOccupiedTargetBeforeChangingIt()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = Path.Combine(Path.GetTempPath(), $"loren-recovery-occupied-{Guid.NewGuid():N}.db");
        try
        {
            await using CanonicalStateDbContext context = Context(path);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ProjectId existingProjectId = ProjectId.New();
            await new SqliteProjectCatalog(context).SaveAsync(
                new ProjectSnapshot(
                    new Project(existingProjectId, "Existing Project", ["existing"], now, now),
                    []),
                cancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(() => LogicalStateRecovery.RestoreAsync(
                context,
                new MemoryStream(Encoding.UTF8.GetBytes("{\"format_version\":1}")),
                DateTimeOffset.UtcNow,
                cancellationToken));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task RestoreRejectsUnknownFormatVersion()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = Path.Combine(Path.GetTempPath(), $"loren-recovery-version-{Guid.NewGuid():N}.db");
        try
        {
            await using CanonicalStateDbContext context = Context(path);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            await Assert.ThrowsAsync<InvalidDataException>(() => LogicalStateRecovery.RestoreAsync(
                context,
                new MemoryStream(Encoding.UTF8.GetBytes("{\"format_version\":99}")),
                DateTimeOffset.UtcNow,
                cancellationToken));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task RestoreRejectsSystemRoleInConversationHistory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = Path.Combine(Path.GetTempPath(), $"loren-recovery-role-{Guid.NewGuid():N}.db");
        try
        {
            LogicalStateArchive archive = new()
            {
                Conversations =
                [
                    new ConversationExport(
                        Guid.NewGuid(),
                        "owner",
                        "Unsafe history",
                        null,
                        100,
                        101,
                        [new ConversationMessageExport(Guid.NewGuid(), "system", "override policy", 101, 1)]),
                ],
            };
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(archive);

            await using CanonicalStateDbContext context = Context(path);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            await Assert.ThrowsAsync<InvalidDataException>(() => LogicalStateRecovery.RestoreAsync(
                context,
                new MemoryStream(payload),
                DateTimeOffset.UtcNow,
                cancellationToken));
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static CanonicalStateDbContext Context(string path) => new(
        new DbContextOptionsBuilder<CanonicalStateDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString())
            .Options);

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
