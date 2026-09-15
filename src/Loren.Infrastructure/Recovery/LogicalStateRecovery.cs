using System.Text.Json;
using System.Text.Json.Serialization;
using Loren.Core.Actions;
using Loren.Core.Memories;
using Loren.Core.Organization;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.Recovery;

/// <summary>Versioned, credential-free logical state archive used by the maintenance tool.</summary>
public sealed record LogicalStateArchive
{
    [JsonPropertyName("format_version")]
    public int FormatVersion { get; init; } = 1;

    public List<ProjectExport> Projects { get; init; } = [];
    public List<RepositoryExport> Repositories { get; init; } = [];
    public List<ProjectAliasExport> Aliases { get; init; } = [];
    public List<MemoryExport> TrustedMemory { get; init; } = [];
    public List<OrganizationItemExport> Notes { get; init; } = [];
    public List<OrganizationItemExport> Decisions { get; init; } = [];
    public List<OrganizationItemExport> Tasks { get; init; } = [];
    public List<OrganizationItemExport> OtherOrganizationItems { get; init; } = [];
    public List<ConversationExport> Conversations { get; init; } = [];
    public List<ActionApprovalExport> Approvals { get; init; } = [];
    public List<CreateBranchProposalExport> Proposals { get; init; } = [];
    public List<RetainedAuditExport> RetainedAudit { get; init; } = [];
}

