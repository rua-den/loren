using Loren.Core.Credentials;
using Loren.Infrastructure.Credentials;
using Loren.Web;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public async Task ConfigurationOverrideWinsForSecretAndRevocation()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string secretEnvironmentVariable = "LOREN_TEST_WRITE_TOKEN";
        string revocationEnvironmentVariable = "LOREN_TEST_WRITE_REVOKED";
        string? previousSecret = Environment.GetEnvironmentVariable(secretEnvironmentVariable);
        string? previousRevocation = Environment.GetEnvironmentVariable(revocationEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(secretEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(revocationEnvironmentVariable, null);
            File.WriteAllText(
                Path.Combine(directory, "appsettings.Local.json"),
                "{\"LOREN_TEST_WRITE_TOKEN\":\"local-token\",\"LOREN_TEST_WRITE_REVOKED\":\"false\"}");
            using (ConfigurationManager localConfiguration = new())
            {
                localConfiguration.SetBasePath(directory);
                LorenConfiguration.AddLocalConfigurationBeforeEnvironmentOverrides(localConfiguration, []);
                CredentialResolution localResolution = await CreateResolver(localConfiguration, secretEnvironmentVariable, revocationEnvironmentVariable)
                    .ResolveAsync(new CredentialResolutionRequest(Purpose, Reference), TestContext.Current.CancellationToken);

                Assert.Equal(CredentialResolutionStatus.Resolved, localResolution.Status);
                Assert.Equal("local-token", Assert.IsType<CredentialLease>(localResolution.Lease).Use(value => value));
            }

            File.Delete(Path.Combine(directory, "appsettings.Local.json"));
            using (ConfigurationManager missingLocalConfiguration = new())
            {
                missingLocalConfiguration.SetBasePath(directory);
                LorenConfiguration.AddLocalConfigurationBeforeEnvironmentOverrides(missingLocalConfiguration, []);
                CredentialResolution missingResolution = await CreateResolver(missingLocalConfiguration, secretEnvironmentVariable, revocationEnvironmentVariable)
                    .ResolveAsync(new CredentialResolutionRequest(Purpose, Reference), TestContext.Current.CancellationToken);

                Assert.Equal(CredentialResolutionStatus.Missing, missingResolution.Status);
            }

            File.WriteAllText(
                Path.Combine(directory, "appsettings.Local.json"),
                "{\"LOREN_TEST_WRITE_TOKEN\":\"local-token\",\"LOREN_TEST_WRITE_REVOKED\":\"false\"}");

            Environment.SetEnvironmentVariable(secretEnvironmentVariable, "environment-token");
            Environment.SetEnvironmentVariable(revocationEnvironmentVariable, "true");
            using (ConfigurationManager environmentConfiguration = new())
            {
                environmentConfiguration.SetBasePath(directory);
                LorenConfiguration.AddLocalConfigurationBeforeEnvironmentOverrides(environmentConfiguration, []);
                CredentialResolution environmentResolution = await CreateResolver(environmentConfiguration, secretEnvironmentVariable, revocationEnvironmentVariable)
                    .ResolveAsync(new CredentialResolutionRequest(Purpose, Reference), TestContext.Current.CancellationToken);

                Assert.Equal(CredentialResolutionStatus.Revoked, environmentResolution.Status);
                Assert.Equal("environment-token", environmentConfiguration[secretEnvironmentVariable]);
                Assert.Equal("true", environmentConfiguration[revocationEnvironmentVariable]);
            }

            Environment.SetEnvironmentVariable(revocationEnvironmentVariable, "false");
            using (ConfigurationManager commandLineConfiguration = new())
            {
                commandLineConfiguration.SetBasePath(directory);
                LorenConfiguration.AddLocalConfigurationBeforeEnvironmentOverrides(
                    commandLineConfiguration,
                    [$"--{secretEnvironmentVariable}=command-line-token"]);
                CredentialResolution commandLineResolution = await CreateResolver(commandLineConfiguration, secretEnvironmentVariable, revocationEnvironmentVariable)
                    .ResolveAsync(new CredentialResolutionRequest(Purpose, Reference), TestContext.Current.CancellationToken);

                Assert.Equal(CredentialResolutionStatus.Resolved, commandLineResolution.Status);
                Assert.Equal("command-line-token", Assert.IsType<CredentialLease>(commandLineResolution.Lease).Use(value => value));
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretEnvironmentVariable, previousSecret);
            Environment.SetEnvironmentVariable(revocationEnvironmentVariable, previousRevocation);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static EnvironmentActionCredentialResolver CreateResolver(
        ConfigurationManager configuration,
        string secretEnvironmentVariable,
        string revocationEnvironmentVariable)
    {
        return new EnvironmentActionCredentialResolver(
            [new EnvironmentCredentialBinding(
                Purpose,
                Reference,
                secretEnvironmentVariable,
                revocationEnvironmentVariable)],
            key => configuration[key]);
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
