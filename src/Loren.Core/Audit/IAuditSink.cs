namespace Loren.Core.Audit;

public interface IAuditSink
{
    Task AppendAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken);
}
