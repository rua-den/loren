using Loren.Core.Credentials;

namespace Loren.Infrastructure.Credentials;

public sealed record EnvironmentCredentialBinding
{
    public EnvironmentCredentialBinding(
        CredentialPurpose purpose,
        CredentialReference reference,
        string secretEnvironmentVariable,
        string revocationEnvironmentVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretEnvironmentVariable);
        ArgumentException.ThrowIfNullOrWhiteSpace(revocationEnvironmentVariable);

        Purpose = purpose;
        Reference = reference;
        SecretEnvironmentVariable = secretEnvironmentVariable.Trim();
        RevocationEnvironmentVariable = revocationEnvironmentVariable.Trim();
    }

    public CredentialPurpose Purpose { get; }

    public CredentialReference Reference { get; }

    public string SecretEnvironmentVariable { get; }

    public string RevocationEnvironmentVariable { get; }
}

public sealed class EnvironmentActionCredentialResolver : IActionCredentialResolver
{
    private readonly Dictionary<BindingKey, EnvironmentCredentialBinding> _bindings;
    private readonly Func<string, string?> _readEnvironmentVariable;

    public EnvironmentActionCredentialResolver(
        IEnumerable<EnvironmentCredentialBinding> bindings,
        Func<string, string?>? readEnvironmentVariable = null)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        _bindings = bindings.ToDictionary(
            binding => new BindingKey(binding.Purpose, binding.Reference));
        _readEnvironmentVariable = readEnvironmentVariable
            ?? Environment.GetEnvironmentVariable;
    }

    public Task<CredentialResolution> ResolveAsync(
        CredentialResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_bindings.TryGetValue(
                new BindingKey(request.Purpose, request.Reference),
                out EnvironmentCredentialBinding? binding))
        {
            return Task.FromResult(CredentialResolution.NotConfigured(request));
        }

        string? revocationValue = _readEnvironmentVariable(
            binding.RevocationEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(revocationValue))
        {
            if (!bool.TryParse(revocationValue, out bool revoked) || revoked)
            {
                return Task.FromResult(CredentialResolution.Revoked(request));
            }
        }

        string? secret = _readEnvironmentVariable(binding.SecretEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Task.FromResult(CredentialResolution.Missing(request));
        }

        CredentialLease lease = new(
            request.Purpose,
            request.Reference,
            secret);
        return Task.FromResult(CredentialResolution.Resolved(lease));
    }

    private readonly record struct BindingKey(
        CredentialPurpose Purpose,
        CredentialReference Reference);
}
