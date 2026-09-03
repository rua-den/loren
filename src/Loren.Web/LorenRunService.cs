using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Infrastructure.Audit;
using Loren.Runtime;
using Loren.Tools.GitHub;

namespace Loren.Web;

public sealed class LorenRunService
{
    private readonly AgentLoop _agentLoop;
    private readonly InMemoryAuditSink _audit;

    public LorenRunService(
        AgentLoop agentLoop,
        InMemoryAuditSink audit)
    {
        _agentLoop = agentLoop ?? throw new ArgumentNullException(nameof(agentLoop));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<LorenRunResult> RunAsync(
        string message,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        AgentRunResult result = await _agentLoop.RunAsync(
            BrainContext.FromUser(message),
            [GitHubActions.ReadRepository],
            cancellationToken);

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
            auditEntries);
    }

    private static LorenAuditEntry ToAuditEntry(AuditEvent auditEvent) => new(
        auditEvent.ActionId.ToString(),
        auditEvent.Kind.ToString(),
        auditEvent.ActionName,
        auditEvent.Outcome,
        auditEvent.Detail);
}

public sealed record LorenRunRequest(string Message);

public sealed record LorenRunResult(
    string FinalOutput,
    string RunId,
    int Turns,
    int ActionCount,
    IReadOnlyList<LorenAuditEntry> Audit);

public sealed record LorenAuditEntry(
    string ActionId,
    string Kind,
    string ActionName,
    string Outcome,
    string? Detail);
