using Loren.Core.Credentials;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Loren.Tools.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Loren.Web;

public sealed class LorenReadinessService
{
    private readonly CanonicalStateDbContext _dbContext;
    private readonly IProjectCatalog _projectCatalog;
    private readonly OwnerPasswordAuthenticator _ownerAuthenticator;
    private readonly IActionCredentialResolver _credentialResolver;
    private readonly IConfiguration _configuration;

    public LorenReadinessService(
        CanonicalStateDbContext dbContext,
        IProjectCatalog projectCatalog,
        OwnerPasswordAuthenticator ownerAuthenticator,
        IActionCredentialResolver credentialResolver,
        IConfiguration configuration)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _projectCatalog = projectCatalog ?? throw new ArgumentNullException(nameof(projectCatalog));
        _ownerAuthenticator = ownerAuthenticator ?? throw new ArgumentNullException(nameof(ownerAuthenticator));
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<LorenReadinessReport> GetAsync(
        CancellationToken cancellationToken = default)
    {
        bool storageReady = await CanConnectAsync(cancellationToken);
        ProjectState projectState = storageReady
            ? await GetProjectStateAsync(cancellationToken)
            : new ProjectState(false, 0);
        bool ownerAuthenticationReady = _ownerAuthenticator.IsConfigured;
        bool brainConfigured = IsBrainConfigured();
        bool webResearchConfigured = IsWebResearchConfigured();
        bool writesEnabled = string.Equals(
            _configuration["LOREN_ENABLE_WRITES"],
            "true",
            StringComparison.OrdinalIgnoreCase);
        LorenExternalWriteReadiness externalWrites = await GetExternalWriteReadinessAsync(
            writesEnabled,
            cancellationToken);

        bool externalWriteConfigurationReady =
            !writesEnabled || string.Equals(externalWrites.Status, "ready", StringComparison.Ordinal);
        string overallStatus = storageReady
            && projectState.Available
            && ownerAuthenticationReady
            && brainConfigured
            && webResearchConfigured
            && externalWriteConfigurationReady
                ? "ready"
                : "needs_setup";
        string projectStatus = projectState switch
        {
            { Available: false } => "unavailable",
            { Count: > 0 } => "ready",
            _ => "empty",
        };

        return new LorenReadinessReport(
            overallStatus,
            new LorenReadinessComponent(storageReady ? "ready" : "unavailable"),
            new LorenReadinessComponent(ownerAuthenticationReady ? "ready" : "not_configured"),
            new LorenReadinessComponent(brainConfigured ? "configured" : "invalid_configuration"),
            new LorenReadinessComponent(webResearchConfigured ? "configured" : "not_configured"),
            new LorenProjectReadiness(projectStatus, projectState.Count),
            externalWrites);
    }

    private async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private async Task<ProjectState> GetProjectStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<ProjectSnapshot> projects = await _projectCatalog.ListAsync(cancellationToken);
            return new ProjectState(true, projects.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new ProjectState(false, 0);
        }
    }

    private bool IsBrainConfigured()
    {
        string model = _configuration["LOREN_OLLAMA_MODEL"] ?? "gpt-oss:120b";
        return !string.IsNullOrWhiteSpace(model)
            && IsHttpEndpointConfigured(
                "LOREN_OLLAMA_ENDPOINT",
                "https://ollama.com/api/chat");
    }

    private bool IsWebResearchConfigured() =>
        !string.IsNullOrWhiteSpace(_configuration["OLLAMA_API_KEY"])
        && IsHttpEndpointConfigured(
            "LOREN_OLLAMA_WEB_SEARCH_ENDPOINT",
            "https://ollama.com/api/web_search")
        && IsHttpEndpointConfigured(
            "LOREN_OLLAMA_WEB_FETCH_ENDPOINT",
            "https://ollama.com/api/web_fetch");

    private bool IsHttpEndpointConfigured(string key, string defaultValue)
    {
        string endpointValue = _configuration[key] ?? defaultValue;
        return Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? endpoint)
            && endpoint.Scheme is "http" or "https";
    }

    private async Task<LorenExternalWriteReadiness> GetExternalWriteReadinessAsync(
        bool writesEnabled,
        CancellationToken cancellationToken)
    {
        if (!writesEnabled)
        {
            return new LorenExternalWriteReadiness("disabled", false);
        }

        CredentialResolution resolution = await _credentialResolver.ResolveAsync(
            new CredentialResolutionRequest(
                GitHubCredentials.WritePurpose,
                GitHubCredentials.LocalV01WriteReference),
            cancellationToken);
        string status = resolution.Status switch
        {
            CredentialResolutionStatus.Resolved => "ready",
            CredentialResolutionStatus.Missing => "missing_credential",
            CredentialResolutionStatus.Revoked => "revoked",
            CredentialResolutionStatus.NotConfigured => "not_configured",
            _ => "not_configured",
        };
        return new LorenExternalWriteReadiness(status, true);
    }

    private sealed record ProjectState(bool Available, int Count);
}

public sealed record LorenReadinessReport(
    string Status,
    LorenReadinessComponent Storage,
    LorenReadinessComponent OwnerAuthentication,
    LorenReadinessComponent Brain,
    LorenReadinessComponent WebResearch,
    LorenProjectReadiness Projects,
    LorenExternalWriteReadiness ExternalWrites);

public sealed record LorenReadinessComponent(string Status);

public sealed record LorenProjectReadiness(string Status, int Count);

public sealed record LorenExternalWriteReadiness(string Status, bool Enabled);
