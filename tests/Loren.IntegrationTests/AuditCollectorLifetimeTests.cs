using Loren.Infrastructure.Audit;
using Loren.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class AuditCollectorLifetimeTests
{
    [Fact]
    public void CurrentRunAuditCollectorIsScopedAndIsolatedBetweenRequests()
    {
        string dataDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-audit-lifetime-{Guid.NewGuid():N}");

        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LOREN_DATA_DIRECTORY"] = dataDirectory,
                    ["OLLAMA_API_KEY"] = "audit-lifetime-test-key",
                })
                .Build();
            ServiceCollection services = new();
            services.AddLorenM2ReadPath(configuration);

            ServiceDescriptor descriptor = Assert.Single(
                services,
                candidate => candidate.ServiceType == typeof(InMemoryAuditSink));
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

            using ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true });
            using IServiceScope firstScope = provider.CreateScope();
            using IServiceScope secondScope = provider.CreateScope();

            InMemoryAuditSink first = firstScope.ServiceProvider
                .GetRequiredService<InMemoryAuditSink>();
            InMemoryAuditSink sameFirst = firstScope.ServiceProvider
                .GetRequiredService<InMemoryAuditSink>();
            InMemoryAuditSink second = secondScope.ServiceProvider
                .GetRequiredService<InMemoryAuditSink>();

            Assert.Same(first, sameFirst);
            Assert.NotSame(first, second);
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
