using Loren.Core.Projects;

namespace Loren.Core.Memories;

public sealed record MemoryRecord
{
    public MemoryRecord(
        MemoryRecordId id,
        MemorySourceClass sourceClass,
        string content,
        ProjectId? projectId,
        RepositoryId? repositoryId,
        string? sourceReference,
        MemoryRecordId? supersededById,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Memory record ID cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (repositoryId is not null && projectId is null)
        {
            throw new ArgumentException(
                "Repository-scoped memory must also include its Project ID.",
                nameof(repositoryId));
        }

        if (supersededById == id)
        {
            throw new ArgumentException(
                "A memory record cannot supersede itself.",
                nameof(supersededById));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException(
                "Memory record UpdatedAt cannot be earlier than CreatedAt.",
                nameof(updatedAt));
        }

        Id = id;
        SourceClass = sourceClass;
        Content = content;
        ProjectId = projectId;
        RepositoryId = repositoryId;
        SourceReference = string.IsNullOrWhiteSpace(sourceReference)
            ? null
            : sourceReference;
        SupersededById = supersededById;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public MemoryRecordId Id { get; }

    public MemorySourceClass SourceClass { get; }

    public string Content { get; }

    public ProjectId? ProjectId { get; }

    public RepositoryId? RepositoryId { get; }

    public string? SourceReference { get; }

    public MemoryRecordId? SupersededById { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public bool IsCurrent => SupersededById is null;
}
