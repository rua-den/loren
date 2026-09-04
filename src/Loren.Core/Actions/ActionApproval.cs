using Loren.Core.Projects;

namespace Loren.Core.Actions;

public readonly record struct ApprovalId
{
    public ApprovalId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Approval ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static ApprovalId New() => new(Guid.NewGuid());

    public static ApprovalId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString("N");
}

public sealed record ActionApproval
{
    public ActionApproval(
        ApprovalId id,
        string ownerPrincipalReference,
        string actionName,
        ProjectId projectId,
        RepositoryId repositoryId,
        string intentFingerprint,
        DateTimeOffset approvedAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? consumedAt = null,
        DateTimeOffset? revokedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPrincipalReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(intentFingerprint);

        if (expiresAt <= approvedAt)
        {
            throw new ArgumentException(
                "Approval expiry must be later than approval time.",
                nameof(expiresAt));
        }

        if (consumedAt is not null && consumedAt < approvedAt)
        {
            throw new ArgumentException(
                "Approval consumption cannot predate approval.",
                nameof(consumedAt));
        }

        if (revokedAt is not null && revokedAt < approvedAt)
        {
            throw new ArgumentException(
                "Approval revocation cannot predate approval.",
                nameof(revokedAt));
        }

        Id = id;
        OwnerPrincipalReference = ownerPrincipalReference.Trim();
        ActionName = actionName.Trim();
        ProjectId = projectId;
        RepositoryId = repositoryId;
        IntentFingerprint = intentFingerprint.Trim();
        ApprovedAt = approvedAt;
        ExpiresAt = expiresAt;
        ConsumedAt = consumedAt;
        RevokedAt = revokedAt;
    }

    public ApprovalId Id { get; }

    public string OwnerPrincipalReference { get; }

    public string ActionName { get; }

    public ProjectId ProjectId { get; }

    public RepositoryId RepositoryId { get; }

    public string IntentFingerprint { get; }

    public DateTimeOffset ApprovedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public DateTimeOffset? ConsumedAt { get; }

    public DateTimeOffset? RevokedAt { get; }
}

public enum ApprovalConsumptionStatus
{
    Consumed,
    Unknown,
    Expired,
    Revoked,
    AlreadyConsumed,
    Mismatch,
}

public sealed record ApprovalConsumptionRequest(
    ApprovalId ApprovalId,
    string OwnerPrincipalReference,
    string ActionName,
    ProjectId ProjectId,
    RepositoryId RepositoryId,
    string IntentFingerprint,
    DateTimeOffset ConsumedAt);

public sealed record ApprovalConsumptionResult(
    ApprovalConsumptionStatus Status,
    string Reason)
{
    public bool IsConsumed => Status is ApprovalConsumptionStatus.Consumed;
}

public interface IActionApprovalStore
{
    Task AddAsync(
        ActionApproval approval,
        CancellationToken cancellationToken = default);

    Task<ActionApproval?> GetAsync(
        ApprovalId approvalId,
        CancellationToken cancellationToken = default);

    Task<ApprovalConsumptionResult> ConsumeAsync(
        ApprovalConsumptionRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        ApprovalId approvalId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default);
}
