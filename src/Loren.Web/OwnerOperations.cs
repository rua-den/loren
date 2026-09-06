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
    private static readonly TimeSpan ApprovalLifetime = TimeSpan.FromMinutes(5);

    private readonly IProjectCatalog _projectCatalog;
    private readonly IActionApprovalStore _approvalStore;
    private readonly IActionGateway _actionGateway;
    private readonly InMemoryAuditSink _audit;

    public LorenOwnerGitHubWriteService(
        IProjectCatalog projectCatalog,
        IActionApprovalStore approvalStore,
        IActionGateway actionGateway,
        InMemoryAuditSink audit)
    {
        _projectCatalog = projectCatalog ?? throw new ArgumentNullException(nameof(projectCatalog));
        _approvalStore = approvalStore ?? throw new ArgumentNullException(nameof(approvalStore));
        _actionGateway = actionGateway ?? throw new ArgumentNullException(nameof(actionGateway));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<OwnerCreateBranchResult> ApproveAndCreateBranchAsync(
        string projectAlias,
        string? repositoryId,
        string branch,
        string sourceSha,
        string ownerPrincipalReference,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectAlias);
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPrincipalReference);

        ProjectSnapshot snapshot = await _projectCatalog.FindByAliasAsync(
                projectAlias,
                cancellationToken)
            ?? throw new UnknownProjectAliasException(projectAlias);
        CanonicalRepository repository = ResolveRepository(snapshot, repositoryId);

        string normalizedBranch = branch.Trim();
        string normalizedSourceSha = sourceSha.Trim().ToLowerInvariant();
        Dictionary<string, string> exactTarget = new(StringComparer.Ordinal)
        {
            [GitHubCreateBranchActionExecutor.BranchTargetKey] = normalizedBranch,
            [GitHubCreateBranchActionExecutor.SourceShaTargetKey] = normalizedSourceSha,
        };
        ActionRequest actionRequest = new(
            GitHubActions.CreateBranch.Name,
            exactTarget);
        ActionAuthorizationContext authorizationContext = new(
            snapshot.Project.Id,
            repository.Id,
            repository.Locator,
            ownerPrincipalReference,
            exactTarget);
        string fingerprint = ActionIntentFingerprint.Compute(
            GitHubActions.CreateBranch,
            actionRequest,
            authorizationContext);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ApprovalId approvalId = ApprovalId.New();
        ActionApproval approval = new(
            approvalId,
            ownerPrincipalReference,
            GitHubActions.CreateBranch.Name,
            snapshot.Project.Id,
            repository.Id,
            fingerprint,
            now,
            now.Add(ApprovalLifetime));
        await _approvalStore.AddAsync(approval, cancellationToken);

        RunId runId = RunId.New();
        ActionId actionId = ActionId.New();
        ActionExecutionRequest execution = new(
            runId,
            actionId,
            actionRequest,
            authorizationContext,
            approvalId);
        ActionResult result = await _actionGateway.ExecuteAsync(
            execution,
            cancellationToken);
        AuditEvent[] audit = _audit.Snapshot()
            .Where(auditEvent => auditEvent.RunId == runId)
            .ToArray();

        return new OwnerCreateBranchResult(
            runId.ToString(),
            actionId.ToString(),
            approvalId.ToString(),
            snapshot.Project.Id.ToString(),
            repository.Id.ToString(),
            repository.Locator.FullName,
            normalizedBranch,
            normalizedSourceSha,
            result,
            audit);
    }

    private static CanonicalRepository ResolveRepository(
        ProjectSnapshot snapshot,
        string? repositoryId)
    {
        CanonicalRepository[] githubRepositories = snapshot.Repositories
            .Where(repository => string.Equals(
                repository.Locator.Provider,
                "github",
                StringComparison.Ordinal))
            .ToArray();

        if (!string.IsNullOrWhiteSpace(repositoryId))
        {
            RepositoryId parsed;
            try
            {
                parsed = RepositoryId.Parse(repositoryId.Trim());
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException(
                    "repositoryId must be a Loren canonical repository GUID.",
                    exception);
            }

            return githubRepositories.SingleOrDefault(repository => repository.Id == parsed)
                ?? throw new InvalidOperationException(
                    "The requested canonical GitHub repository is not part of the project.");
        }

        return githubRepositories.Length switch
        {
            1 => githubRepositories[0],
            0 => throw new InvalidOperationException(
                "The canonical project has no GitHub repository."),
            _ => throw new InvalidOperationException(
                "The canonical project has multiple GitHub repositories; repositoryId is required."),
        };
    }
}

public sealed record OwnerProjectBootstrapResult(
    string ProjectId,
    string ProjectName,
    IReadOnlyList<string> Aliases,
    string RepositoryId,
    string RepositoryFullName);

public sealed record OwnerCreateBranchResult(
    string RunId,
    string ActionId,
    string ApprovalId,
    string ProjectId,
    string RepositoryId,
    string RepositoryFullName,
    string Branch,
    string SourceSha,
    ActionResult Result,
    IReadOnlyList<AuditEvent> Audit);

public sealed record OwnerProjectBootstrapRequest(
    string ProjectName,
    string ProjectAlias,
    string GitHubOwner,
    string GitHubRepository);

public sealed record OwnerCreateBranchRequest(
    string ProjectAlias,
    string? RepositoryId,
    string Branch,
    string SourceSha);
