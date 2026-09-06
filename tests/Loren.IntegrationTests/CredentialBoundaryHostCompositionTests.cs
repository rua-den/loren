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
    public void ProductionHostRegistersWriteCredentialResolverWithoutMutationExecutor()
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
                .ToArray();
            IWriteSafetyState writeSafetyState =
                provider.GetRequiredService<IWriteSafetyState>();

            Assert.IsType<EnvironmentActionCredentialResolver>(resolver);
            Assert.False(writeSafetyState.IsReadOnly);
            IActionExecutor readExecutor = Assert.Single(executors);
            Assert.IsType<GitHubReadRepositoryExecutor>(readExecutor);
            Assert.Equal(GitHubActions.ReadRepository.Name, readExecutor.ActionName);
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
