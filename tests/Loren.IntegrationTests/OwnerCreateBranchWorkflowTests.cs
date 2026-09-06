using System.Net;
using Loren.Core.Actions;
using Loren.Core.Credentials;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Loren.Web;
using Xunit;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.IntegrationTests;

public sealed class OwnerCreateBranchWorkflowTests
{
    private const string Secret = "owner-workflow-secret-that-must-not-leak";
    private const string SourceSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task AuthenticatedOwnerWorkflowCreatesConsumesAndVerifiesExactBranchIntent()
    {
        ProjectSnapshot snapshot = Snapshot();
        StubProjectCatalog catalog = new(snapshot);
        RecordingApprovalStore approvalStore = new();
        InMemoryAuditSink audit = new();
        SequenceHandler handler = new(
            Json(HttpStatusCode.OK, "{\"default_branch\":\"main\"}"),
            Json(HttpStatusCode.Created, "{}"),
            Json(HttpStatusCode.OK, $"{{\"object\":{{\"sha\":\"{SourceSha}\"}}}}"));
        GitHubCreateBranchActionExecutor executor = new(
            new FixedCredentialResolver(revoked: false),
            new GitHubCreateBranchClient(new HttpClient(handler)));
        ActionGateway gateway = new(
            [GitHubActions.CreateBranch],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: false)),
            audit,
            approvalStore);
        LorenOwnerGitHubWriteService service = new(
            catalog,
            approvalStore,
            gateway,
            audit);

        OwnerCreateBranchResult result = await service.ApproveAndCreateBranchAsync(
            "loren",
            null,
            "feat/owner-workflow",
            SourceSha,
            "owner",
            CancellationToken.None);

        Assert.True(result.Result.Success);
        Assert.Equal("verified", result.Result.Data["verification"]);
        Assert.Equal(SourceSha, result.Result.Data["verified_sha"]);
        Assert.Equal("rua-den/loren", result.RepositoryFullName);
        Assert.Equal(3, handler.RequestCount);
        Assert.Single(approvalStore.Approvals);
        ActionApproval approval = approvalStore.Approvals[0];
        Assert.Equal("owner", approval.OwnerPrincipalReference);
        Assert.NotNull(approval.ConsumedAt);
        Assert.Contains(
            result.Audit,
            auditEvent => auditEvent.Kind is Loren.Core.Audit.AuditEventKind.ApprovalEvaluated
                && auditEvent.Outcome == "consumed");
        Assert.DoesNotContain(
            Secret,
            string.Join('|', result.Result.Data.Select(pair => $"{pair.Key}={pair.Value}")),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RevokedCredentialOverridesFreshOwnerApprovalBeforeAnyGitHubCall()
    {
        ProjectSnapshot snapshot = Snapshot();
        StubProjectCatalog catalog = new(snapshot);
        RecordingApprovalStore approvalStore = new();
        InMemoryAuditSink audit = new();
        SequenceHandler handler = new();
        GitHubCreateBranchActionExecutor executor = new(
            new FixedCredentialResolver(revoked: true),
            new GitHubCreateBranchClient(new HttpClient(handler)));
        ActionGateway gateway = new(
            [GitHubActions.CreateBranch],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: false)),
            audit,
            approvalStore);
        LorenOwnerGitHubWriteService service = new(
            catalog,
            approvalStore,
            gateway,
            audit);

        OwnerCreateBranchResult result = await service.ApproveAndCreateBranchAsync(
            "loren",
            null,
            "feat/revoked",
            SourceSha,
            "owner",
            CancellationToken.None);

        Assert.False(result.Result.Success);
        Assert.Equal("revoked", result.Result.Data["credential_status"]);
        Assert.Equal(0, handler.RequestCount);
        Assert.Single(approvalStore.Approvals);
        Assert.NotNull(approvalStore.Approvals[0].ConsumedAt);
    }

    private static ProjectSnapshot Snapshot()
    {
        DateTimeOffset now = new(2026, 9, 6, 0, 0, 0, TimeSpan.Zero);
        ProjectId projectId = ProjectId.New();
        Project project = new(projectId, "Loren", ["loren"], now, now);
        CanonicalRepository repository = new(
            RepositoryId.New(),
            projectId,
            "loren",
            new RepositoryLocator("github", "rua-den", "loren"),
            now,
            now);
        return new ProjectSnapshot(project, [repository]);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };

    private sealed class SequenceHandler(params HttpResponseMessage[] responses)
        : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("Unexpected external GitHub request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class FixedCredentialResolver(bool revoked) : IActionCredentialResolver
    {
        public Task<CredentialResolution> ResolveAsync(
            CredentialResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                revoked
                    ? CredentialResolution.Revoked(request)
                    : CredentialResolution.Resolved(
                        new CredentialLease(request.Purpose, request.Reference, Secret)));
        }
    }

    private sealed class StubProjectCatalog(ProjectSnapshot snapshot) : IProjectCatalog
    {
        public Task SaveAsync(
            ProjectSnapshot newSnapshot,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProjectSnapshot?> GetAsync(
            ProjectId projectId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ProjectSnapshot?>(
                snapshot.Project.Id == projectId ? snapshot : null);
        }

        public Task<ProjectSnapshot?> FindByAliasAsync(
            string projectAlias,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool matches = snapshot.Project.Aliases.Contains(
                ProjectAlias.Normalize(projectAlias),
                StringComparer.Ordinal);
            return Task.FromResult<ProjectSnapshot?>(matches ? snapshot : null);
        }
    }

    private sealed class RecordingApprovalStore : IActionApprovalStore
    {
        private readonly Dictionary<ApprovalId, ActionApproval> _approvals = [];

        public ActionApproval[] Approvals => _approvals.Values.ToArray();

        public Task AddAsync(
            ActionApproval approval,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _approvals.Add(approval.Id, approval);
            return Task.CompletedTask;
        }

        public Task<ActionApproval?> GetAsync(
            ApprovalId approvalId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _approvals.TryGetValue(approvalId, out ActionApproval? approval);
            return Task.FromResult(approval);
        }

        public Task<ApprovalConsumptionResult> ConsumeAsync(
            ApprovalConsumptionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_approvals.TryGetValue(request.ApprovalId, out ActionApproval? approval))
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.Unknown,
                    "Approval does not exist."));
            }

            if (approval.ConsumedAt is not null)
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.AlreadyConsumed,
                    "Approval was already consumed."));
            }

            if (approval.RevokedAt is not null)
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.Revoked,
                    "Approval was revoked."));
            }

            if (request.ConsumedAt >= approval.ExpiresAt)
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.Expired,
                    "Approval expired."));
            }

            bool matches = approval.OwnerPrincipalReference == request.OwnerPrincipalReference
                && approval.ActionName == request.ActionName
                && approval.ProjectId == request.ProjectId
                && approval.RepositoryId == request.RepositoryId
                && approval.IntentFingerprint == request.IntentFingerprint;
            if (!matches)
            {
                return Task.FromResult(new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.Mismatch,
                    "Approval intent mismatch."));
            }

            _approvals[approval.Id] = new ActionApproval(
                approval.Id,
                approval.OwnerPrincipalReference,
                approval.ActionName,
                approval.ProjectId,
                approval.RepositoryId,
                approval.IntentFingerprint,
                approval.ApprovedAt,
                approval.ExpiresAt,
                request.ConsumedAt,
                approval.RevokedAt);
            return Task.FromResult(new ApprovalConsumptionResult(
                ApprovalConsumptionStatus.Consumed,
                "Approval consumed."));
        }

        public Task RevokeAsync(
            ApprovalId approvalId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
