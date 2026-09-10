using Loren.Core.Audit;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.Audit;

public sealed class DurableAuditSink(CanonicalStateDbContext dbContext, InMemoryAuditSink memory) : IAuditSink
{
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        dbContext.AuditEvents.Add(new AuditEventRow
        {
            RunId = auditEvent.RunId.ToString(),
            ActionId = auditEvent.ActionId.ToString(),
            Kind = auditEvent.Kind.ToString(),
            ActionName = auditEvent.ActionName,
            Outcome = auditEvent.Outcome,
            Detail = auditEvent.Detail,
            OccurredAtUnixMs = auditEvent.OccurredAt.ToUnixTimeMilliseconds(),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await memory.AppendAsync(auditEvent, cancellationToken);
    }
}
