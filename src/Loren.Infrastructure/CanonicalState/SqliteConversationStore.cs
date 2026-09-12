using Loren.Core.Conversations;
using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.CanonicalState;

public sealed class SqliteConversationStore : IConversationStore
{
    private readonly CanonicalStateDbContext _dbContext;

    public SqliteConversationStore(CanonicalStateDbContext dbContext) => _dbContext = dbContext;

    public async Task<ConversationRecord> CreateAsync(
        string ownerPrincipalReference,
        string? title,
        string? projectAlias,
        CancellationToken cancellationToken = default)
    {
        ValidateOwner(ownerPrincipalReference);
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        ConversationRow row = new()
        {
            Id = Guid.NewGuid(),
            OwnerPrincipalReference = ownerPrincipalReference,
            Title = NormalizeTitle(title),
            ProjectAlias = NormalizeAlias(projectAlias),
            CreatedAtUnixMs = now,
            UpdatedAtUnixMs = now,
        };
        _dbContext.Conversations.Add(row);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(row, []);
    }

    public async Task<IReadOnlyList<ConversationSummary>> ListAsync(
        string ownerPrincipalReference,
        CancellationToken cancellationToken = default)
    {
        ValidateOwner(ownerPrincipalReference);
        var rows = await _dbContext.Conversations.AsNoTracking()
            .Where(item => item.OwnerPrincipalReference == ownerPrincipalReference)
            .OrderByDescending(item => item.UpdatedAtUnixMs)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        var counts = await _dbContext.ConversationMessages.AsNoTracking()
            .Where(message => rows.Select(row => row.Id).Contains(message.ConversationId))
            .GroupBy(message => message.ConversationId)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Id, item => item.Count, cancellationToken);
        return rows.Select(row => new ConversationSummary(
            row.Id,
            row.Title,
            row.ProjectAlias,
            FromUnix(row.CreatedAtUnixMs),
            FromUnix(row.UpdatedAtUnixMs),
            counts.GetValueOrDefault(row.Id))).ToArray();
    }

    public async Task<ConversationRecord?> GetAsync(
        string ownerPrincipalReference,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        ValidateOwner(ownerPrincipalReference);
        ConversationRow? row = await _dbContext.Conversations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == conversationId && item.OwnerPrincipalReference == ownerPrincipalReference, cancellationToken);
        if (row is null) return null;
        ConversationMessageRow[] messages = await _dbContext.ConversationMessages.AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.Sequence)
            .Take(200)
            .ToArrayAsync(cancellationToken);
        Array.Reverse(messages);
        return Map(row, messages);
    }

    public async Task AppendTurnAsync(
        string ownerPrincipalReference,
        Guid conversationId,
        string userContent,
        string assistantContent,
        string? projectAlias,
        CancellationToken cancellationToken = default,
        bool clearProjectAlias = false)
    {
        ValidateOwner(ownerPrincipalReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(userContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(assistantContent);
        ConversationRow? row = await _dbContext.Conversations
            .SingleOrDefaultAsync(item => item.Id == conversationId && item.OwnerPrincipalReference == ownerPrincipalReference, cancellationToken);
        if (row is null) throw new KeyNotFoundException("Conversation was not found.");
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long nextSequence = (await _dbContext.ConversationMessages
            .Where(message => message.ConversationId == conversationId)
            .MaxAsync(message => (long?)message.Sequence, cancellationToken) ?? 0) + 1;
        row.ProjectAlias = clearProjectAlias ? null : NormalizeAlias(projectAlias) ?? row.ProjectAlias;
        row.UpdatedAtUnixMs = now;
        _dbContext.ConversationMessages.AddRange(
            new ConversationMessageRow { Id = Guid.NewGuid(), ConversationId = conversationId, Role = "user", Content = userContent.Trim(), CreatedAtUnixMs = now, Sequence = nextSequence },
            new ConversationMessageRow { Id = Guid.NewGuid(), ConversationId = conversationId, Role = "assistant", Content = assistantContent.Trim(), CreatedAtUnixMs = now + 1, Sequence = nextSequence + 1 });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ConversationRecord Map(ConversationRow row, IEnumerable<ConversationMessageRow> messages) => new(
        row.Id, row.Title, row.ProjectAlias, FromUnix(row.CreatedAtUnixMs), FromUnix(row.UpdatedAtUnixMs),
        messages.Select(message => new ConversationMessage(message.Role, message.Content, FromUnix(message.CreatedAtUnixMs))).ToArray());

    private static void ValidateOwner(string owner) => ArgumentException.ThrowIfNullOrWhiteSpace(owner);
    private static string NormalizeTitle(string? title) => string.IsNullOrWhiteSpace(title) ? "Cuộc trò chuyện mới" : title.Trim()[..Math.Min(200, title.Trim().Length)];
    private static string? NormalizeAlias(string? alias) => string.IsNullOrWhiteSpace(alias) ? null : alias.Trim()[..Math.Min(200, alias.Trim().Length)];
    private static DateTimeOffset FromUnix(long value) => DateTimeOffset.FromUnixTimeMilliseconds(value);
}
