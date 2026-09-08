using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Tools.GitHub;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.Web;

public sealed class LorenOwnerProjectBootstrapService
{
    private readonly IProjectCatalog _projectCatalog;

    public LorenOwnerProjectBootstrapService(IProjectCatalog projectCatalog)
    {
        _projectCatalog = projectCatalog ?? throw new ArgumentNullException(nameof(projectCatalog));
    }

    public async Task<OwnerProjectBootstrapResult> BootstrapGitHubProjectAsync(
        string projectName,
        string projectAlias,
        string githubOwner,
        string githubRepository,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectAlias);
        ArgumentException.ThrowIfNullOrWhiteSpace(githubOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(githubRepository);

        ProjectSnapshot? existing = await _projectCatalog.FindByAliasAsync(
            projectAlias,
            cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Project alias '{projectAlias}' is already configured. Bootstrap refuses to rebind an existing alias.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        Project project = new(
            projectId,
            projectName,
            [projectAlias],
            now,
            now);
        CanonicalRepository repository = new(
            repositoryId,
            projectId,
            githubRepository,
            new RepositoryLocator("github", githubOwner, githubRepository),
            now,
            now);
        ProjectSnapshot snapshot = new(project, [repository]);

        await _projectCatalog.SaveAsync(snapshot, cancellationToken);

        return new OwnerProjectBootstrapResult(
            project.Id.ToString(),
            project.Name,
            project.Aliases,
            repository.Id.ToString(),
            repository.Locator.FullName);
    }
}

public sealed class LorenOwnerGitHubWriteService
{
    private readonly IProjectCatalog _projectCatalog;
    private readonly IActionApprovalStore _approvalStore;
    private readonly ICreateBranchProposalStore _proposalStore;
    private readonly IActionGateway _actionGateway;
    private readonly InMemoryAuditSink _audit;

