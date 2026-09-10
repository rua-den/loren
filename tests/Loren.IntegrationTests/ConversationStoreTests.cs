using Loren.Core.Conversations;
using Loren.Infrastructure.CanonicalState;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class ConversationStoreTests
{
    [Fact]
    public async Task ConversationSurvivesRestartAndCannotBeReadByAnotherOwner()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-conversation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "loren.db"), Pooling = false }.ToString();
        try
        {
            await using (CanonicalStateDbContext db = CreateContext(connectionString))
            {
                await CanonicalStateDatabase.MigrateAsync(db, TestContext.Current.CancellationToken);
                SqliteConversationStore store = new(db);
                ConversationRecord created = await store.CreateAsync("owner-a", "Sprint notes", "loren", TestContext.Current.CancellationToken);
                await store.AppendTurnAsync("owner-a", created.Id, "Xin chào", "Chào mày", "loren", TestContext.Current.CancellationToken);
            }

            await using (CanonicalStateDbContext restarted = CreateContext(connectionString))
            {
                SqliteConversationStore store = new(restarted);
                ConversationSummary summary = Assert.Single(await store.ListAsync("owner-a", TestContext.Current.CancellationToken));
                ConversationRecord? restored = await store.GetAsync("owner-a", summary.Id, TestContext.Current.CancellationToken);
                Assert.NotNull(restored);
                Assert.Equal("Sprint notes", restored.Title);
                Assert.Collection(restored.Messages,
                    first => { Assert.Equal("user", first.Role); Assert.Equal("Xin chào", first.Content); },
                    second => { Assert.Equal("assistant", second.Role); Assert.Equal("Chào mày", second.Content); });
                Assert.Empty(await store.ListAsync("owner-b", TestContext.Current.CancellationToken));
                Assert.Null(await store.GetAsync("owner-b", summary.Id, TestContext.Current.CancellationToken));
            }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task ConversationReadKeepsMostRecentBoundedMessagesInOrder()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-conversation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string connectionString = $"Data Source={Path.Combine(directory, "loren.db")};Pooling=False";
        try
        {
            await using CanonicalStateDbContext db = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(db, TestContext.Current.CancellationToken);
            SqliteConversationStore store = new(db);
            ConversationRecord conversation = await store.CreateAsync("owner", null, null, TestContext.Current.CancellationToken);
            for (int index = 0; index < 105; index++)
            {
                await store.AppendTurnAsync("owner", conversation.Id, $"u{index}", $"a{index}", null, TestContext.Current.CancellationToken);
            }
            ConversationRecord? loaded = await store.GetAsync("owner", conversation.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            Assert.Equal(200, loaded.Messages.Count);
            Assert.Equal("u5", loaded.Messages[0].Content);
            Assert.Equal("a104", loaded.Messages[^1].Content);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task ExplicitProjectClearDoesNotReusePreviousConversationScope()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-conversation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string connectionString = $"Data Source={Path.Combine(directory, "loren.db")};Pooling=False";
        try
        {
            await using CanonicalStateDbContext db = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(db, TestContext.Current.CancellationToken);
            SqliteConversationStore store = new(db);
            ConversationRecord conversation = await store.CreateAsync("owner", null, "loren", TestContext.Current.CancellationToken);
            await store.AppendTurnAsync("owner", conversation.Id, "clear", "cleared", null, TestContext.Current.CancellationToken, clearProjectAlias: true);
            Assert.Null((await store.GetAsync("owner", conversation.Id, TestContext.Current.CancellationToken))!.ProjectAlias);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static CanonicalStateDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<CanonicalStateDbContext>().UseSqlite(connectionString).Options);
}
