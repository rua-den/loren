using Loren.Core.Actions;
using Loren.Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.CanonicalState;

public sealed class SqliteCreateBranchProposalStore : ICreateBranchProposalStore
{
    private readonly CanonicalStateDbContext _dbContext;

    public SqliteCreateBranchProposalStore(CanonicalStateDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(CreateBranchProposal proposal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (proposal.Status != CreateBranchProposalStatus.Pending || proposal.DecidedAt is not null)
        {
            throw new ArgumentException("Only undecided Pending proposals can be added.", nameof(proposal));
        }
        await ValidateScopeAsync(proposal.ProjectId, proposal.RepositoryId, cancellationToken);
        _dbContext.CreateBranchProposals.Add(ToRow(proposal));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreateBranchProposal?> GetAsync(CreateBranchProposalId proposalId, CancellationToken cancellationToken = default)
    {
        CreateBranchProposalRow? row = await _dbContext.CreateBranchProposals.AsNoTracking()
            .SingleOrDefaultAsync(proposal => proposal.Id == proposalId.Value, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CreateBranchProposalDecisionResult> ApproveAsync(
        CreateBranchProposalDecisionRequest decision,
        ActionApproval approval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(approval);
        CreateBranchProposalRow? current = await _dbContext.CreateBranchProposals.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == decision.ProposalId.Value, cancellationToken);
        if (current is null)
        {
            return Result(CreateBranchProposalDecisionStatus.Unknown, null, "Proposal does not exist.");
        }
        if (!string.Equals(current.OwnerPrincipalReference, decision.OwnerPrincipalReference, StringComparison.Ordinal))
        {
            return Result(CreateBranchProposalDecisionStatus.OwnerMismatch, null, "Proposal belongs to another owner.");
        }
        if (!ApprovalMatches(current, decision, approval))
        {
            return Result(CreateBranchProposalDecisionStatus.Mismatch, ToDomain(current), "Approval does not match the frozen proposal.");
        }

        long decidedAt = decision.DecidedAt.ToUnixTimeMilliseconds();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            int updated = await _dbContext.CreateBranchProposals
                .Where(p => p.Id == decision.ProposalId.Value
                    && p.OwnerPrincipalReference == decision.OwnerPrincipalReference
                    && p.Status == nameof(CreateBranchProposalStatus.Pending)
                    && p.CreatedAtUnixMs <= decidedAt && p.ExpiresAtUnixMs > decidedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, nameof(CreateBranchProposalStatus.Approved))
                    .SetProperty(p => p.DecidedAtUnixMs, decidedAt), cancellationToken);
            if (updated != 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                _dbContext.ChangeTracker.Clear();
                return await ClassifyAsync(decision, cancellationToken);
            }

            _dbContext.Set<ActionApprovalRow>().Add(ToApprovalRow(approval));
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            CreateBranchProposal? result = await GetAsync(decision.ProposalId, cancellationToken);
            return Result(CreateBranchProposalDecisionStatus.Approved, result, "Proposal approved and one-time approval created atomically.");
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }
            throw;
        }
    }

