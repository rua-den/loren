#pragma warning disable CA1068
namespace Loren.Core.Conversations;

public interface IConversationStore
{
    Task<ConversationRecord> CreateAsync(
        string ownerPrincipalReference,
        string? title,
        string? projectAlias,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummary>> ListAsync(
        string ownerPrincipalReference,
        CancellationToken cancellationToken = default);

    Task<ConversationRecord?> GetAsync(
        string ownerPrincipalReference,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task AppendTurnAsync(
        string ownerPrincipalReference,
        Guid conversationId,
        string userContent,
        string assistantContent,
        string? projectAlias,
        CancellationToken cancellationToken = default,
        bool clearProjectAlias = false);
}

public sealed record ConversationSummary(
    Guid Id,
    string Title,
    string? ProjectAlias,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int MessageCount);

public sealed record ConversationRecord(
    Guid Id,
    string Title,
    string? ProjectAlias,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ConversationMessage> Messages);

public sealed record ConversationMessage(
    string Role,
    string Content,
    DateTimeOffset CreatedAt);
