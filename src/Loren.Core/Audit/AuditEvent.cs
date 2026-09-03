using Loren.Core.Actions;

namespace Loren.Core.Audit;

public enum AuditEventKind
{
    ActionRequested,
    PolicyEvaluated,
    ActionCompleted,
}

public sealed record AuditEvent(
    DateTimeOffset OccurredAt,
    RunId RunId,
    ActionId ActionId,
    AuditEventKind Kind,
    string ActionName,
    string Outcome,
    string? Detail = null);
