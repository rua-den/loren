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
    private readonly LorenProjectContextBuilder? _projectContextBuilder;

    public LorenRunService(
        AgentLoop agentLoop,
        InMemoryAuditSink audit)
        : this(agentLoop, audit, null)
    {
    }

    public LorenRunService(
        AgentLoop agentLoop,
        InMemoryAuditSink audit,
        LorenProjectContextBuilder? projectContextBuilder)
    {
        _agentLoop = agentLoop ?? throw new ArgumentNullException(nameof(agentLoop));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _projectContextBuilder = projectContextBuilder;
    }

    public Task<LorenRunResult> RunAsync(
        string message,
        CancellationToken cancellationToken) =>
        RunAsync(message, null, cancellationToken);

    public async Task<LorenRunResult> RunAsync(
        string message,
        string? projectAlias,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        PreparedLorenContext preparedContext = _projectContextBuilder is null
            ? new PreparedLorenContext(BrainContext.FromUser(message), null)
            : await _projectContextBuilder.BuildAsync(
                message,
                projectAlias,
                cancellationToken);

        AgentRunResult result = await _agentLoop.RunAsync(
            preparedContext.BrainContext,
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
            auditEntries,
            preparedContext.Project);
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
    string? ProjectAlias = null);

public sealed record LorenRunResult(
    string FinalOutput,
    string RunId,
    int Turns,
    int ActionCount,
    IReadOnlyList<LorenAuditEntry> Audit,
    LorenProjectContext? Project = null);

public sealed record LorenAuditEntry(
    string ActionId,
    string Kind,
    string ActionName,
    string Outcome,
    string? Detail);
