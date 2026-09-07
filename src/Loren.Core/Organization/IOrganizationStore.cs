using Loren.Core.Projects;

namespace Loren.Core.Organization;

public interface IOrganizationStore
{
    Task AddAsync(
        OrganizationItem item,
        CancellationToken cancellationToken = default);

    Task<OrganizationItem?> GetAsync(
        OrganizationItemId itemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationItem>> ListAsync(
        ProjectId? projectId = null,
        OrganizationItemKind? kind = null,
        OrganizationTaskStatus? taskStatus = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<OrganizationItem> SetTaskStatusAsync(
        OrganizationItemId taskId,
        OrganizationTaskStatus status,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default);
}
