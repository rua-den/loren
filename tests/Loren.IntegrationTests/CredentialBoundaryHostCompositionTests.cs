using Loren.Core.Actions;
using Loren.Core.Credentials;
using Loren.Infrastructure.Credentials;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Loren.Tools.Web;
using Loren.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CredentialBoundaryHostCompositionTests
{
    [Fact]
    public void ProductionHostRegistersReadExecutorsAndOnlyOneTrustedMutationExecutor()
    {
        string dataDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-credential-host-{Guid.NewGuid():N}");

        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LOREN_DATA_DIRECTORY"] = dataDirectory,
                    ["LOREN_ENABLE_WRITES"] = "true",
                    ["OLLAMA_API_KEY"] = "host-composition-test-key",
                })
                .Build();
            ServiceCollection services = new();
            services.AddLorenM2ReadPath(configuration);

            using ServiceProvider provider = services.BuildServiceProvider();

            IActionCredentialResolver resolver =
                provider.GetRequiredService<IActionCredentialResolver>();
            IActionExecutor[] executors = provider
                .GetServices<IActionExecutor>()
                .OrderBy(executor => executor.ActionName, StringComparer.Ordinal)
                .ToArray();
            IWriteSafetyState writeSafetyState =
                provider.GetRequiredService<IWriteSafetyState>();

            Assert.IsType<EnvironmentActionCredentialResolver>(resolver);
            Assert.False(writeSafetyState.IsReadOnly);
            Assert.Equal(3, executors.Length);

            IActionExecutor createBranchExecutor = Assert.Single(
                executors,
                executor => executor.ActionName == GitHubActions.CreateBranch.Name);
            Assert.IsType<GitHubCreateBranchActionExecutor>(createBranchExecutor);
            Assert.IsAssignableFrom<ITrustedActionExecutor>(createBranchExecutor);

            IActionExecutor repositoryReadExecutor = Assert.Single(
                executors,
                executor => executor.ActionName == GitHubActions.ReadRepository.Name);
            Assert.IsType<GitHubReadRepositoryExecutor>(repositoryReadExecutor);

            IActionExecutor webSearchExecutor = Assert.Single(
                executors,
                executor => executor.ActionName == WebActions.Search.Name);
            Assert.IsType<OllamaWebSearchExecutor>(webSearchExecutor);

            IActionExecutor mutationExecutor = Assert.Single(
                executors,
                executor => executor is ITrustedActionExecutor);
            Assert.Equal(GitHubActions.CreateBranch.Name, mutationExecutor.ActionName);
        }
        finally
        {
            if (Directory.Exists(dataDirectory))
            {
                Directory.Delete(dataDirectory, recursive: true);
            }
        }
    }
}
