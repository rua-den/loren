using Loren.Brain.Ollama;
using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Core.Memories;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Infrastructure.CanonicalState;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Loren.Web;

public static class LorenHostServices
{
    private const string OllamaHttpClientName = "loren-ollama";
    private const string GitHubHttpClientName = "loren-github-read";

    public static IServiceCollection AddLorenM2ReadPath(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpClient(OllamaHttpClientName);
        services.AddHttpClient(GitHubHttpClientName);

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
        services.AddScoped<LorenProjectContextBuilder>();

        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IAuditSink>(provider =>
            provider.GetRequiredService<InMemoryAuditSink>());
        services.AddSingleton<IActionPolicy, ReadOnlyActionPolicy>();

        services.AddSingleton<IActionExecutor>(provider =>
        {
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new GitHubReadRepositoryExecutor(
                httpClientFactory.CreateClient(GitHubHttpClientName));
        });

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

        services.AddSingleton<IActionGateway>(provider =>
            new ActionGateway(
                [GitHubActions.ReadRepository],
                provider.GetServices<IActionExecutor>(),
                provider.GetRequiredService<IActionPolicy>(),
                provider.GetRequiredService<IAuditSink>()));

        services.AddSingleton(provider =>
            new AgentLoop(
                provider.GetRequiredService<IBrain>(),
                provider.GetRequiredService<IActionGateway>(),
                new AgentLoopOptions()));

        services.AddScoped<LorenRunService>();
        return services;
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