    public async Task<CreateBranchProposalDecisionResult> CancelAsync(CreateBranchProposalDecisionRequest decision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        long decidedAt = decision.DecidedAt.ToUnixTimeMilliseconds();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            int updated = await _dbContext.CreateBranchProposals
                .Where(p => p.Id == decision.ProposalId.Value
                    && p.OwnerPrincipalReference == decision.OwnerPrincipalReference
                    && p.Status == nameof(CreateBranchProposalStatus.Pending)
                    && p.CreatedAtUnixMs <= decidedAt && p.ExpiresAtUnixMs > decidedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, nameof(CreateBranchProposalStatus.Cancelled))
                    .SetProperty(p => p.DecidedAtUnixMs, decidedAt), cancellationToken);
            if (updated != 1)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                _dbContext.ChangeTracker.Clear();
                return await ClassifyAsync(decision, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            return Result(CreateBranchProposalDecisionStatus.Cancelled,
                await GetAsync(decision.ProposalId, cancellationToken), "Proposal cancelled.");
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }
            throw;
        }
    }

    private async Task<CreateBranchProposalDecisionResult> ClassifyAsync(CreateBranchProposalDecisionRequest decision, CancellationToken cancellationToken)
    {
        CreateBranchProposalRow? row = await _dbContext.CreateBranchProposals.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == decision.ProposalId.Value, cancellationToken);
        if (row is null) return Result(CreateBranchProposalDecisionStatus.Unknown, null, "Proposal does not exist.");
        if (!string.Equals(row.OwnerPrincipalReference, decision.OwnerPrincipalReference, StringComparison.Ordinal))
            return Result(CreateBranchProposalDecisionStatus.OwnerMismatch, null, "Proposal belongs to another owner.");
        long at = decision.DecidedAt.ToUnixTimeMilliseconds();
        if (row.Status != nameof(CreateBranchProposalStatus.Pending))
            return Result(CreateBranchProposalDecisionStatus.AlreadyDecided, ToDomain(row), "Proposal was already decided.");
        if (row.ExpiresAtUnixMs <= at)
            return Result(CreateBranchProposalDecisionStatus.Expired, ToDomain(row), "Proposal expired before the decision.");
        if (row.CreatedAtUnixMs > at)
            return Result(CreateBranchProposalDecisionStatus.Mismatch, ToDomain(row), "Decision predates proposal creation.");
        return Result(CreateBranchProposalDecisionStatus.Mismatch, ToDomain(row), "Proposal decision conflicted with another transition.");
    }

    private static bool ApprovalMatches(CreateBranchProposalRow proposal, CreateBranchProposalDecisionRequest decision, ActionApproval approval) =>
        approval.OwnerPrincipalReference == proposal.OwnerPrincipalReference
        && approval.ActionName == CreateBranchProposal.ActionNameValue
        && approval.ProjectId.Value == proposal.ProjectId
        && approval.RepositoryId.Value == proposal.RepositoryId
        && approval.IntentFingerprint == proposal.IntentFingerprint
        && approval.ApprovedAt.ToUnixTimeMilliseconds() == decision.DecidedAt.ToUnixTimeMilliseconds()
        && approval.ExpiresAt.ToUnixTimeMilliseconds() == decision.DecidedAt.AddMinutes(5).ToUnixTimeMilliseconds()
        && approval.ConsumedAt is null && approval.RevokedAt is null;

    private async Task ValidateScopeAsync(ProjectId projectId, RepositoryId repositoryId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId.Value, cancellationToken))
            throw new InvalidOperationException($"Project '{projectId}' does not exist.");
        if (!await _dbContext.Repositories.AsNoTracking().AnyAsync(r => r.Id == repositoryId.Value && r.ProjectId == projectId.Value, cancellationToken))
            throw new InvalidOperationException($"Repository '{repositoryId}' does not belong to project '{projectId}'.");
    }

    private static CreateBranchProposalDecisionResult Result(CreateBranchProposalDecisionStatus status, CreateBranchProposal? proposal, string reason) => new(status, proposal, reason);

    private static CreateBranchProposalRow ToRow(CreateBranchProposal p) => new()
    {
        Id = p.Id.Value,
        OwnerPrincipalReference = p.OwnerPrincipalReference,
        ProjectId = p.ProjectId.Value,
        RepositoryId = p.RepositoryId.Value,
        RepositoryProvider = p.RepositoryLocator.Provider,
        RepositoryNamespace = p.RepositoryLocator.ExternalNamespace,
        RepositoryName = p.RepositoryLocator.ExternalName,
        Branch = p.Branch,
        SourceRef = p.SourceRef,
        SourceSha = p.SourceSha,
        IntentFingerprint = p.IntentFingerprint,
        Status = p.Status.ToString(),
        CreatedAtUnixMs = p.CreatedAt.ToUnixTimeMilliseconds(),
        ExpiresAtUnixMs = p.ExpiresAt.ToUnixTimeMilliseconds(),
        DecidedAtUnixMs = p.DecidedAt?.ToUnixTimeMilliseconds(),
    };

    private static ActionApprovalRow ToApprovalRow(ActionApproval a) => new()
    {
        Id = a.Id.Value,
        OwnerPrincipalReference = a.OwnerPrincipalReference,
        ActionName = a.ActionName,
        ProjectId = a.ProjectId.Value,
        RepositoryId = a.RepositoryId.Value,
        IntentFingerprint = a.IntentFingerprint,
        ApprovedAtUnixMs = a.ApprovedAt.ToUnixTimeMilliseconds(),
        ExpiresAtUnixMs = a.ExpiresAt.ToUnixTimeMilliseconds(),
    };

    private static CreateBranchProposal ToDomain(CreateBranchProposalRow p) => new(
        new CreateBranchProposalId(p.Id), p.OwnerPrincipalReference, new ProjectId(p.ProjectId), new RepositoryId(p.RepositoryId),
        new RepositoryLocator(p.RepositoryProvider, p.RepositoryNamespace, p.RepositoryName), p.Branch, p.SourceRef, p.SourceSha,
        p.IntentFingerprint, DateTimeOffset.FromUnixTimeMilliseconds(p.CreatedAtUnixMs), DateTimeOffset.FromUnixTimeMilliseconds(p.ExpiresAtUnixMs),
        Enum.Parse<CreateBranchProposalStatus>(p.Status), p.DecidedAtUnixMs is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(p.DecidedAtUnixMs.Value));
}
