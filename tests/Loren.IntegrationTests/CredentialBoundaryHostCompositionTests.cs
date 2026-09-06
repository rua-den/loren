using Loren.Core.Actions;
using Loren.Core.Credentials;
using Loren.Infrastructure.Credentials;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Loren.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CredentialBoundaryHostCompositionTests
{
    [Fact]
    public void ProductionHostRegistersOnlyReadAndTrustedCreateBranchExecutors()
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
            Assert.Equal(2, executors.Length);

            IActionExecutor createBranchExecutor = Assert.Single(
                executors,
                executor => executor.ActionName == GitHubActions.CreateBranch.Name);
            Assert.IsType<GitHubCreateBranchActionExecutor>(createBranchExecutor);
            Assert.IsAssignableFrom<ITrustedActionExecutor>(createBranchExecutor);

            IActionExecutor readExecutor = Assert.Single(
                executors,
                executor => executor.ActionName == GitHubActions.ReadRepository.Name);
            Assert.IsType<GitHubReadRepositoryExecutor>(readExecutor);

            Assert.All(
                executors.Where(executor => executor.ActionName != GitHubActions.ReadRepository.Name),
                executor => Assert.Equal(GitHubActions.CreateBranch.Name, executor.ActionName));
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
