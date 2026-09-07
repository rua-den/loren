using Loren.Core.Projects;

namespace Loren.Core.Organization;

public enum OrganizationItemKind
{
    Note,
    Decision,
    Task,
}

public enum OrganizationTaskStatus
{
    Open,
    Completed,
}

public sealed record OrganizationItem
{
    public OrganizationItem(
        OrganizationItemId id,
        OrganizationItemKind kind,
        string? title,
        string content,
        ProjectId? projectId,
        OrganizationTaskStatus? taskStatus,
        string sourceReference,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? completedAt = null)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Organization item ID cannot be empty.", nameof(id));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);

        string? normalizedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        string normalizedContent = content.Trim();
        string normalizedSourceReference = sourceReference.Trim();

        if (normalizedTitle?.Length > 500)
        {
            throw new ArgumentException("Organization title cannot exceed 500 characters.", nameof(title));
        }

        if (normalizedContent.Length > 20_000)
        {
            throw new ArgumentException("Organization content cannot exceed 20000 characters.", nameof(content));
        }

        if (normalizedSourceReference.Length > 1000)
        {
            throw new ArgumentException("Organization source reference cannot exceed 1000 characters.", nameof(sourceReference));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("UpdatedAt cannot be earlier than CreatedAt.", nameof(updatedAt));
        }

        if (kind is OrganizationItemKind.Task)
        {
            if (normalizedTitle is null)
            {
                throw new ArgumentException("Task title is required.", nameof(title));
            }

            if (taskStatus is null || !Enum.IsDefined(taskStatus.Value))
            {
                throw new ArgumentException("Task status is required.", nameof(taskStatus));
            }

            if (taskStatus is OrganizationTaskStatus.Completed && completedAt is null)
            {
                throw new ArgumentException("Completed tasks require CompletedAt.", nameof(completedAt));
            }

            if (taskStatus is OrganizationTaskStatus.Open && completedAt is not null)
            {
                throw new ArgumentException("Open tasks cannot have CompletedAt.", nameof(completedAt));
            }
        }
        else if (taskStatus is not null || completedAt is not null)
        {
            throw new ArgumentException(
                "Only task organization items may have task status or completion time.",
                nameof(taskStatus));
        }

        if (completedAt is not null
            && (completedAt.Value < createdAt || completedAt.Value > updatedAt))
        {
            throw new ArgumentException(
                "CompletedAt must be within the item lifecycle.",
                nameof(completedAt));
        }

        Id = id;
        Kind = kind;
        Title = normalizedTitle;
        Content = normalizedContent;
        ProjectId = projectId;
        TaskStatus = taskStatus;
        SourceReference = normalizedSourceReference;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        CompletedAt = completedAt;
    }

    public OrganizationItemId Id { get; }

    public OrganizationItemKind Kind { get; }

    public string? Title { get; }

    public string Content { get; }

    public ProjectId? ProjectId { get; }

    public OrganizationTaskStatus? TaskStatus { get; }

    public string SourceReference { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public DateTimeOffset? CompletedAt { get; }
}
