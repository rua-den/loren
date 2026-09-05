using Loren.Core.Actions;
using Loren.Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.CanonicalState;

public sealed class SqliteActionApprovalStore : IActionApprovalStore
{
    private readonly CanonicalStateDbContext _dbContext;

    public SqliteActionApprovalStore(CanonicalStateDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(
        ActionApproval approval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);
        await ValidateScopeAsync(approval.ProjectId, approval.RepositoryId, cancellationToken);

        _dbContext.ActionApprovals.Add(ToRow(approval));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ActionApproval?> GetAsync(
        ApprovalId approvalId,
        CancellationToken cancellationToken = default)
    {
        ActionApprovalRow? row = await _dbContext.ActionApprovals
            .AsNoTracking()
            .SingleOrDefaultAsync(
                approval => approval.Id == approvalId.Value,
                cancellationToken);

        return row is null ? null : ToDomain(row);
    }

    public async Task<ApprovalConsumptionResult> ConsumeAsync(
        ApprovalConsumptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OwnerPrincipalReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IntentFingerprint);

        long consumedAtUnixMs = request.ConsumedAt.ToUnixTimeMilliseconds();
        int updated = await _dbContext.ActionApprovals
            .Where(approval =>
                approval.Id == request.ApprovalId.Value
                && approval.OwnerPrincipalReference == request.OwnerPrincipalReference
                && approval.ActionName == request.ActionName
                && approval.ProjectId == request.ProjectId.Value
                && approval.RepositoryId == request.RepositoryId.Value
                && approval.IntentFingerprint == request.IntentFingerprint
                && approval.ConsumedAtUnixMs == null
                && approval.RevokedAtUnixMs == null
                && approval.ApprovedAtUnixMs <= consumedAtUnixMs
                && approval.ExpiresAtUnixMs > consumedAtUnixMs)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    approval => approval.ConsumedAtUnixMs,
                    consumedAtUnixMs),
                cancellationToken);

        _dbContext.ChangeTracker.Clear();

        if (updated == 1)
        {
            return new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.Consumed,
                "Exact owner approval was consumed for this action intent.");
        }

        ActionApprovalRow? row = await _dbContext.ActionApprovals
            .AsNoTracking()
            .SingleOrDefaultAsync(
                approval => approval.Id == request.ApprovalId.Value,
                cancellationToken);

        if (row is null)
        {
            return new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.Unknown,
                "Approval does not exist.");
        }

        if (!MatchesIntent(row, request))
        {
            return new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.Mismatch,
                "Approval does not match the exact normalized action intent.");
        }

        if (row.RevokedAtUnixMs is not null)
        {
            return new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.Revoked,
                "Approval was revoked before execution.");
        }

        if (row.ConsumedAtUnixMs is not null)
        {
            return new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.AlreadyConsumed,
                "Approval has already been consumed and cannot be replayed.");
        }

        if (row.ExpiresAtUnixMs <= consumedAtUnixMs)
        {
            return new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.Expired,
                "Approval expired before execution.");
        }

        return new ApprovalConsumptionResult(
            ApprovalConsumptionStatus.Mismatch,
            "Approval is not valid at the requested consumption time.");
    }

    public async Task RevokeAsync(
        ApprovalId approvalId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        long revokedAtUnixMs = revokedAt.ToUnixTimeMilliseconds();
        int updated = await _dbContext.ActionApprovals
            .Where(approval =>
                approval.Id == approvalId.Value
                && approval.ConsumedAtUnixMs == null
                && approval.RevokedAtUnixMs == null
                && approval.ApprovedAtUnixMs <= revokedAtUnixMs)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    approval => approval.RevokedAtUnixMs,
                    revokedAtUnixMs),
                cancellationToken);

        _dbContext.ChangeTracker.Clear();

        if (updated == 1)
        {
            return;
        }

        ActionApprovalRow? row = await _dbContext.ActionApprovals
            .AsNoTracking()
            .SingleOrDefaultAsync(
                approval => approval.Id == approvalId.Value,
                cancellationToken);

        if (row is null)
        {
            throw new InvalidOperationException(
                $"Approval '{approvalId}' does not exist.");
        }

        if (row.ConsumedAtUnixMs is not null)
        {
            throw new InvalidOperationException(
                $"Approval '{approvalId}' is already consumed and cannot be revoked retroactively.");
        }

        if (row.RevokedAtUnixMs is not null)
        {
            throw new InvalidOperationException(
                $"Approval '{approvalId}' is already revoked.");
        }

        throw new InvalidOperationException(
            $"Approval '{approvalId}' cannot be revoked before it was approved.");
    }

    private async Task ValidateScopeAsync(
        ProjectId projectId,
        RepositoryId repositoryId,
        CancellationToken cancellationToken)
    {
        bool projectExists = await _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == projectId.Value, cancellationToken);

        if (!projectExists)
        {
            throw new InvalidOperationException(
                $"Project '{projectId}' does not exist.");
        }

        bool repositoryBelongsToProject = await _dbContext.Repositories
            .AsNoTracking()
            .AnyAsync(
                repository => repository.Id == repositoryId.Value
                    && repository.ProjectId == projectId.Value,
                cancellationToken);

        if (!repositoryBelongsToProject)
        {
            throw new InvalidOperationException(
                $"Repository '{repositoryId}' does not belong to project '{projectId}'.");
        }
    }

    private static bool MatchesIntent(
        ActionApprovalRow row,
        ApprovalConsumptionRequest request) =>
        row.OwnerPrincipalReference == request.OwnerPrincipalReference
        && row.ActionName == request.ActionName
        && row.ProjectId == request.ProjectId.Value
        && row.RepositoryId == request.RepositoryId.Value
        && row.IntentFingerprint == request.IntentFingerprint;

    private static ActionApprovalRow ToRow(ActionApproval approval) => new()
    {
        Id = approval.Id.Value,
        OwnerPrincipalReference = approval.OwnerPrincipalReference,
        ActionName = approval.ActionName,
        ProjectId = approval.ProjectId.Value,
        RepositoryId = approval.RepositoryId.Value,
        IntentFingerprint = approval.IntentFingerprint,
        ApprovedAtUnixMs = approval.ApprovedAt.ToUnixTimeMilliseconds(),
        ExpiresAtUnixMs = approval.ExpiresAt.ToUnixTimeMilliseconds(),
        ConsumedAtUnixMs = approval.ConsumedAt?.ToUnixTimeMilliseconds(),
        RevokedAtUnixMs = approval.RevokedAt?.ToUnixTimeMilliseconds(),
    };

    private static ActionApproval ToDomain(ActionApprovalRow row) => new(
        new ApprovalId(row.Id),
        row.OwnerPrincipalReference,
        row.ActionName,
        new ProjectId(row.ProjectId),
        new RepositoryId(row.RepositoryId),
        row.IntentFingerprint,
        DateTimeOffset.FromUnixTimeMilliseconds(row.ApprovedAtUnixMs),
        DateTimeOffset.FromUnixTimeMilliseconds(row.ExpiresAtUnixMs),
        row.ConsumedAtUnixMs is null
            ? null
            : DateTimeOffset.FromUnixTimeMilliseconds(row.ConsumedAtUnixMs.Value),
        row.RevokedAtUnixMs is null
            ? null
            : DateTimeOffset.FromUnixTimeMilliseconds(row.RevokedAtUnixMs.Value));
}