    public LorenOwnerGitHubWriteService(
        IProjectCatalog projectCatalog,
        IActionApprovalStore approvalStore,
        IActionGateway actionGateway,
        InMemoryAuditSink audit,
        ICreateBranchProposalStore proposalStore)
    {
        _projectCatalog = projectCatalog ?? throw new ArgumentNullException(nameof(projectCatalog));
        _approvalStore = approvalStore ?? throw new ArgumentNullException(nameof(approvalStore));
        _proposalStore = proposalStore ?? throw new ArgumentNullException(nameof(proposalStore));
        _actionGateway = actionGateway ?? throw new ArgumentNullException(nameof(actionGateway));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<OwnerActionProposalDecisionResult> ApproveProposalAndCreateBranchAsync(
        string proposalId, string ownerPrincipalReference, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(proposalId, out Guid parsed) || parsed == Guid.Empty) return new(proposalId, "unknown", false, "Proposal not found.");
        if (string.IsNullOrWhiteSpace(ownerPrincipalReference)) return new(proposalId, "owner_mismatch", false, "Owner identity does not match this proposal.");
        CreateBranchProposalId id = new(parsed);
        CreateBranchProposal? proposal = await _proposalStore.GetAsync(id, cancellationToken);
        if (proposal is null) return new(proposalId, "unknown", false, "Proposal not found.");
        if (!string.Equals(proposal.OwnerPrincipalReference, ownerPrincipalReference, StringComparison.Ordinal)) return new(proposalId, "owner_mismatch", false, "Owner identity does not match this proposal.");
        DateTimeOffset decided = DateTimeOffset.UtcNow;
        ProjectSnapshot? snapshot = await _projectCatalog.GetAsync(proposal.ProjectId, cancellationToken);
        CanonicalRepository? repository = snapshot?.Repositories.SingleOrDefault(r => r.Id == proposal.RepositoryId);
        if (snapshot is null || repository is null || !string.Equals(repository.Locator.Provider, proposal.RepositoryLocator.Provider, StringComparison.Ordinal) || !string.Equals(repository.Locator.ExternalNamespace, proposal.RepositoryLocator.ExternalNamespace, StringComparison.Ordinal) || !string.Equals(repository.Locator.ExternalName, proposal.RepositoryLocator.ExternalName, StringComparison.Ordinal))
            return new(proposalId, "mismatch", false, "Canonical target changed; approval was not created.");
        Dictionary<string, string> target = new(StringComparer.Ordinal) { [GitHubCreateBranchActionExecutor.BranchTargetKey] = proposal.Branch, [GitHubCreateBranchActionExecutor.SourceShaTargetKey] = proposal.SourceSha };
        ActionRequest request = new(GitHubActions.CreateBranch.Name, target);
        ActionAuthorizationContext authorization = new(proposal.ProjectId, proposal.RepositoryId, repository.Locator, ownerPrincipalReference, target);
        if (!string.Equals(ActionIntentFingerprint.Compute(GitHubActions.CreateBranch, request, authorization), proposal.IntentFingerprint, StringComparison.Ordinal)) return new(proposalId, "mismatch", false, "Proposal target no longer matches canonical state.");
        ApprovalId approvalId = ApprovalId.New();
        ActionApproval approval = new(approvalId, ownerPrincipalReference, GitHubActions.CreateBranch.Name, proposal.ProjectId, proposal.RepositoryId, proposal.IntentFingerprint, decided, decided.AddMinutes(5));
        CreateBranchProposalDecisionResult decision = await _proposalStore.ApproveAsync(new(id, ownerPrincipalReference, decided), approval, cancellationToken);
        if (decision.Status != CreateBranchProposalDecisionStatus.Approved) return new(proposalId, decision.Status.ToString().ToLowerInvariant(), false, decision.Reason);
        RunId runId = RunId.New(); ActionId actionId = ActionId.New();
        ActionResult execution = await _actionGateway.ExecuteAsync(new ActionExecutionRequest(runId, actionId, request, authorization, approvalId), cancellationToken);
        IReadOnlyList<LorenAuditEntry> audit = _audit.Snapshot().Where(a => a.RunId == runId).Select(ToAuditEntry).ToArray();
        string message = execution.Success ? $"Đã tạo và kiểm tra branch {proposal.Branch} trên {repository.Locator.FullName} từ {proposal.SourceRef} ({proposal.SourceSha})." : "Không thể hoàn tất thay đổi GitHub; quyết định đã được ghi nhận và Loren sẽ không tự động thử lại.";
        return new(proposalId, "approved", execution.Success, message, execution, audit);
    }

    public async Task<OwnerActionProposalDecisionResult> CancelProposalAsync(string proposalId, string ownerPrincipalReference, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(proposalId, out Guid parsed) || parsed == Guid.Empty) return new(proposalId, "unknown", false, "Proposal not found.");
        CreateBranchProposalDecisionResult result = await _proposalStore.CancelAsync(new(new CreateBranchProposalId(parsed), ownerPrincipalReference, DateTimeOffset.UtcNow), cancellationToken);
        return new(proposalId, result.Status.ToString().ToLowerInvariant(), result.Status == CreateBranchProposalDecisionStatus.Cancelled, result.Status == CreateBranchProposalDecisionStatus.Cancelled ? "Đã huỷ đề xuất; không có thay đổi GitHub nào được thực hiện." : result.Reason);
    }

    private static LorenAuditEntry ToAuditEntry(AuditEvent e) => new(e.ActionId.ToString(), e.Kind.ToString(), e.ActionName, e.Outcome, e.Detail);

}

public sealed record OwnerProjectBootstrapResult(
    string ProjectId,
    string ProjectName,
    IReadOnlyList<string> Aliases,
    string RepositoryId,
    string RepositoryFullName);

public sealed record OwnerProjectBootstrapRequest(
    string ProjectName,
    string ProjectAlias,
    string GitHubOwner,
    string GitHubRepository);
