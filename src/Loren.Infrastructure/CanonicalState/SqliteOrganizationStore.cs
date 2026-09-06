using Loren.Core.Organization;
using Loren.Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.CanonicalState;

public sealed class SqliteOrganizationStore(CanonicalStateDbContext dbContext) : IOrganizationStore
{
    private readonly CanonicalStateDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task AddAsync(
        OrganizationItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (await _dbContext.OrganizationItems
            .AnyAsync(row => row.Id == item.Id.Value, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Organization item '{item.Id}' already exists.");
        }

        await ValidateProjectScopeAsync(item.ProjectId, cancellationToken);

        _dbContext.OrganizationItems.Add(ToRow(item));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrganizationItem?> GetAsync(
        OrganizationItemId itemId,
        CancellationToken cancellationToken = default)
    {
        OrganizationItemRow? row = await _dbContext.OrganizationItems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == itemId.Value, cancellationToken);

        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<OrganizationItem>> ListAsync(
        ProjectId? projectId = null,
        OrganizationItemKind? kind = null,
        OrganizationTaskStatus? taskStatus = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0 || limit > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        if (kind is not null && !Enum.IsDefined(kind.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (taskStatus is not null)
        {
            if (!Enum.IsDefined(taskStatus.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(taskStatus));
            }

            if (kind is not null && kind is not OrganizationItemKind.Task)
            {
                throw new ArgumentException(
                    "Task status can only filter task organization items.",
                    nameof(taskStatus));
            }
        }

        IQueryable<OrganizationItemRow> query = _dbContext.OrganizationItems.AsNoTracking();

        if (projectId is not null)
        {
            Guid projectValue = projectId.Value.Value;
            query = query.Where(item => item.ProjectId == projectValue);
        }

        if (kind is not null)
        {
            string kindValue = kind.Value.ToString();
            query = query.Where(item => item.Kind == kindValue);
        }

        if (taskStatus is not null)
        {
            string statusValue = taskStatus.Value.ToString();
            query = query.Where(item => item.TaskStatus == statusValue);
        }

        OrganizationItemRow[] rows = await query
            .OrderByDescending(item => item.UpdatedAt)
            .ThenBy(item => item.Id)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return rows.Select(ToDomain).ToArray();
    }

    public async Task<OrganizationItem> SetTaskStatusAsync(
        OrganizationItemId taskId,
        OrganizationTaskStatus status,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        OrganizationItemRow row = await _dbContext.OrganizationItems
            .SingleOrDefaultAsync(item => item.Id == taskId.Value, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Organization task '{taskId}' does not exist.");

        OrganizationItem current = ToDomain(row);
        if (current.Kind is not OrganizationItemKind.Task)
        {
            throw new InvalidOperationException(
                $"Organization item '{taskId}' is not a task.");
        }

        if (changedAt < current.CreatedAt || changedAt < current.UpdatedAt)
        {
            throw new ArgumentException(
                "Task status change cannot precede the current task lifecycle.",
                nameof(changedAt));
        }

        row.TaskStatus = status.ToString();
        row.UpdatedAt = changedAt;
        row.CompletedAt = status is OrganizationTaskStatus.Completed
            ? changedAt
            : null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDomain(row);
    }

    private async Task ValidateProjectScopeAsync(
        ProjectId? projectId,
        CancellationToken cancellationToken)
    {
        if (projectId is null)
        {
            return;
        }

        bool exists = await _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == projectId.Value.Value, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException(
                $"Canonical project '{projectId}' does not exist.");
        }
    }

    private static OrganizationItemRow ToRow(OrganizationItem item) => new()
    {
        Id = item.Id.Value,
        Kind = item.Kind.ToString(),
        Title = item.Title,
        Content = item.Content,
        ProjectId = item.ProjectId?.Value,
        TaskStatus = item.TaskStatus?.ToString(),
        SourceReference = item.SourceReference,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        CompletedAt = item.CompletedAt,
    };

    private static OrganizationItem ToDomain(OrganizationItemRow row)
    {
        if (!Enum.TryParse(row.Kind, ignoreCase: false, out OrganizationItemKind kind)
            || !Enum.IsDefined(kind))
        {
            throw new InvalidOperationException(
                $"Organization item '{row.Id:N}' has invalid kind '{row.Kind}'.");
        }

        OrganizationTaskStatus? status = null;
        if (row.TaskStatus is not null)
        {
            if (!Enum.TryParse(
                    row.TaskStatus,
                    ignoreCase: false,
                    out OrganizationTaskStatus parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                throw new InvalidOperationException(
                    $"Organization item '{row.Id:N}' has invalid task status '{row.TaskStatus}'.");
            }

            status = parsedStatus;
        }

        return new OrganizationItem(
            new OrganizationItemId(row.Id),
            kind,
            row.Title,
            row.Content,
            row.ProjectId is Guid projectId ? new ProjectId(projectId) : null,
            status,
            row.SourceReference,
            row.CreatedAt,
            row.UpdatedAt,
            row.CompletedAt);
    }
}