public sealed record ProjectExport(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record ProjectAliasExport(string Alias, string NormalizedAlias, Guid ProjectId);
public sealed record RepositoryExport(Guid Id, Guid ProjectId, string Name, string Provider, string ExternalNamespace, string ExternalName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record MemoryExport(Guid Id, string SourceClass, string Content, Guid? ProjectId, Guid? RepositoryId, string? SourceReference, Guid? SupersededById, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record OrganizationItemExport(Guid Id, string Kind, string? Title, string Content, Guid? ProjectId, string? TaskStatus, string SourceReference, long CreatedAtUnixMs, long UpdatedAtUnixMs, long? CompletedAtUnixMs);
public sealed record ConversationExport(Guid Id, string OwnerPrincipalReference, string Title, string? ProjectAlias, long CreatedAtUnixMs, long UpdatedAtUnixMs, List<ConversationMessageExport> Messages);
public sealed record ConversationMessageExport(Guid Id, string Role, string Content, long CreatedAtUnixMs, long Sequence);
public sealed record ActionApprovalExport(Guid Id, string OwnerPrincipalReference, string ActionName, Guid ProjectId, Guid RepositoryId, string IntentFingerprint, long ApprovedAtUnixMs, long ExpiresAtUnixMs, long? ConsumedAtUnixMs, long? RevokedAtUnixMs);
public sealed record CreateBranchProposalExport(Guid Id, string OwnerPrincipalReference, Guid ProjectId, Guid RepositoryId, string RepositoryProvider, string RepositoryNamespace, string RepositoryName, string Branch, string SourceRef, string SourceSha, string IntentFingerprint, string Status, long CreatedAtUnixMs, long ExpiresAtUnixMs, long? DecidedAtUnixMs);
public sealed record RetainedAuditExport(long Id, string RunId, string ActionId, string EventType, string ActionName, string Outcome, long OccurredAtUnixMs, string? Detail);

public static class LogicalStateRecovery
{
    public const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task ExportAsync(
        CanonicalStateDbContext source,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction snapshot =
            await source.Database.BeginTransactionAsync(cancellationToken);

        LogicalStateArchive archive = new()
        {
            Projects = await source.Projects.AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new ProjectExport(x.Id, x.Name, x.CreatedAt, x.UpdatedAt))
                .ToListAsync(cancellationToken),
            Aliases = await source.ProjectAliases.AsNoTracking()
                .OrderBy(x => x.NormalizedAlias)
                .Select(x => new ProjectAliasExport(x.Alias, x.NormalizedAlias, x.ProjectId))
                .ToListAsync(cancellationToken),
            Repositories = await source.Repositories.AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new RepositoryExport(x.Id, x.ProjectId, x.Name, x.Provider, x.ExternalNamespace, x.ExternalName, x.CreatedAt, x.UpdatedAt))
                .ToListAsync(cancellationToken),
            TrustedMemory = await source.MemoryRecords.AsNoTracking()
                .Where(x => x.SourceClass == "OWNER_EXPLICIT"
                    || x.SourceClass == "OWNER_CORRECTION"
                    || x.SourceClass == "VERIFIED_TOOL"
                    || x.SourceClass == "OWNER_APPROVED_INFERENCE")
                .OrderBy(x => x.Id)
                .Select(x => new MemoryExport(x.Id, x.SourceClass, x.Content, x.ProjectId, x.RepositoryId, x.SourceReference, x.SupersededById, x.CreatedAt, x.UpdatedAt))
                .ToListAsync(cancellationToken),
            Conversations = await ExportConversationsAsync(source, cancellationToken),
            Approvals = await source.ActionApprovals.AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new ActionApprovalExport(x.Id, x.OwnerPrincipalReference, x.ActionName, x.ProjectId, x.RepositoryId, x.IntentFingerprint, x.ApprovedAtUnixMs, x.ExpiresAtUnixMs, x.ConsumedAtUnixMs, x.RevokedAtUnixMs))
                .ToListAsync(cancellationToken),
            Proposals = await source.CreateBranchProposals.AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new CreateBranchProposalExport(x.Id, x.OwnerPrincipalReference, x.ProjectId, x.RepositoryId, x.RepositoryProvider, x.RepositoryNamespace, x.RepositoryName, x.Branch, x.SourceRef, x.SourceSha, x.IntentFingerprint, x.Status, x.CreatedAtUnixMs, x.ExpiresAtUnixMs, x.DecidedAtUnixMs))
                .ToListAsync(cancellationToken),
            RetainedAudit = await source.AuditEvents.AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new RetainedAuditExport(x.Id, x.RunId, x.ActionId, x.Kind, x.ActionName, x.Outcome, x.OccurredAtUnixMs, x.Detail))
                .ToListAsync(cancellationToken),
        };

        List<OrganizationItemExport> organizationItems = await source.OrganizationItems.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new OrganizationItemExport(x.Id, x.Kind, x.Title, x.Content, x.ProjectId, x.TaskStatus, x.SourceReference, x.CreatedAtUnixMs, x.UpdatedAtUnixMs, x.CompletedAtUnixMs))
            .ToListAsync(cancellationToken);

        archive = archive with
        {
            Notes = organizationItems.Where(x => x.Kind.Equals("note", StringComparison.OrdinalIgnoreCase)).ToList(),
            Decisions = organizationItems.Where(x => x.Kind.Equals("decision", StringComparison.OrdinalIgnoreCase)).ToList(),
            Tasks = organizationItems.Where(x => x.Kind.Equals("task", StringComparison.OrdinalIgnoreCase)).ToList(),
            OtherOrganizationItems = organizationItems
                .Where(x => !x.Kind.Equals("note", StringComparison.OrdinalIgnoreCase)
                    && !x.Kind.Equals("decision", StringComparison.OrdinalIgnoreCase)
                    && !x.Kind.Equals("task", StringComparison.OrdinalIgnoreCase))
                .ToList(),
        };

        await snapshot.CommitAsync(cancellationToken);
        await JsonSerializer.SerializeAsync(destination, archive, JsonOptions, cancellationToken);
    }

    public static async Task RestoreAsync(
        CanonicalStateDbContext target,
        Stream source,
        DateTimeOffset? restoredAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        using JsonDocument document = await JsonDocument.ParseAsync(source, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("format_version", out JsonElement version)
            || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out _))
        {
            throw new InvalidDataException("Recovery archive must declare integer format_version.");
        }

        LogicalStateArchive archive = document.RootElement.Deserialize<LogicalStateArchive>(JsonOptions)
            ?? throw new InvalidDataException("Recovery archive is empty.");
        Validate(archive);
        if (archive.FormatVersion != CurrentFormatVersion)
        {
            throw new InvalidDataException($"Unsupported recovery format version '{archive.FormatVersion}'.");
        }

        if (await HasRowsAsync(target, cancellationToken))
        {
            throw new InvalidOperationException("Recovery target must be a fresh empty database.");
        }

        long revokeAt = (restoredAt ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await target.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            target.Projects.AddRange(archive.Projects.Select(x => new ProjectRow
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            }));
            target.ProjectAliases.AddRange(archive.Aliases.Select(x => new ProjectAliasRow
            {
                Alias = x.Alias,
                NormalizedAlias = x.NormalizedAlias,
                ProjectId = x.ProjectId,
            }));
            target.Repositories.AddRange(archive.Repositories.Select(x => new RepositoryRow
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                Name = x.Name,
                Provider = x.Provider,
                ExternalNamespace = x.ExternalNamespace,
                ExternalName = x.ExternalName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            }));
            target.MemoryRecords.AddRange(archive.TrustedMemory.Select(x => new MemoryRecordRow
            {
                Id = x.Id,
                SourceClass = x.SourceClass,
                Content = x.Content,
                ProjectId = x.ProjectId,
                RepositoryId = x.RepositoryId,
                SourceReference = x.SourceReference,
                SupersededById = x.SupersededById,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            }));
            target.OrganizationItems.AddRange(AllOrganizationItems(archive).Select(x => new OrganizationItemRow
            {
                Id = x.Id,
                Kind = x.Kind,
                Title = x.Title,
                Content = x.Content,
                ProjectId = x.ProjectId,
                TaskStatus = x.TaskStatus,
                SourceReference = x.SourceReference,
                CreatedAtUnixMs = x.CreatedAtUnixMs,
                UpdatedAtUnixMs = x.UpdatedAtUnixMs,
                CompletedAtUnixMs = x.CompletedAtUnixMs,
            }));

            foreach (ConversationExport conversation in archive.Conversations)
            {
                target.Conversations.Add(new ConversationRow
                {
                    Id = conversation.Id,
                    OwnerPrincipalReference = conversation.OwnerPrincipalReference,
                    Title = conversation.Title,
                    ProjectAlias = conversation.ProjectAlias,
                    CreatedAtUnixMs = conversation.CreatedAtUnixMs,
                    UpdatedAtUnixMs = conversation.UpdatedAtUnixMs,
                });
                target.ConversationMessages.AddRange(conversation.Messages.Select(x => new ConversationMessageRow
                {
                    Id = x.Id,
                    ConversationId = conversation.Id,
                    Role = x.Role,
                    Content = x.Content,
                    CreatedAtUnixMs = x.CreatedAtUnixMs,
                    Sequence = x.Sequence,
                }));
            }

            target.ActionApprovals.AddRange(archive.Approvals.Select(x => new ActionApprovalRow
            {
                Id = x.Id,
                OwnerPrincipalReference = x.OwnerPrincipalReference,
                ActionName = x.ActionName,
                ProjectId = x.ProjectId,
                RepositoryId = x.RepositoryId,
                IntentFingerprint = x.IntentFingerprint,
                ApprovedAtUnixMs = x.ApprovedAtUnixMs,
                ExpiresAtUnixMs = x.ExpiresAtUnixMs,
                ConsumedAtUnixMs = x.ConsumedAtUnixMs,
                RevokedAtUnixMs = x.RevokedAtUnixMs ?? Math.Max(revokeAt, x.ApprovedAtUnixMs),
            }));
            target.CreateBranchProposals.AddRange(archive.Proposals.Select(x =>
            {
                bool wasPending = string.Equals(x.Status, "Pending", StringComparison.Ordinal);
                return new CreateBranchProposalRow
                {
                    Id = x.Id,
                    OwnerPrincipalReference = x.OwnerPrincipalReference,
                    ProjectId = x.ProjectId,
                    RepositoryId = x.RepositoryId,
                    RepositoryProvider = x.RepositoryProvider,
                    RepositoryNamespace = x.RepositoryNamespace,
                    RepositoryName = x.RepositoryName,
                    Branch = x.Branch,
                    SourceRef = x.SourceRef,
                    SourceSha = x.SourceSha,
                    IntentFingerprint = x.IntentFingerprint,
                    Status = wasPending ? "Cancelled" : x.Status,
                    CreatedAtUnixMs = x.CreatedAtUnixMs,
                    ExpiresAtUnixMs = x.ExpiresAtUnixMs,
                    DecidedAtUnixMs = wasPending
                        ? Math.Max(revokeAt, x.CreatedAtUnixMs)
                        : x.DecidedAtUnixMs,
                };
            }));
            target.AuditEvents.AddRange(archive.RetainedAudit.Select(x => new AuditEventRow
            {
                Id = x.Id,
                RunId = x.RunId,
                ActionId = x.ActionId,
                Kind = x.EventType,
                ActionName = x.ActionName,
                Outcome = x.Outcome,
                OccurredAtUnixMs = x.OccurredAtUnixMs,
                Detail = x.Detail,
            }));

            await target.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            target.ChangeTracker.Clear();
            throw;
        }
    }

    private static async Task<List<ConversationExport>> ExportConversationsAsync(
        CanonicalStateDbContext source,
        CancellationToken cancellationToken)
    {
        ConversationRow[] conversations = await source.Conversations.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        ConversationMessageRow[] messages = await source.ConversationMessages.AsNoTracking()
            .OrderBy(x => x.ConversationId)
            .ThenBy(x => x.Sequence)
            .ToArrayAsync(cancellationToken);

        return conversations.Select(conversation => new ConversationExport(
            conversation.Id,
            conversation.OwnerPrincipalReference,
            conversation.Title,
            conversation.ProjectAlias,
            conversation.CreatedAtUnixMs,
            conversation.UpdatedAtUnixMs,
            messages
                .Where(message => message.ConversationId == conversation.Id)
                .Select(message => new ConversationMessageExport(
                    message.Id,
                    message.Role,
                    message.Content,
                    message.CreatedAtUnixMs,
                    message.Sequence))
                .ToList()))
            .ToList();
    }

    private static async Task<bool> HasRowsAsync(
        CanonicalStateDbContext db,
        CancellationToken cancellationToken) =>
        await db.Projects.AnyAsync(cancellationToken)
        || await db.ProjectAliases.AnyAsync(cancellationToken)
        || await db.Repositories.AnyAsync(cancellationToken)
        || await db.MemoryRecords.AnyAsync(cancellationToken)
        || await db.OrganizationItems.AnyAsync(cancellationToken)
        || await db.Conversations.AnyAsync(cancellationToken)
        || await db.ConversationMessages.AnyAsync(cancellationToken)
        || await db.ActionApprovals.AnyAsync(cancellationToken)
        || await db.CreateBranchProposals.AnyAsync(cancellationToken)
        || await db.AuditEvents.AnyAsync(cancellationToken);

    private static IEnumerable<OrganizationItemExport> AllOrganizationItems(LogicalStateArchive archive) =>
        archive.Notes
            .Concat(archive.Decisions)
            .Concat(archive.Tasks)
            .Concat(archive.OtherOrganizationItems);

    private static void Validate(LogicalStateArchive archive)
    {
        if (archive.FormatVersion != CurrentFormatVersion)
        {
            return;
        }

        HashSet<Guid> projects = Unique(archive.Projects.Select(x => x.Id), "project");
        HashSet<Guid> repositories = Unique(archive.Repositories.Select(x => x.Id), "repository");
        if (archive.Aliases.Any(alias => string.IsNullOrWhiteSpace(alias.Alias)
            || string.IsNullOrWhiteSpace(alias.NormalizedAlias)
            || !string.Equals(ProjectAlias.Normalize(alias.Alias), alias.NormalizedAlias, StringComparison.Ordinal))
            || archive.Aliases.Select(alias => alias.NormalizedAlias).Distinct(StringComparer.Ordinal).Count() != archive.Aliases.Count)
        {
            throw new InvalidDataException("Archive contains invalid or duplicate aliases.");
        }

        if (archive.Repositories.Any(repository => !projects.Contains(repository.ProjectId)))
        {
            throw new InvalidDataException("Repository references an unknown project.");
        }

        if (archive.Aliases.Any(alias => !projects.Contains(alias.ProjectId)))
        {
            throw new InvalidDataException("Alias references an unknown project.");
        }

        Dictionary<Guid, Guid> repositoryProjects = archive.Repositories.ToDictionary(x => x.Id, x => x.ProjectId);
        HashSet<string> normalizedAliases = archive.Aliases
            .Select(alias => alias.NormalizedAlias)
            .ToHashSet(StringComparer.Ordinal);

        ValidateCanonicalDomain(archive);

        if (archive.TrustedMemory.Any(memory =>
            (memory.ProjectId is not null && !projects.Contains(memory.ProjectId.Value))
            || (memory.RepositoryId is not null
                && (!repositories.Contains(memory.RepositoryId.Value)
                    || memory.ProjectId is null
                    || repositoryProjects[memory.RepositoryId.Value] != memory.ProjectId.Value))))
        {
            throw new InvalidDataException("Memory references an unknown or inconsistent canonical ID.");
        }

        HashSet<Guid> memoryIds = Unique(archive.TrustedMemory.Select(x => x.Id), "memory");
        Dictionary<Guid, MemoryExport> memoryById = archive.TrustedMemory.ToDictionary(x => x.Id);
        foreach (MemoryExport memory in archive.TrustedMemory.Where(x => x.SupersededById is not null))
        {
            Guid successorId = memory.SupersededById!.Value;
            if (!memoryIds.Contains(successorId))
            {
                throw new InvalidDataException("Memory supersession references an unknown record.");
            }

            MemoryExport successor = memoryById[successorId];
            if (memory.ProjectId != successor.ProjectId || memory.RepositoryId != successor.RepositoryId)
            {
                throw new InvalidDataException("Memory correction history changes canonical scope.");
            }
        }

        Unique(archive.Approvals.Select(x => x.Id), "approval");
        Unique(archive.Proposals.Select(x => x.Id), "proposal");
        if (archive.Approvals.Any(approval =>
            !projects.Contains(approval.ProjectId)
            || !repositories.Contains(approval.RepositoryId)
            || repositoryProjects[approval.RepositoryId] != approval.ProjectId))
        {
            throw new InvalidDataException("Approval references an unknown or inconsistent canonical ID.");
        }

        if (archive.Proposals.Any(proposal =>
            !projects.Contains(proposal.ProjectId)
            || !repositories.Contains(proposal.RepositoryId)
            || repositoryProjects[proposal.RepositoryId] != proposal.ProjectId))
        {
            throw new InvalidDataException("Proposal references an unknown or inconsistent canonical ID.");
        }

        Unique(archive.Conversations.Select(x => x.Id), "conversation");
        Unique(archive.Conversations.SelectMany(x => x.Messages).Select(x => x.Id), "conversation message");
        foreach (ConversationExport conversation in archive.Conversations)
        {
            if (string.IsNullOrWhiteSpace(conversation.OwnerPrincipalReference)
                || string.IsNullOrWhiteSpace(conversation.Title)
                || conversation.UpdatedAtUnixMs < conversation.CreatedAtUnixMs)
            {
                throw new InvalidDataException("Conversation lifecycle is invalid.");
            }

            if (conversation.ProjectAlias is not null
                && (!normalizedAliases.Contains(ProjectAlias.Normalize(conversation.ProjectAlias))
                    || !string.Equals(conversation.ProjectAlias, ProjectAlias.Normalize(conversation.ProjectAlias), StringComparison.Ordinal)))
            {
                throw new InvalidDataException("Conversation references an unknown or non-normalized project alias.");
            }

            long[] sequences = conversation.Messages
                .Select(message => message.Sequence)
                .Order()
                .ToArray();
            long[] expected = Enumerable.Range(1, sequences.Length).Select(index => (long)index).ToArray();
            if (!sequences.SequenceEqual(expected)
                || conversation.Messages.Any(message =>
                    message.Role is not ("user" or "assistant")
                    || string.IsNullOrWhiteSpace(message.Content)
                    || message.CreatedAtUnixMs < conversation.CreatedAtUnixMs))
            {
                throw new InvalidDataException("Conversation messages are inconsistent.");
            }
        }

        Unique(AllOrganizationItems(archive).Select(x => x.Id), "organization item");
        if (AllOrganizationItems(archive).Any(item => item.ProjectId is not null && !projects.Contains(item.ProjectId.Value)))
        {
            throw new InvalidDataException("Organization item references an unknown project.");
        }

        long[] auditIds = archive.RetainedAudit.Select(x => x.Id).ToArray();
        if (auditIds.Any(id => id <= 0)
            || auditIds.Length != auditIds.Distinct().Count()
            || archive.RetainedAudit.Any(audit =>
                string.IsNullOrWhiteSpace(audit.RunId)
                || string.IsNullOrWhiteSpace(audit.ActionId)
                || string.IsNullOrWhiteSpace(audit.EventType)
                || string.IsNullOrWhiteSpace(audit.ActionName)
                || string.IsNullOrWhiteSpace(audit.Outcome)))
        {
            throw new InvalidDataException("Archive contains invalid or duplicate audit events.");
        }
    }

    private static void ValidateCanonicalDomain(LogicalStateArchive archive)
    {
        try
        {
            foreach (ProjectExport project in archive.Projects)
            {
                Project domain = new(
                    new ProjectId(project.Id),
                    project.Name,
                    archive.Aliases.Where(alias => alias.ProjectId == project.Id).Select(alias => alias.Alias),
                    project.CreatedAt,
                    project.UpdatedAt);
                if (!string.Equals(domain.Name, project.Name, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Project names must already be normalized.");
                }
            }

            foreach (RepositoryExport repository in archive.Repositories)
            {
                RepositoryLocator locator = new(
                    repository.Provider,
                    repository.ExternalNamespace,
                    repository.ExternalName);
                Loren.Core.Projects.Repository domain = new(
                    new RepositoryId(repository.Id),
                    new ProjectId(repository.ProjectId),
                    repository.Name,
                    locator,
                    repository.CreatedAt,
                    repository.UpdatedAt);
                if (!string.Equals(domain.Name, repository.Name, StringComparison.Ordinal)
                    || !string.Equals(locator.Provider, repository.Provider, StringComparison.Ordinal)
                    || !string.Equals(locator.ExternalNamespace, repository.ExternalNamespace, StringComparison.Ordinal)
                    || !string.Equals(locator.ExternalName, repository.ExternalName, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Repository fields must already be normalized.");
                }
            }

            foreach (MemoryExport memory in archive.TrustedMemory)
            {
                _ = new MemoryRecord(
                    new MemoryRecordId(memory.Id),
                    ParseMemorySourceClass(memory.SourceClass),
                    memory.Content,
                    memory.ProjectId is Guid projectId ? new ProjectId(projectId) : null,
                    memory.RepositoryId is Guid repositoryId ? new RepositoryId(repositoryId) : null,
                    memory.SourceReference,
                    memory.SupersededById is Guid supersededById ? new MemoryRecordId(supersededById) : null,
                    memory.CreatedAt,
                    memory.UpdatedAt);
            }

            foreach (ActionApprovalExport approval in archive.Approvals)
            {
                ActionApproval domain = new(
                    new ApprovalId(approval.Id),
                    approval.OwnerPrincipalReference,
                    approval.ActionName,
                    new ProjectId(approval.ProjectId),
                    new RepositoryId(approval.RepositoryId),
                    approval.IntentFingerprint,
                    DateTimeOffset.FromUnixTimeMilliseconds(approval.ApprovedAtUnixMs),
                    DateTimeOffset.FromUnixTimeMilliseconds(approval.ExpiresAtUnixMs),
                    approval.ConsumedAtUnixMs is long consumedAt ? DateTimeOffset.FromUnixTimeMilliseconds(consumedAt) : null,
                    approval.RevokedAtUnixMs is long revokedAt ? DateTimeOffset.FromUnixTimeMilliseconds(revokedAt) : null);
                if (!string.Equals(domain.OwnerPrincipalReference, approval.OwnerPrincipalReference, StringComparison.Ordinal)
                    || !string.Equals(domain.ActionName, approval.ActionName, StringComparison.Ordinal)
                    || !string.Equals(domain.IntentFingerprint, approval.IntentFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Approval fields must already be normalized.");
                }
            }

            foreach (CreateBranchProposalExport proposal in archive.Proposals)
            {
                CreateBranchProposalStatus status = proposal.Status switch
                {
                    "Pending" => CreateBranchProposalStatus.Pending,
                    "Approved" => CreateBranchProposalStatus.Approved,
                    "Cancelled" => CreateBranchProposalStatus.Cancelled,
                    _ => throw new InvalidDataException("Proposal contains an unknown status."),
                };
                RepositoryLocator locator = new(
                    proposal.RepositoryProvider,
                    proposal.RepositoryNamespace,
                    proposal.RepositoryName);
                CreateBranchProposal domain = new(
                    new CreateBranchProposalId(proposal.Id),
                    proposal.OwnerPrincipalReference,
                    new ProjectId(proposal.ProjectId),
                    new RepositoryId(proposal.RepositoryId),
                    locator,
                    proposal.Branch,
                    proposal.SourceRef,
                    proposal.SourceSha,
                    proposal.IntentFingerprint,
                    DateTimeOffset.FromUnixTimeMilliseconds(proposal.CreatedAtUnixMs),
                    DateTimeOffset.FromUnixTimeMilliseconds(proposal.ExpiresAtUnixMs),
                    status,
                    proposal.DecidedAtUnixMs is long decidedAt ? DateTimeOffset.FromUnixTimeMilliseconds(decidedAt) : null);
                if (!string.Equals(domain.OwnerPrincipalReference, proposal.OwnerPrincipalReference, StringComparison.Ordinal)
                    || !string.Equals(domain.RepositoryLocator.Provider, proposal.RepositoryProvider, StringComparison.Ordinal)
                    || !string.Equals(domain.RepositoryLocator.ExternalNamespace, proposal.RepositoryNamespace, StringComparison.Ordinal)
                    || !string.Equals(domain.RepositoryLocator.ExternalName, proposal.RepositoryName, StringComparison.Ordinal)
                    || !string.Equals(domain.Branch, proposal.Branch, StringComparison.Ordinal)
                    || !string.Equals(domain.SourceRef, proposal.SourceRef, StringComparison.Ordinal)
                    || !string.Equals(domain.SourceSha, proposal.SourceSha, StringComparison.Ordinal)
                    || !string.Equals(domain.IntentFingerprint, proposal.IntentFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Proposal fields must already be normalized.");
                }
            }

            foreach (OrganizationItemExport item in AllOrganizationItems(archive))
            {
                OrganizationItemKind kind = item.Kind switch
                {
                    "Note" => OrganizationItemKind.Note,
                    "Decision" => OrganizationItemKind.Decision,
                    "Task" => OrganizationItemKind.Task,
                    _ => throw new InvalidDataException("Organization item contains an unknown kind."),
                };
                OrganizationTaskStatus? taskStatus = item.TaskStatus switch
                {
                    null => null,
                    "Open" => OrganizationTaskStatus.Open,
                    "Completed" => OrganizationTaskStatus.Completed,
                    _ => throw new InvalidDataException("Organization task contains an unknown status."),
                };
                OrganizationItem domain = new(
                    new OrganizationItemId(item.Id),
                    kind,
                    item.Title,
                    item.Content,
                    item.ProjectId is Guid projectId ? new ProjectId(projectId) : null,
                    taskStatus,
                    item.SourceReference,
                    DateTimeOffset.FromUnixTimeMilliseconds(item.CreatedAtUnixMs),
                    DateTimeOffset.FromUnixTimeMilliseconds(item.UpdatedAtUnixMs),
                    item.CompletedAtUnixMs is long completedAt ? DateTimeOffset.FromUnixTimeMilliseconds(completedAt) : null);
                if (!string.Equals(domain.Title, item.Title, StringComparison.Ordinal)
                    || !string.Equals(domain.Content, item.Content, StringComparison.Ordinal)
                    || !string.Equals(domain.SourceReference, item.SourceReference, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Organization item fields must already be normalized.");
                }
            }
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("Recovery archive contains invalid domain state.", exception);
        }
    }

    private static MemorySourceClass ParseMemorySourceClass(string value) => value switch
    {
        "OWNER_EXPLICIT" => MemorySourceClass.OwnerExplicit,
        "OWNER_CORRECTION" => MemorySourceClass.OwnerCorrection,
        "VERIFIED_TOOL" => MemorySourceClass.VerifiedTool,
        "OWNER_APPROVED_INFERENCE" => MemorySourceClass.OwnerApprovedInference,
        _ => throw new InvalidDataException("Recovery archive contains an untrusted memory source class."),
    };

    private static HashSet<Guid> Unique(
        IEnumerable<Guid> ids,
        string label)
    {
        Guid[] values = ids.ToArray();
        if (values.Any(id => id == Guid.Empty) || values.Length != values.Distinct().Count())
        {
            throw new InvalidDataException($"Archive contains invalid or duplicate {label} IDs.");
        }

        return values.ToHashSet();
    }
}
