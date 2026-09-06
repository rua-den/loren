using Loren.Core.Credentials;
using Xunit;

namespace Loren.Core.Tests;

public sealed class ActionCredentialTests
{
    [Fact]
    public void CredentialPurposeIsNormalizedDeterministically()
    {
        CredentialPurpose purpose = new("  GitHub.Write  ");

        Assert.Equal("github.write", purpose.Value);
        Assert.Equal("github.write", purpose.ToString());
    }

    [Fact]
    public void CredentialLeaseNeverExposesSecretThroughMetadataOrToString()
    {
        const string secret = "slice2-test-secret";
        CredentialLease lease = new(
            new CredentialPurpose("github.write"),
            new CredentialReference("github.write.local-v0.1"),
            secret);
        CredentialResolution resolution = CredentialResolution.Resolved(lease);

        Assert.DoesNotContain(secret, lease.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(secret, resolution.ToString(), StringComparison.Ordinal);
        Assert.Contains(CredentialLease.RedactedValue, lease.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CredentialLeaseRedactsExactSecretFromOutwardText()
    {
        const string secret = "slice2-test-secret";
        CredentialLease lease = new(
            new CredentialPurpose("github.write"),
            new CredentialReference("github.write.local-v0.1"),
            secret);

        string redacted = lease.Redact($"failure before {secret} after {secret}");

        Assert.DoesNotContain(secret, redacted, StringComparison.Ordinal);
        Assert.Equal(
            "failure before [REDACTED] after [REDACTED]",
            redacted);
    }

    [Fact]
    public void CredentialLeaseOnlyRevealsSecretInsideExplicitUseCallback()
    {
        const string secret = "slice2-test-secret";
        CredentialLease lease = new(
            new CredentialPurpose("github.write"),
            new CredentialReference("github.write.local-v0.1"),
            secret);

        string observed = lease.Use(value => value);

        Assert.Equal(secret, observed);
    }
}
