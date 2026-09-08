using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Loren.Tools.Web;

namespace Loren.Web;

public sealed class LorenRunService
{
    private readonly AgentLoop _agentLoop;
    private readonly InMemoryAuditSink _audit;
    private readonly LorenProjectContextBuilder? _projectContextBuilder;
    private readonly ICurrentRunProposalCollector _proposalCollector;
    private readonly ICreateBranchProposalStore? _proposalStore;

    public LorenRunService(
        AgentLoop agentLoop,
        InMemoryAuditSink audit)
        : this(agentLoop, audit, null)
    {
    }

    public LorenRunService(
        AgentLoop agentLoop,
        InMemoryAuditSink audit,
        LorenProjectContextBuilder? projectContextBuilder,
        ICurrentRunProposalCollector? proposalCollector = null,
        ICreateBranchProposalStore? proposalStore = null)
    {
        _agentLoop = agentLoop ?? throw new ArgumentNullException(nameof(agentLoop));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _projectContextBuilder = projectContextBuilder;
        _proposalCollector = proposalCollector ?? new CurrentRunProposalCollector();
        _proposalStore = proposalStore;
    }

    public Task<LorenRunResult> RunAsync(
        string message,
        CancellationToken cancellationToken) =>
        RunAsync(message, null, null, null, cancellationToken);

    public Task<LorenRunResult> RunAsync(
        string message,
        string? projectAlias,
        CancellationToken cancellationToken) =>
        RunAsync(message, projectAlias, null, null, cancellationToken);

    public Task<LorenRunResult> RunAsync(
        string message,
        string? projectAlias,
        IReadOnlyList<LorenConversationMessage>? history,
        CancellationToken cancellationToken) =>
        RunAsync(message, projectAlias, history, null, cancellationToken);

    public async Task<LorenRunResult> RunAsync(
        string message,
        string? projectAlias,
        IReadOnlyList<LorenConversationMessage>? history,
        string? ownerPrincipalReference,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        PreparedLorenContext preparedContext = _projectContextBuilder is null
            ? new PreparedLorenContext(BrainContext.FromUser(message), null)
            : await _projectContextBuilder.BuildAsync(
                message,
                projectAlias,
                history,
                cancellationToken);

        AuthenticatedOwnerContext? ownerContext = string.IsNullOrWhiteSpace(ownerPrincipalReference)
            ? null
            : new AuthenticatedOwnerContext(
                ownerPrincipalReference,
                preparedContext.Project is null
                    ? null
                    : ProjectId.Parse(preparedContext.Project.ProjectId));

        _proposalCollector.Start();
        AgentRunResult result = await _agentLoop.RunAsync(
            preparedContext.BrainContext,
            [
                GitHubActions.ReadRepository,
                GitHubActions.ProposeCreateBranch,
                WebActions.Search,
                WebActions.Fetch,
                OrganizationActions.CreateNote,
                OrganizationActions.RecordDecision,
                OrganizationActions.CreateTask,
                OrganizationActions.List,
                OrganizationActions.CompleteTask,
                OrganizationActions.ReopenTask,
            ],
            ownerContext,
            cancellationToken);

        List<PendingCreateBranchProposal> proposals = [];
        if (_proposalStore is not null)
        {
            foreach (CreateBranchProposalId proposalId in _proposalCollector.Drain())
            {
                CreateBranchProposal? proposal = await _proposalStore.GetAsync(proposalId, cancellationToken);
                if (proposal is not null)
                {
                    proposals.Add(new PendingCreateBranchProposal(proposal.Id.ToString(), proposal.ActionName, proposal.RepositoryLocator.FullName, proposal.Branch, proposal.SourceRef, proposal.SourceSha, proposal.AccessClass.ToString(), proposal.CreatedAt, proposal.ExpiresAt));
                }
            }
        }

        LorenAuditEntry[] auditEntries = _audit
            .Snapshot()
            .Where(auditEvent => auditEvent.RunId == result.RunId)
            .Select(ToAuditEntry)
            .ToArray();

        return new LorenRunResult(
            result.FinalOutput,
            result.RunId.ToString(),
            result.Turns,
            result.ActionCount,
            auditEntries,
            preparedContext.Project,
            proposals);
    }

    private static LorenAuditEntry ToAuditEntry(AuditEvent auditEvent) => new(
        auditEvent.ActionId.ToString(),
        auditEvent.Kind.ToString(),
        auditEvent.ActionName,
        auditEvent.Outcome,
        auditEvent.Detail);
}

public sealed record LorenRunRequest(
    string Message,
    string? ProjectAlias = null,
    IReadOnlyList<LorenConversationMessage>? History = null);

public sealed record LorenRunResult(
    string FinalOutput,
    string RunId,
    int Turns,
    int ActionCount,
    IReadOnlyList<LorenAuditEntry> Audit,
    LorenProjectContext? Project = null,
    IReadOnlyList<PendingCreateBranchProposal>? Proposals = null);

public sealed record LorenAuditEntry(
    string ActionId,
    string Kind,
    string ActionName,
    string Outcome,
    string? Detail);
