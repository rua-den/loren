using System.Text.Json;
using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Core.Credentials;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Runtime;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CredentialRedactionBoundaryTests
{
    private static readonly CredentialPurpose Purpose = new("github.write");
    private static readonly CredentialReference Reference = new("github.write.local-v0.1");

    [Fact]
    public async Task SecretCannotEscapeIntoAuditOrBrainObservation()
    {
        const string secret = "slice2-boundary-secret";
        const string actionName = "test.credential_write";
        CredentialLease lease = new(Purpose, Reference, secret);
        ResolvedCredentialResolver resolver = new(lease);
        LeakyCredentialExecutor executor = new(resolver, actionName);
        InMemoryAuditSink audit = new();
        ActionDefinition definition = new(
            actionName,
            "Credential redaction plumbing test.",
            ActionAccessClass.ExternalWrite);
        ActionGateway gateway = new(
            [definition],
            [executor],
            new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: false)),
            audit,
            new AlwaysConsumeApprovalStore());
        ActionRequest request = new(
            actionName,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["safe_argument"] = "safe-value",
            });
        ActionAuthorizationContext authorization = new(
            ProjectId.New(),
            RepositoryId.New(),
            new RepositoryLocator("github", "owner", "repository"),
            "owner");
        ActionExecutionRequest execution = new(
            RunId.New(),
            ActionId.New(),
            request,
            authorization,
            ApprovalId.New());

        ActionResult result = await gateway.ExecuteAsync(
            execution,
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.DoesNotContain(secret, result.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.All(result.Data, pair =>
        {
            Assert.DoesNotContain(secret, pair.Key, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, pair.Value, StringComparison.Ordinal);
        });

        IReadOnlyList<AuditEvent> events = audit.Snapshot();
        Assert.Equal(4, events.Count);
        Assert.All(events, auditEvent =>
        {
            Assert.DoesNotContain(secret, auditEvent.ActionName, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, auditEvent.Outcome, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, auditEvent.Detail ?? string.Empty, StringComparison.Ordinal);
        });

        BrainActionObservation observation = new(request, result);
        string serializedObservation = JsonSerializer.Serialize(observation);
        Assert.DoesNotContain(secret, serializedObservation, StringComparison.Ordinal);
        Assert.Contains(CredentialLease.RedactedValue, serializedObservation, StringComparison.Ordinal);
    }

    private sealed class ResolvedCredentialResolver(CredentialLease lease)
        : IActionCredentialResolver
    {
        public Task<CredentialResolution> ResolveAsync(
            CredentialResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(Purpose, request.Purpose);
            Assert.Equal(Reference, request.Reference);
            return Task.FromResult(CredentialResolution.Resolved(lease));
        }
    }

    private sealed class LeakyCredentialExecutor(
        IActionCredentialResolver resolver,
        string actionName)
        : CredentialBoundActionExecutor(resolver, Purpose, Reference)
    {
        public override string ActionName { get; } = actionName;

        protected override Task<ActionResult> ExecuteWithCredentialAsync(
            ActionExecutionRequest execution,
            string credentialSecret,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                new ActionResult(
                    execution.Request.Name,
                    true,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["safe"] = "ok",
                        ["leaky_value"] = credentialSecret,
                    },
                    $"diagnostic contained {credentialSecret}"));
        }
    }

    private sealed class AlwaysConsumeApprovalStore : IActionApprovalStore
    {
        public Task AddAsync(
            ActionApproval approval,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ActionApproval?> GetAsync(
            ApprovalId approvalId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ApprovalConsumptionResult> ConsumeAsync(
            ApprovalConsumptionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                new ApprovalConsumptionResult(
                    ApprovalConsumptionStatus.Consumed,
                    "consumed for redaction test"));
        }

        public Task RevokeAsync(
            ApprovalId approvalId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
