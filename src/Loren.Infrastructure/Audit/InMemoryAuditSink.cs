using Loren.Core.Audit;

namespace Loren.Infrastructure.Audit;

public sealed class InMemoryAuditSink : IAuditSink
{
    private readonly object _gate = new();
    private readonly List<AuditEvent> _events = [];

    public Task AppendAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _events.Add(auditEvent);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<AuditEvent> Snapshot()
    {
        lock (_gate)
        {
            return _events.ToArray();
        }
    }
}
