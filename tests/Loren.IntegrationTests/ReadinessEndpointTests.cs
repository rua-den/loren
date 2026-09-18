using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Loren.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class ReadinessEndpointTests
{
    [Fact]
    public async Task HealthRemainsPublicAndDetailedReadinessRequiresOwnerAuthentication()
    {
        string directory = CreateDirectory();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            using ReadinessFactory factory = new(directory, writesEnabled: false);
            using HttpClient client = factory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            Assert.Equal(
                HttpStatusCode.OK,
                (await client.GetAsync("/health", cancellationToken)).StatusCode);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await client.GetAsync("/api/readiness", cancellationToken)).StatusCode);

            await LoginAsync(client, cancellationToken);

            HttpResponseMessage response = await client.GetAsync("/api/readiness", cancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            LorenReadinessReport report = JsonSerializer.Deserialize<LorenReadinessReport>(
                body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

            Assert.Equal("ready", report.Status);
            Assert.Equal("ready", report.Storage.Status);
            Assert.Equal("ready", report.OwnerAuthentication.Status);
            Assert.Equal("configured", report.Brain.Status);
            Assert.Equal("configured", report.WebResearch.Status);
            Assert.Equal("empty", report.Projects.Status);
            Assert.Equal(0, report.Projects.Count);
            Assert.Equal("disabled", report.ExternalWrites.Status);
            Assert.False(report.ExternalWrites.Enabled);
            Assert.DoesNotContain("provider-secret", body, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task EnabledWritesWithoutCredentialAreReportedWithoutBlockingProjectDiagnostics()
    {
        string directory = CreateDirectory();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            using ReadinessFactory factory = new(directory, writesEnabled: true);
            using HttpClient client = factory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            await LoginAsync(client, cancellationToken);

            using (IServiceScope scope = factory.Services.CreateScope())
            {
                IProjectCatalog catalog = scope.ServiceProvider.GetRequiredService<IProjectCatalog>();
                ProjectId projectId = ProjectId.New();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                await catalog.SaveAsync(
                    new ProjectSnapshot(
                        new Project(projectId, "Loren", ["loren"], now, now),
                        []),
                    cancellationToken);
            }

            LorenReadinessReport report = (await client.GetFromJsonAsync<LorenReadinessReport>(
                "/api/readiness",
                cancellationToken))!;

            Assert.Equal("needs_setup", report.Status);
            Assert.Equal("ready", report.Projects.Status);
            Assert.Equal(1, report.Projects.Count);
            Assert.True(report.ExternalWrites.Enabled);
            Assert.Equal("missing_credential", report.ExternalWrites.Status);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RevokedWriteCredentialIsReportedWithoutReturningCredentialValues()
    {
        string directory = CreateDirectory();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            using ReadinessFactory factory = new(
                directory,
                writesEnabled: true,
                writeToken: "write-secret",
                writeCredentialRevoked: "true");
            using HttpClient client = factory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            await LoginAsync(client, cancellationToken);

            HttpResponseMessage response = await client.GetAsync("/api/readiness", cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            LorenReadinessReport report = JsonSerializer.Deserialize<LorenReadinessReport>(
                body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

            Assert.Equal("needs_setup", report.Status);
            Assert.Equal("revoked", report.ExternalWrites.Status);
            Assert.DoesNotContain("write-secret", body, StringComparison.Ordinal);
            Assert.DoesNotContain("provider-secret", body, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"loren-readiness-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task LoginAsync(HttpClient client, CancellationToken cancellationToken)
    {
        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/auth/login",
            new { password = "test-password" },
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private sealed class ReadinessFactory : WebApplicationFactory<Program>
    {
        private readonly string _directory;
        private readonly bool _writesEnabled;
        private readonly string? _writeToken;
        private readonly string? _writeCredentialRevoked;

        public ReadinessFactory(
            string directory,
            bool writesEnabled,
            string? writeToken = null,
            string? writeCredentialRevoked = null)
        {
            _directory = directory;
            _writesEnabled = writesEnabled;
            _writeToken = writeToken;
            _writeCredentialRevoked = writeCredentialRevoked;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Dictionary<string, string?> settings = new()
            {
                ["LOREN_DATA_DIRECTORY"] = _directory,
                ["LOREN_OWNER_PASSWORD"] = "test-password",
                ["OLLAMA_API_KEY"] = "provider-secret",
                ["LOREN_ENABLE_WRITES"] = _writesEnabled ? "true" : "false",
                ["GITHUB_WRITE_TOKEN"] = _writeToken,
                ["LOREN_GITHUB_WRITE_CREDENTIAL_REVOKED"] = _writeCredentialRevoked,
            };

            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<CanonicalStateDbContext>();
                services.RemoveAll<DbContextOptions<CanonicalStateDbContext>>();
                string connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = Path.Combine(_directory, "loren.db"),
                    Pooling = false,
                }.ToString();
                services.AddDbContext<CanonicalStateDbContext>(options =>
                    options.UseSqlite(connectionString));
            });
        }
    }
}
