using Loren.Core.Credentials;
using Loren.Infrastructure.Credentials;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class EnvironmentActionCredentialResolverTests
{
    private static readonly CredentialPurpose Purpose = new("github.write");
    private static readonly CredentialReference Reference = new("github.write.local-v0.1");

    [Fact]
    public async Task ExactConfiguredCredentialResolvesWithoutExposingSecretInMetadata()
    {
        const string secret = "slice2-environment-secret";
        Dictionary<string, string?> environment = new(StringComparer.Ordinal)
        {
            ["TEST_GITHUB_WRITE_TOKEN"] = secret,
            ["TEST_GITHUB_WRITE_REVOKED"] = "false",
        };
        EnvironmentActionCredentialResolver resolver = CreateResolver(environment);

        CredentialResolution resolution = await resolver.ResolveAsync(
            new CredentialResolutionRequest(Purpose, Reference),
            CancellationToken.None);

        Assert.True(resolution.IsResolved);
        CredentialLease lease = Assert.IsType<CredentialLease>(resolution.Lease);
        Assert.Equal(secret, lease.Use(value => value));
        Assert.DoesNotContain(secret, lease.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(secret, resolution.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingSecretFailsClosed()
    {
        Dictionary<string, string?> environment = new(StringComparer.Ordinal)
        {
            ["TEST_GITHUB_WRITE_REVOKED"] = "false",
        };
        EnvironmentActionCredentialResolver resolver = CreateResolver(environment);

        CredentialResolution resolution = await resolver.ResolveAsync(
            new CredentialResolutionRequest(Purpose, Reference),
            CancellationToken.None);

        Assert.Equal(CredentialResolutionStatus.Missing, resolution.Status);
        Assert.False(resolution.IsResolved);
        Assert.Null(resolution.Lease);
    }

    [Fact]
    public async Task RevocationOverridesPresentSecret()
    {
        Dictionary<string, string?> environment = new(StringComparer.Ordinal)
        {
            ["TEST_GITHUB_WRITE_TOKEN"] = "present-but-revoked",
            ["TEST_GITHUB_WRITE_REVOKED"] = "true",
        };
        EnvironmentActionCredentialResolver resolver = CreateResolver(environment);

        CredentialResolution resolution = await resolver.ResolveAsync(
            new CredentialResolutionRequest(Purpose, Reference),
            CancellationToken.None);

        Assert.Equal(CredentialResolutionStatus.Revoked, resolution.Status);
        Assert.False(resolution.IsResolved);
        Assert.Null(resolution.Lease);
    }

    [Fact]
    public async Task MalformedRevocationStateFailsClosedAsRevoked()
    {
        Dictionary<string, string?> environment = new(StringComparer.Ordinal)
        {
            ["TEST_GITHUB_WRITE_TOKEN"] = "present-but-revocation-is-ambiguous",
            ["TEST_GITHUB_WRITE_REVOKED"] = "not-a-boolean",
        };
        EnvironmentActionCredentialResolver resolver = CreateResolver(environment);

        CredentialResolution resolution = await resolver.ResolveAsync(
            new CredentialResolutionRequest(Purpose, Reference),
            CancellationToken.None);

        Assert.Equal(CredentialResolutionStatus.Revoked, resolution.Status);
        Assert.False(resolution.IsResolved);
    }

    [Fact]
    public async Task DifferentReferenceDoesNotFallbackToConfiguredBroaderCredential()
    {
        const string secret = "must-not-be-fallback-token";
        Dictionary<string, string?> environment = new(StringComparer.Ordinal)
        {
            ["TEST_GITHUB_WRITE_TOKEN"] = secret,
            ["TEST_GITHUB_WRITE_REVOKED"] = "false",
        };
        EnvironmentActionCredentialResolver resolver = CreateResolver(environment);
        CredentialReference differentReference = new("github.write.other");

        CredentialResolution resolution = await resolver.ResolveAsync(
            new CredentialResolutionRequest(Purpose, differentReference),
            CancellationToken.None);

        Assert.Equal(CredentialResolutionStatus.NotConfigured, resolution.Status);
        Assert.False(resolution.IsResolved);
        Assert.Null(resolution.Lease);
        Assert.DoesNotContain(secret, resolution.ToString(), StringComparison.Ordinal);
    }

    private static EnvironmentActionCredentialResolver CreateResolver(
        Dictionary<string, string?> environment)
    {
        EnvironmentCredentialBinding binding = new(
            Purpose,
            Reference,
            "TEST_GITHUB_WRITE_TOKEN",
            "TEST_GITHUB_WRITE_REVOKED");

        return new EnvironmentActionCredentialResolver(
            [binding],
            variable => environment.TryGetValue(variable, out string? value)
                ? value
                : null);
    }
}
