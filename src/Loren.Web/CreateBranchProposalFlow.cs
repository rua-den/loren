using Loren.Core.Actions;
using Loren.Core.Projects;
using Loren.Tools.GitHub;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.Web;

public interface ICurrentRunProposalCollector
{
    void Start();
    void Record(CreateBranchProposalId proposalId);
    IReadOnlyList<CreateBranchProposalId> Drain();
}

public sealed class CurrentRunProposalCollector : ICurrentRunProposalCollector
{
    private readonly List<CreateBranchProposalId> _ids = [];
    public void Start() => _ids.Clear();
    public void Record(CreateBranchProposalId proposalId) => _ids.Add(proposalId);
    public IReadOnlyList<CreateBranchProposalId> Drain() { CreateBranchProposalId[] result = [.. _ids]; _ids.Clear(); return result; }
}

public sealed class GitHubCreateBranchProposalExecutor : ITrustedActionExecutor
{
    private readonly IProjectCatalog _catalog;
    private readonly GitHubRepositoryReadClient _readClient;
    private readonly ICreateBranchProposalStore _store;
    private readonly ICurrentRunProposalCollector _collector;
    private readonly TimeProvider _clock;

    public GitHubCreateBranchProposalExecutor(IProjectCatalog catalog, GitHubRepositoryReadClient readClient, ICreateBranchProposalStore store, ICurrentRunProposalCollector collector, TimeProvider? clock = null)
    { _catalog = catalog; _readClient = readClient; _store = store; _collector = collector; _clock = clock ?? TimeProvider.System; }
    public string ActionName => GitHubActions.ProposeCreateBranch.Name;
    public Task<ActionResult> ExecuteAsync(ActionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new ActionResult(request.Name, false, new Dictionary<string, string>(), "Trusted owner context is required."));

    public async Task<ActionResult> ExecuteTrustedAsync(ActionExecutionRequest execution, CancellationToken cancellationToken)
    {
        AuthenticatedOwnerContext owner = execution.OwnerContext ?? throw new InvalidOperationException("Authenticated owner context is required.");
        if (owner.ProjectId is null) return Fail(execution.Request, "A canonical project is required for branch proposals.");
        if (!TryGet(execution.Request.Arguments, "branch", out string branch)) return Fail(execution.Request, "Branch is required.");
        string? requestedRepository = Get(execution.Request.Arguments, "repository_id");
        string? sourceRef = Get(execution.Request.Arguments, "source_ref");
        ProjectSnapshot? snapshot = await _catalog.GetAsync(owner.ProjectId.Value, cancellationToken);
        if (snapshot is null) return Fail(execution.Request, "Canonical project could not be found.");
        CanonicalRepository repository = ResolveRepository(snapshot, requestedRepository);
        GitHubRepositoryResolutionResult source = await _readClient.ResolveSourceAsync(repository.Locator, sourceRef, cancellationToken);
        if (!source.Success || source.SourceRef is null || source.SourceSha is null) return Fail(execution.Request, source.Error ?? "Source branch could not be resolved.");
        if (string.Equals(source.DefaultBranch, branch, StringComparison.Ordinal)) return Fail(execution.Request, "The proposed branch must differ from the live default branch.");
        Dictionary<string, string> target = new(StringComparer.Ordinal) { [GitHubCreateBranchActionExecutor.BranchTargetKey] = branch, [GitHubCreateBranchActionExecutor.SourceShaTargetKey] = source.SourceSha };
        ActionRequest createRequest = new(GitHubActions.CreateBranch.Name, target);
        ActionAuthorizationContext authorization = new(snapshot.Project.Id, repository.Id, repository.Locator, owner.OwnerPrincipalReference, target);
        string fingerprint = ActionIntentFingerprint.Compute(GitHubActions.CreateBranch, createRequest, authorization);
        DateTimeOffset created = _clock.GetUtcNow();
        CreateBranchProposal proposal = new(CreateBranchProposalId.New(), owner.OwnerPrincipalReference, snapshot.Project.Id, repository.Id, repository.Locator, branch, source.SourceRef, source.SourceSha, fingerprint, created, created.AddMinutes(5));
        await _store.AddAsync(proposal, cancellationToken);
        _collector.Record(proposal.Id);
        return new ActionResult(execution.Request.Name, true, new Dictionary<string, string> { ["proposal_id"] = proposal.Id.ToString(), ["repository"] = repository.Locator.FullName, ["branch"] = proposal.Branch, ["source_ref"] = proposal.SourceRef, ["source_sha"] = proposal.SourceSha, ["expires_at"] = proposal.ExpiresAt.ToString("O") }, null);
    }

    private static CanonicalRepository ResolveRepository(ProjectSnapshot snapshot, string? id)
    {
        CanonicalRepository[] repos = snapshot.Repositories.Where(r => string.Equals(r.Locator.Provider, "github", StringComparison.Ordinal)).ToArray();
        if (!string.IsNullOrWhiteSpace(id)) { if (!Guid.TryParse(id, out Guid value)) throw new ArgumentException("repository_id must be a canonical repository ID."); return repos.SingleOrDefault(r => r.Id.Value == value) ?? throw new InvalidOperationException("The requested canonical GitHub repository is not part of the project."); }
        return repos.Length switch { 1 => repos[0], 0 => throw new InvalidOperationException("The canonical project has no GitHub repository."), _ => throw new InvalidOperationException("repository_id is required when the project has multiple GitHub repositories.") };
    }
    private static string? Get(IReadOnlyDictionary<string, string> args, string key) => args.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    private static bool TryGet(IReadOnlyDictionary<string, string> args, string key, out string value) { value = Get(args, key) ?? string.Empty; return value.Length > 0; }
    private static ActionResult Fail(ActionRequest request, string error) => new(request.Name, false, new Dictionary<string, string>(), error);
}

public sealed record PendingCreateBranchProposal(
    string ProposalId, string ActionName, string Repository, string Branch, string SourceRef, string SourceSha, string AccessClass, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

public sealed record OwnerActionProposalDecisionResult(
    string ProposalId, string Status, bool Success, string Message, ActionResult? Execution = null, IReadOnlyList<LorenAuditEntry>? Audit = null);
