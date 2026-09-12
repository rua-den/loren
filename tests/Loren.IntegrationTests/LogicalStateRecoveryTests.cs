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
        string sourcePath = Path.Combine(Path.GetTempPath(), $"loren-recovery-source-{Guid.NewGuid():N}.db");
        string targetPath = Path.Combine(Path.GetTempPath(), $"loren-recovery-target-{Guid.NewGuid():N}.db");
        try
        {
            Guid projectId = Guid.NewGuid();
            Guid repositoryId = Guid.NewGuid();
            await using (CanonicalStateDbContext source = Context(sourcePath))
            {
                await source.Database.EnsureCreatedAsync();
                SqliteProjectCatalog catalog = new(source);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                projectId = Guid.NewGuid();
                repositoryId = Guid.NewGuid();
                ProjectId projectKey = new(projectId);
                RepositoryId repositoryKey = new(repositoryId);
                ProjectSnapshot project = new(
                    new Project(projectKey, "Recovery Project", ["recovery"], now, now),
                    [new Repository(repositoryKey, projectKey, "Recovery", new RepositoryLocator("github", "acme", "recovery"), now, now)]);
                await catalog.SaveAsync(project);
                SqliteMemoryStore memory = new(source);
                await memory.AddAsync(new MemoryRecord(
                    MemoryRecordId.New(), MemorySourceClass.OwnerExplicit, "trusted memory", projectKey,
                    null, "owner:test", null, now, now));
                SqliteConversationStore conversations = new(source);
                ConversationRecord conversation = await conversations.CreateAsync("owner", "Recovered", "recovery");
                await conversations.AppendTurnAsync("owner", conversation.Id, "hello", "world", "recovery");
                SqliteActionApprovalStore approvals = new(source);
                DateTimeOffset approvalCreatedAt = DateTimeOffset.UtcNow;
                await approvals.AddAsync(new ActionApproval(
                    new ApprovalId(Guid.NewGuid()), "owner", "github.create_branch",
                    projectKey, repositoryKey, "fingerprint",
                    approvalCreatedAt, approvalCreatedAt.AddMinutes(5)));
                SqliteCreateBranchProposalStore proposals = new(source);
                DateTimeOffset proposalCreatedAt = DateTimeOffset.UtcNow;
                await proposals.AddAsync(new CreateBranchProposal(
                    new CreateBranchProposalId(Guid.NewGuid()), "owner", projectKey,
                    repositoryKey, new RepositoryLocator("github", "acme", "recovery"),
                    "feature/recovery", "refs/heads/main", new string('a', 40), "fingerprint",
                    proposalCreatedAt, proposalCreatedAt.AddMinutes(5)));

                await using MemoryStream archive = new();
                await LogicalStateRecovery.ExportAsync(source, archive);
                archive.Position = 0;
                await using CanonicalStateDbContext target = Context(targetPath);
                await target.Database.EnsureCreatedAsync();
                await LogicalStateRecovery.RestoreAsync(target, archive, DateTimeOffset.UnixEpoch.AddMilliseconds(123));
            }

            await using CanonicalStateDbContext restored = Context(targetPath);
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Projects").SingleAsync());
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM MemoryRecords").SingleAsync());
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Conversations").SingleAsync());
            Assert.Equal(2, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM ConversationMessages").SingleAsync());
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM ActionApprovals WHERE RevokedAtUnixMs IS NOT NULL").SingleAsync());
            Assert.Equal(1, await restored.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM CreateBranchProposals WHERE Status = 'Cancelled'").SingleAsync());
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
        string path = Path.Combine(Path.GetTempPath(), $"loren-recovery-occupied-{Guid.NewGuid():N}.db");
        try
        {
            await using CanonicalStateDbContext context = Context(path);
            await context.Database.EnsureCreatedAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ProjectId existingProjectId = new(Guid.NewGuid());
            await new SqliteProjectCatalog(context).SaveAsync(new ProjectSnapshot(
                new Project(existingProjectId, "Existing Project", ["existing"], now, now),
                []));

            await Assert.ThrowsAsync<InvalidOperationException>(() => LogicalStateRecovery.RestoreAsync(
                context,
                new MemoryStream(Encoding.UTF8.GetBytes("{\"format_version\":1}")),
                DateTimeOffset.UtcNow));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task RestoreRejectsUnknownFormatVersion()
    {
        string path = Path.Combine(Path.GetTempPath(), $"loren-recovery-version-{Guid.NewGuid():N}.db");
        try
        {
            await using CanonicalStateDbContext context = Context(path);
            await context.Database.EnsureCreatedAsync();
            await Assert.ThrowsAsync<InvalidDataException>(() => LogicalStateRecovery.RestoreAsync(
                context,
                new MemoryStream(Encoding.UTF8.GetBytes("{\"format_version\":99}")),
                DateTimeOffset.UtcNow));
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
        if (File.Exists(path)) File.Delete(path);
    }
}
