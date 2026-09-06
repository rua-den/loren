using Loren.Brain.Ollama;
using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Core.Credentials;
using Loren.Core.Memories;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Infrastructure.CanonicalState;
using Loren.Infrastructure.Credentials;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Loren.Tools.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Loren.Web;

public static class LorenHostServices
{
    private const string OllamaHttpClientName = "loren-ollama";
    private const string OllamaWebSearchHttpClientName = "loren-ollama-web-search";
    private const string OllamaWebFetchHttpClientName = "loren-ollama-web-fetch";
    private const string GitHubReadHttpClientName = "loren-github-read";
    private const string GitHubWriteHttpClientName = "loren-github-write";

    public static IServiceCollection AddLorenM2ReadPath(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpClient(OllamaHttpClientName);
        services.AddHttpClient(OllamaWebSearchHttpClientName);
        services.AddHttpClient(OllamaWebFetchHttpClientName);
        services.AddHttpClient(GitHubReadHttpClientName);
        services.AddHttpClient(GitHubWriteHttpClientName);

        string dataDirectory = ResolveDataDirectory(configuration);
        Directory.CreateDirectory(dataDirectory);
        string databasePath = Path.Combine(dataDirectory, "loren.db");
        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = databasePath,
        };

        services.AddDbContext<CanonicalStateDbContext>(options =>
            options.UseSqlite(connectionStringBuilder.ConnectionString));
        services.AddScoped<IProjectCatalog, SqliteProjectCatalog>();
        services.AddScoped<IMemoryStore, SqliteMemoryStore>();
        services.AddScoped<IActionApprovalStore, SqliteActionApprovalStore>();
        services.AddSingleton(new LorenMemoryContextOptions());
        services.AddScoped<LorenMemoryContextBuilder>();
        services.AddScoped<LorenProjectContextBuilder>();

        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IAuditSink>(provider =>
            provider.GetRequiredService<InMemoryAuditSink>());

        bool writesEnabled = string.Equals(
            configuration["LOREN_ENABLE_WRITES"],
            "true",
            StringComparison.OrdinalIgnoreCase);
        services.AddSingleton<IWriteSafetyState>(
            new FixedWriteSafetyState(isReadOnly: !writesEnabled));
        services.AddSingleton<IActionPolicy, GateDActionPolicy>();

        services.AddSingleton<IActionCredentialResolver>(
            new EnvironmentActionCredentialResolver(
                [
                    new EnvironmentCredentialBinding(
                        GitHubCredentials.WritePurpose,
                        GitHubCredentials.LocalV01WriteReference,
                        "GITHUB_WRITE_TOKEN",
                        "LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED"),
                ]));

        services.AddSingleton<IActionExecutor>(provider =>
        {
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new GitHubReadRepositoryExecutor(
                httpClientFactory.CreateClient(GitHubReadHttpClientName));
        });

        services.AddSingleton<IActionExecutor>(provider =>
        {
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            Uri endpoint = ResolveHttpEndpoint(
                configuration,
                "LOREN_OLLAMA_WEB_SEARCH_ENDPOINT",
                "https://ollama.com/api/web_search");
            return new OllamaWebSearchExecutor(
                httpClientFactory.CreateClient(OllamaWebSearchHttpClientName),
                new OllamaWebSearchOptions(endpoint),
                configuration["OLLAMA_API_KEY"]);
        });

        services.AddSingleton<IActionExecutor>(provider =>
        {
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            Uri endpoint = ResolveHttpEndpoint(
                configuration,
                "LOREN_OLLAMA_WEB_FETCH_ENDPOINT",
                "https://ollama.com/api/web_fetch");
            return new OllamaWebFetchExecutor(
                httpClientFactory.CreateClient(OllamaWebFetchHttpClientName),
                new OllamaWebFetchOptions(endpoint),
                configuration["OLLAMA_API_KEY"]);
        });

        services.AddSingleton(provider =>
        {
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new GitHubCreateBranchClient(
                httpClientFactory.CreateClient(GitHubWriteHttpClientName));
        });
        services.AddSingleton<IActionExecutor>(provider =>
            new GitHubCreateBranchActionExecutor(
                provider.GetRequiredService<IActionCredentialResolver>(),
                provider.GetRequiredService<GitHubCreateBranchClient>()));

        services.AddSingleton<IBrain>(provider =>
        {
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            string model = configuration["LOREN_OLLAMA_MODEL"] ?? "gpt-oss:120b";
            string endpointValue = configuration["LOREN_OLLAMA_ENDPOINT"] ?? "https://ollama.com/api/chat";

            if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? endpoint))
            {
                throw new InvalidOperationException("LOREN_OLLAMA_ENDPOINT must be an absolute URI.");
            }

            OllamaBrainOptions options = new(model, endpoint);
            string? apiKey = configuration["OLLAMA_API_KEY"];
            return new OllamaBrain(
                httpClientFactory.CreateClient(OllamaHttpClientName),
                options,
                apiKey);
        });

        services.AddScoped<IActionGateway>(provider =>
            new ActionGateway(
                [
                    GitHubActions.ReadRepository,
                    WebActions.Search,
                    WebActions.Fetch,
                    GitHubActions.CreateBranch,
                ],
                provider.GetServices<IActionExecutor>(),
                provider.GetRequiredService<IActionPolicy>(),
                provider.GetRequiredService<IAuditSink>(),
                provider.GetRequiredService<IActionApprovalStore>()));

        services.AddScoped(provider =>
            new AgentLoop(
                provider.GetRequiredService<IBrain>(),
                provider.GetRequiredService<IActionGateway>(),
                new AgentLoopOptions()));

        services.AddScoped<LorenRunService>();
        services.AddScoped<LorenOwnerProjectBootstrapService>();
        services.AddScoped<LorenOwnerGitHubWriteService>();
        return services;
    }

    private static Uri ResolveHttpEndpoint(
        IConfiguration configuration,
        string key,
        string defaultValue)
    {
        string endpointValue = configuration[key] ?? defaultValue;
        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? endpoint)
            || endpoint.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException($"{key} must be an absolute http/https URI.");
        }

        return endpoint;
    }

    private static string ResolveDataDirectory(IConfiguration configuration)
    {
        string? configuredDirectory = configuration["LOREN_DATA_DIRECTORY"];
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return Path.GetFullPath(configuredDirectory);
        }

        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        string baseDirectory = string.IsNullOrWhiteSpace(localApplicationData)
            ? AppContext.BaseDirectory
            : localApplicationData;

        return Path.Combine(baseDirectory, "Loren");
    }
}
