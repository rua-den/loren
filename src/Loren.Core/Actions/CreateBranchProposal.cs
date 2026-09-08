using Loren.Core.Projects;

namespace Loren.Core.Actions;

public readonly record struct CreateBranchProposalId
{
    public CreateBranchProposalId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Create-branch proposal ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static CreateBranchProposalId New() => new(Guid.NewGuid());

    public static CreateBranchProposalId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString("N");
}

public enum CreateBranchProposalStatus
{
    Pending,
    Approved,
    Cancelled,
}

public sealed record CreateBranchProposal
{
    public const string ActionNameValue = "github.create_branch";

    public CreateBranchProposal(
        CreateBranchProposalId id,
        string ownerPrincipalReference,
        ProjectId projectId,
        RepositoryId repositoryId,
        RepositoryLocator repositoryLocator,
        string branch,
        string sourceRef,
        string sourceSha,
        string intentFingerprint,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CreateBranchProposalStatus status = CreateBranchProposalStatus.Pending,
        DateTimeOffset? decidedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPrincipalReference);
        ArgumentNullException.ThrowIfNull(repositoryLocator);
        ArgumentException.ThrowIfNullOrWhiteSpace(intentFingerprint);

        if (id.Value == Guid.Empty || projectId.Value == Guid.Empty || repositoryId.Value == Guid.Empty)
            throw new ArgumentException("Proposal identifiers cannot be empty.");
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));

        OwnerPrincipalReference = ownerPrincipalReference.Trim();
        Branch = NormalizeRef(branch, nameof(branch), allowRefsPrefix: false);
        SourceRef = NormalizeSourceRef(sourceRef);
        SourceSha = NormalizeSha(sourceSha);
        IntentFingerprint = intentFingerprint.Trim();

        if (expiresAt != createdAt.AddMinutes(5))
        {
            throw new ArgumentException("Proposal expiry must be exactly five minutes after creation.", nameof(expiresAt));
        }

        if (status == CreateBranchProposalStatus.Pending && decidedAt is not null)
        {
            throw new ArgumentException("Pending proposals cannot have a decision time.", nameof(decidedAt));
        }

        if (status != CreateBranchProposalStatus.Pending && decidedAt is null)
        {
            throw new ArgumentException("Terminal proposals require a decision time.", nameof(decidedAt));
        }

        if (decidedAt is not null && decidedAt < createdAt)
        {
            throw new ArgumentException("Decision time cannot precede creation.", nameof(decidedAt));
        }

        Id = id;
        ActionName = ActionNameValue;
        AccessClass = ActionAccessClass.ReversibleWrite;
        ProjectId = projectId;
        RepositoryId = repositoryId;
        RepositoryLocator = repositoryLocator;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        Status = status;
        DecidedAt = decidedAt;
    }

    public CreateBranchProposalId Id { get; }
    public string OwnerPrincipalReference { get; }
    public ProjectId ProjectId { get; }
    public RepositoryId RepositoryId { get; }
    public RepositoryLocator RepositoryLocator { get; }
    public string Branch { get; }
    public string SourceRef { get; }
    public string SourceSha { get; }
    public string IntentFingerprint { get; }
    public ActionAccessClass AccessClass { get; }
    public string ActionName { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public CreateBranchProposalStatus Status { get; }
    public DateTimeOffset? DecidedAt { get; }

    private static string NormalizeRef(string value, string parameterName, bool allowRefsPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string trimmed = value.Trim();
        if (!string.Equals(value, trimmed, StringComparison.Ordinal)
            || trimmed.Length > 255
            || trimmed.StartsWith('-')
            || trimmed.StartsWith('/')
            || trimmed.EndsWith('/')
            || trimmed.EndsWith('.')
            || trimmed.Contains("..", StringComparison.Ordinal)
            || trimmed.Contains("//", StringComparison.Ordinal)
            || (!allowRefsPrefix && trimmed.StartsWith("refs/", StringComparison.Ordinal))
            || trimmed.Contains("@{", StringComparison.Ordinal)
            || trimmed.Any(char.IsWhiteSpace)
            || trimmed.Any(c => char.IsControl(c) || c is '~' or '^' or ':' or '?' or '*' or '[' or '\\'))
        {
            throw new ArgumentException("Git ref is unsafe or not normalized.", parameterName);
        }

        foreach (string component in trimmed.Split('/'))
        {
            if (component.Length == 0 || component.StartsWith('.') || component.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Git ref is unsafe or not normalized.", parameterName);
        }

        return trimmed;
    }

    private static string NormalizeSourceRef(string value)
    {
        string normalized = NormalizeRef(value, nameof(value), allowRefsPrefix: true);
        if (!normalized.StartsWith("refs/heads/", StringComparison.Ordinal))
            throw new ArgumentException("Source ref must be a canonical refs/heads ref.", nameof(value));
        NormalizeRef(normalized["refs/heads/".Length..], nameof(value), allowRefsPrefix: false);
        return normalized;
    }

    private static string NormalizeSha(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Trim().ToLowerInvariant();
        if (value.Trim() != value
            || normalized.Length != 40
            || normalized.Any(c => c is < '0' or > '9' and < 'a' or > 'f'))
        {
            throw new ArgumentException("Source SHA must be exactly 40 hexadecimal characters.", nameof(value));
        }

        return normalized;
    }
}

public sealed record CreateBranchProposalDecisionRequest(
    CreateBranchProposalId ProposalId,
    string OwnerPrincipalReference,
    DateTimeOffset DecidedAt);

public enum CreateBranchProposalDecisionStatus
{
    Approved,
    Cancelled,
    Unknown,
    OwnerMismatch,
    Expired,
    AlreadyDecided,
    Mismatch,
}

public sealed record CreateBranchProposalDecisionResult(
    CreateBranchProposalDecisionStatus Status,
    CreateBranchProposal? Proposal,
    string Reason)
{
    public bool IsSuccessful => Status is CreateBranchProposalDecisionStatus.Approved
        or CreateBranchProposalDecisionStatus.Cancelled;
}

public interface ICreateBranchProposalStore
{
    Task AddAsync(CreateBranchProposal proposal, CancellationToken cancellationToken = default);

    Task<CreateBranchProposal?> GetAsync(
        CreateBranchProposalId proposalId,
        CancellationToken cancellationToken = default);

    Task<CreateBranchProposalDecisionResult> ApproveAsync(
        CreateBranchProposalDecisionRequest decision,
        ActionApproval approval,
        CancellationToken cancellationToken = default);

    Task<CreateBranchProposalDecisionResult> CancelAsync(
        CreateBranchProposalDecisionRequest decision,
        CancellationToken cancellationToken = default);
}
