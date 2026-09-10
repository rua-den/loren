using System.Text.Json;
using System.Text.Json.Serialization;
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

    public static async Task ExportAsync(CanonicalStateDbContext source, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction snapshot = await source.Database.BeginTransactionAsync(cancellationToken);
        LogicalStateArchive archive = new()
        {
            Projects = await source.Projects.AsNoTracking().OrderBy(x => x.Id).Select(x => new ProjectExport(x.Id, x.Name, x.CreatedAt, x.UpdatedAt)).ToListAsync(cancellationToken),
            Aliases = await source.ProjectAliases.AsNoTracking().OrderBy(x => x.NormalizedAlias).Select(x => new ProjectAliasExport(x.Alias, x.NormalizedAlias, x.ProjectId)).ToListAsync(cancellationToken),
            Repositories = await source.Repositories.AsNoTracking().OrderBy(x => x.Id).Select(x => new RepositoryExport(x.Id, x.ProjectId, x.Name, x.Provider, x.ExternalNamespace, x.ExternalName, x.CreatedAt, x.UpdatedAt)).ToListAsync(cancellationToken),
            TrustedMemory = await source.MemoryRecords.AsNoTracking().Where(x => x.SourceClass == "OwnerExplicit" || x.SourceClass == "OwnerCorrection" || x.SourceClass == "VerifiedTool" || x.SourceClass == "OwnerApprovedInference").OrderBy(x => x.Id).Select(x => new MemoryExport(x.Id, x.SourceClass, x.Content, x.ProjectId, x.RepositoryId, x.SourceReference, x.SupersededById, x.CreatedAt, x.UpdatedAt)).ToListAsync(cancellationToken),
            Conversations = await ExportConversationsAsync(source, cancellationToken),
            Approvals = await source.ActionApprovals.AsNoTracking().OrderBy(x => x.Id).Select(x => new ActionApprovalExport(x.Id, x.OwnerPrincipalReference, x.ActionName, x.ProjectId, x.RepositoryId, x.IntentFingerprint, x.ApprovedAtUnixMs, x.ExpiresAtUnixMs, x.ConsumedAtUnixMs, x.RevokedAtUnixMs)).ToListAsync(cancellationToken),
            Proposals = await source.CreateBranchProposals.AsNoTracking().OrderBy(x => x.Id).Select(x => new CreateBranchProposalExport(x.Id, x.OwnerPrincipalReference, x.ProjectId, x.RepositoryId, x.RepositoryProvider, x.RepositoryNamespace, x.RepositoryName, x.Branch, x.SourceRef, x.SourceSha, x.IntentFingerprint, x.Status, x.CreatedAtUnixMs, x.ExpiresAtUnixMs, x.DecidedAtUnixMs)).ToListAsync(cancellationToken),
            RetainedAudit = await source.AuditEvents.AsNoTracking().OrderBy(x => x.Id).Select(x => new RetainedAuditExport(x.Id, x.RunId, x.ActionId, x.Kind, x.ActionName, x.Outcome, x.OccurredAtUnixMs, x.Detail)).ToListAsync(cancellationToken),
        };
        List<OrganizationItemExport> organizationItems = await source.OrganizationItems.AsNoTracking().OrderBy(x => x.Id).Select(x => new OrganizationItemExport(x.Id, x.Kind, x.Title, x.Content, x.ProjectId, x.TaskStatus, x.SourceReference, x.CreatedAtUnixMs, x.UpdatedAtUnixMs, x.CompletedAtUnixMs)).ToListAsync(cancellationToken);
        archive = archive with
        {
            Notes = organizationItems.Where(x => x.Kind.Equals("note", StringComparison.OrdinalIgnoreCase)).ToList(),
            Decisions = organizationItems.Where(x => x.Kind.Equals("decision", StringComparison.OrdinalIgnoreCase)).ToList(),
            Tasks = organizationItems.Where(x => x.Kind.Equals("task", StringComparison.OrdinalIgnoreCase)).ToList(),
            OtherOrganizationItems = organizationItems.Where(x => !x.Kind.Equals("note", StringComparison.OrdinalIgnoreCase) && !x.Kind.Equals("decision", StringComparison.OrdinalIgnoreCase) && !x.Kind.Equals("task", StringComparison.OrdinalIgnoreCase)).ToList(),
        };
        await snapshot.CommitAsync(cancellationToken);
        await JsonSerializer.SerializeAsync(destination, archive, JsonOptions, cancellationToken);
    }

    public static async Task RestoreAsync(CanonicalStateDbContext target, Stream source, DateTimeOffset? restoredAt = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        using JsonDocument document = await JsonDocument.ParseAsync(source, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("format_version", out JsonElement version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out _))
            throw new InvalidDataException("Recovery archive must declare integer format_version.");
        LogicalStateArchive archive = document.RootElement.Deserialize<LogicalStateArchive>(JsonOptions)
            ?? throw new InvalidDataException("Recovery archive is empty.");
        Validate(archive);
        if (archive.FormatVersion != CurrentFormatVersion) throw new InvalidDataException($"Unsupported recovery format version '{archive.FormatVersion}'.");
        if (await HasRowsAsync(target, cancellationToken)) throw new InvalidOperationException("Recovery target must be a fresh empty database.");
        long revokeAt = (restoredAt ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await target.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            target.Projects.AddRange(archive.Projects.Select(x => new ProjectRow { Id = x.Id, Name = x.Name, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt }));
            target.ProjectAliases.AddRange(archive.Aliases.Select(x => new ProjectAliasRow { Alias = x.Alias, NormalizedAlias = x.NormalizedAlias, ProjectId = x.ProjectId }));
            target.Repositories.AddRange(archive.Repositories.Select(x => new RepositoryRow { Id = x.Id, ProjectId = x.ProjectId, Name = x.Name, Provider = x.Provider, ExternalNamespace = x.ExternalNamespace, ExternalName = x.ExternalName, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt }));
            target.MemoryRecords.AddRange(archive.TrustedMemory.Select(x => new MemoryRecordRow { Id = x.Id, SourceClass = x.SourceClass, Content = x.Content, ProjectId = x.ProjectId, RepositoryId = x.RepositoryId, SourceReference = x.SourceReference, SupersededById = x.SupersededById, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt }));
            target.OrganizationItems.AddRange(AllOrganizationItems(archive).Select(x => new OrganizationItemRow { Id = x.Id, Kind = x.Kind, Title = x.Title, Content = x.Content, ProjectId = x.ProjectId, TaskStatus = x.TaskStatus, SourceReference = x.SourceReference, CreatedAtUnixMs = x.CreatedAtUnixMs, UpdatedAtUnixMs = x.UpdatedAtUnixMs, CompletedAtUnixMs = x.CompletedAtUnixMs }));
            foreach (ConversationExport conversation in archive.Conversations)
            {
                target.Conversations.Add(new ConversationRow { Id = conversation.Id, OwnerPrincipalReference = conversation.OwnerPrincipalReference, Title = conversation.Title, ProjectAlias = conversation.ProjectAlias, CreatedAtUnixMs = conversation.CreatedAtUnixMs, UpdatedAtUnixMs = conversation.UpdatedAtUnixMs });
                target.ConversationMessages.AddRange(conversation.Messages.Select(x => new ConversationMessageRow { Id = x.Id, ConversationId = conversation.Id, Role = x.Role, Content = x.Content, CreatedAtUnixMs = x.CreatedAtUnixMs, Sequence = x.Sequence }));
            }
            target.ActionApprovals.AddRange(archive.Approvals.Select(x => new ActionApprovalRow { Id = x.Id, OwnerPrincipalReference = x.OwnerPrincipalReference, ActionName = x.ActionName, ProjectId = x.ProjectId, RepositoryId = x.RepositoryId, IntentFingerprint = x.IntentFingerprint, ApprovedAtUnixMs = x.ApprovedAtUnixMs, ExpiresAtUnixMs = x.ExpiresAtUnixMs, ConsumedAtUnixMs = x.ConsumedAtUnixMs, RevokedAtUnixMs = x.RevokedAtUnixMs ?? Math.Max(revokeAt, x.ApprovedAtUnixMs) }));
            target.CreateBranchProposals.AddRange(archive.Proposals.Select(x => new CreateBranchProposalRow { Id = x.Id, OwnerPrincipalReference = x.OwnerPrincipalReference, ProjectId = x.ProjectId, RepositoryId = x.RepositoryId, RepositoryProvider = x.RepositoryProvider, RepositoryNamespace = x.RepositoryNamespace, RepositoryName = x.RepositoryName, Branch = x.Branch, SourceRef = x.SourceRef, SourceSha = x.SourceSha, IntentFingerprint = x.IntentFingerprint, Status = string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase) ? "Cancelled" : x.Status, CreatedAtUnixMs = x.CreatedAtUnixMs, ExpiresAtUnixMs = x.ExpiresAtUnixMs, DecidedAtUnixMs = string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase) ? x.DecidedAtUnixMs ?? revokeAt : x.DecidedAtUnixMs }));
            target.AuditEvents.AddRange(archive.RetainedAudit.Select(x => new AuditEventRow { Id = x.Id, RunId = x.RunId, ActionId = x.ActionId, Kind = x.EventType, ActionName = x.ActionName, Outcome = x.Outcome, OccurredAtUnixMs = x.OccurredAtUnixMs, Detail = x.Detail }));
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

    private static async Task<List<ConversationExport>> ExportConversationsAsync(CanonicalStateDbContext source, CancellationToken cancellationToken)
    {
        ConversationRow[] conversations = await source.Conversations.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(cancellationToken);
        ConversationMessageRow[] messages = await source.ConversationMessages.AsNoTracking().OrderBy(x => x.ConversationId).ThenBy(x => x.Sequence).ToArrayAsync(cancellationToken);
        return conversations.Select(c => new ConversationExport(c.Id, c.OwnerPrincipalReference, c.Title, c.ProjectAlias, c.CreatedAtUnixMs, c.UpdatedAtUnixMs, messages.Where(m => m.ConversationId == c.Id).Select(m => new ConversationMessageExport(m.Id, m.Role, m.Content, m.CreatedAtUnixMs, m.Sequence)).ToList())).ToList();
    }

    private static async Task<bool> HasRowsAsync(CanonicalStateDbContext db, CancellationToken ct) =>
        await db.Projects.AnyAsync(ct) || await db.ProjectAliases.AnyAsync(ct) || await db.Repositories.AnyAsync(ct) || await db.MemoryRecords.AnyAsync(ct) || await db.OrganizationItems.AnyAsync(ct) || await db.Conversations.AnyAsync(ct) || await db.ConversationMessages.AnyAsync(ct) || await db.ActionApprovals.AnyAsync(ct) || await db.CreateBranchProposals.AnyAsync(ct) || await db.AuditEvents.AnyAsync(ct);

    private static IEnumerable<OrganizationItemExport> AllOrganizationItems(LogicalStateArchive a) => a.Notes.Concat(a.Decisions).Concat(a.Tasks).Concat(a.OtherOrganizationItems);

    private static void Validate(LogicalStateArchive a)
    {
        if (a.FormatVersion != CurrentFormatVersion) return;
        HashSet<Guid> projects = Unique(a.Projects.Select(x => x.Id), "project");
        HashSet<Guid> repositories = Unique(a.Repositories.Select(x => x.Id), "repository");
        Unique(a.Aliases.Select(x => x.ProjectId), "alias project reference", allowDuplicates: true);
        if (a.Aliases.Any(x => string.IsNullOrWhiteSpace(x.Alias) || string.IsNullOrWhiteSpace(x.NormalizedAlias) || !string.Equals(ProjectAlias.Normalize(x.Alias), x.NormalizedAlias, StringComparison.Ordinal)) || a.Aliases.Select(x => x.NormalizedAlias).Distinct(StringComparer.Ordinal).Count() != a.Aliases.Count)
            throw new InvalidDataException("Archive contains invalid or duplicate aliases.");
        if (a.Repositories.Any(x => !projects.Contains(x.ProjectId))) throw new InvalidDataException("Repository references an unknown project.");
        if (a.Aliases.Any(x => !projects.Contains(x.ProjectId))) throw new InvalidDataException("Alias references an unknown project.");
        Dictionary<Guid, Guid> repositoryProjects = a.Repositories.ToDictionary(x => x.Id, x => x.ProjectId);
        if (a.TrustedMemory.Any(x => (x.ProjectId is not null && !projects.Contains(x.ProjectId.Value)) || (x.RepositoryId is not null && (!repositories.Contains(x.RepositoryId.Value) || (x.ProjectId is not null && repositoryProjects[x.RepositoryId.Value] != x.ProjectId.Value))))) throw new InvalidDataException("Memory references an unknown or inconsistent canonical ID.");
        HashSet<Guid> memory = Unique(a.TrustedMemory.Select(x => x.Id), "memory");
        if (a.TrustedMemory.Any(x => x.SupersededById is not null && !memory.Contains(x.SupersededById.Value))) throw new InvalidDataException("Memory supersession references an unknown record.");
        Unique(a.Approvals.Select(x => x.Id), "approval");
        Unique(a.Proposals.Select(x => x.Id), "proposal");
        if (a.Approvals.Any(x => !projects.Contains(x.ProjectId) || !repositories.Contains(x.RepositoryId) || repositoryProjects[x.RepositoryId] != x.ProjectId)) throw new InvalidDataException("Approval references an unknown or inconsistent canonical ID.");
        if (a.Proposals.Any(x => !projects.Contains(x.ProjectId) || !repositories.Contains(x.RepositoryId) || repositoryProjects[x.RepositoryId] != x.ProjectId)) throw new InvalidDataException("Proposal references an unknown or inconsistent canonical ID.");
        if (a.Proposals.Any(x => x.Status is not ("Pending" or "Approved" or "Cancelled"))) throw new InvalidDataException("Proposal contains an unknown status.");
        Unique(a.Conversations.Select(x => x.Id), "conversation");
        Unique(a.Conversations.SelectMany(x => x.Messages).Select(x => x.Id), "conversation message");
        if (a.Conversations.Any(x => x.Messages.GroupBy(m => m.Sequence).Any(g => g.Count() != 1) || x.Messages.Any(m => m.Sequence < 0 || string.IsNullOrWhiteSpace(m.Role) || string.IsNullOrWhiteSpace(m.Content)))) throw new InvalidDataException("Conversation messages are inconsistent.");
        Unique(AllOrganizationItems(a).Select(x => x.Id), "organization item");
        if (AllOrganizationItems(a).Any(x => x.ProjectId is not null && !projects.Contains(x.ProjectId.Value))) throw new InvalidDataException("Organization item references an unknown project.");
        long[] auditIds = a.RetainedAudit.Select(x => x.Id).ToArray();
        if (auditIds.Any(x => x <= 0) || auditIds.Length != auditIds.Distinct().Count()) throw new InvalidDataException("Archive contains invalid or duplicate audit event IDs.");
    }

    private static HashSet<Guid> Unique(IEnumerable<Guid> ids, string label, bool allowDuplicates = false)
    {
        Guid[] values = ids.ToArray();
        if (values.Any(x => x == Guid.Empty) || (!allowDuplicates && values.Length != values.Distinct().Count())) throw new InvalidDataException($"Archive contains invalid or duplicate {label} IDs.");
        return values.ToHashSet();
    }
}
