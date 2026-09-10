namespace Loren.Infrastructure.CanonicalState;

internal sealed class AuditEventRow
{
    public long Id { get; set; }
    public string RunId { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public long OccurredAtUnixMs { get; set; }
}
