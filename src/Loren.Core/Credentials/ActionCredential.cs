namespace Loren.Core.Credentials;

public readonly record struct CredentialPurpose
{
    public CredentialPurpose(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToLowerInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct CredentialReference
{
    public CredentialReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record CredentialResolutionRequest(
    CredentialPurpose Purpose,
    CredentialReference Reference);

public enum CredentialResolutionStatus
{
    Resolved,
    Missing,
    Revoked,
    NotConfigured,
}

public sealed class CredentialLease
{
    public const string RedactedValue = "[REDACTED]";

    private readonly string _secret;

    public CredentialLease(
        CredentialPurpose purpose,
        CredentialReference reference,
        string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        Purpose = purpose;
        Reference = reference;
        _secret = secret;
    }

    public CredentialPurpose Purpose { get; }

    public CredentialReference Reference { get; }

    public TResult Use<TResult>(Func<string, TResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return operation(_secret);
    }

    public Task<TResult> UseAsync<TResult>(
        Func<string, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        return operation(_secret, cancellationToken);
    }

    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        return value.Replace(
            _secret,
            RedactedValue,
            StringComparison.Ordinal);
    }

    public override string ToString() =>
        $"{Purpose}/{Reference}:{RedactedValue}";
}

public sealed class CredentialResolution
{
    private CredentialResolution(
        CredentialResolutionStatus status,
        CredentialPurpose purpose,
        CredentialReference reference,
        CredentialLease? lease,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = status;
        Purpose = purpose;
        Reference = reference;
        Lease = lease;
        Reason = reason;
    }

    public CredentialResolutionStatus Status { get; }

    public CredentialPurpose Purpose { get; }

    public CredentialReference Reference { get; }

    public CredentialLease? Lease { get; }

    public string Reason { get; }

    public bool IsResolved =>
        Status is CredentialResolutionStatus.Resolved && Lease is not null;

    public static CredentialResolution Resolved(CredentialLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        return new CredentialResolution(
            CredentialResolutionStatus.Resolved,
            lease.Purpose,
            lease.Reference,
            lease,
            "Credential resolved for the requested purpose and reference.");
    }

    public static CredentialResolution Missing(CredentialResolutionRequest request) =>
        new(
            CredentialResolutionStatus.Missing,
            request.Purpose,
            request.Reference,
            null,
            "Credential is missing for the requested purpose and reference.");

    public static CredentialResolution Revoked(CredentialResolutionRequest request) =>
        new(
            CredentialResolutionStatus.Revoked,
            request.Purpose,
            request.Reference,
            null,
            "Credential is revoked or its revocation state is invalid.");

    public static CredentialResolution NotConfigured(CredentialResolutionRequest request) =>
        new(
            CredentialResolutionStatus.NotConfigured,
            request.Purpose,
            request.Reference,
            null,
            "Credential purpose/reference is not configured.");

    public override string ToString() =>
        $"{Status}:{Purpose}/{Reference}:{Reason}";
}

public interface IActionCredentialResolver
{
    Task<CredentialResolution> ResolveAsync(
        CredentialResolutionRequest request,
        CancellationToken cancellationToken = default);
}
