using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class ConversationalApprovalEndpointTests
{
    [Theory]
    [InlineData("00000000000000000000000000000000")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public async Task AuthenticatedInvalidProposalIdsReturn404ForApproveAndCancel(string proposalId)
    {
        using EndpointFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.PostAsJsonAsync("/auth/login", new { password = "test-password" }, TestContext.Current.CancellationToken);

        HttpResponseMessage approve = await client.PostAsync(
            $"/api/action-proposals/{proposalId}/approve",
            JsonContent.Create(new { branch = "attacker" }),
            TestContext.Current.CancellationToken);
        HttpResponseMessage cancel = await client.PostAsync(
            $"/api/action-proposals/{proposalId}/cancel",
            JsonContent.Create(new { branch = "attacker" }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, approve.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
    }

    [Fact]
    public async Task ActualPipelineRequiresOwnerCookieAndAcceptsOnlyRouteProposalId()
    {
        using EndpointFactory factory = new();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpClient anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        HttpResponseMessage anonymousResponse = await anonymous.PostAsync(
            "/api/action-proposals/10000000000000000000000000000000/approve",
            JsonContent.Create(new { branch = "attacker", source_sha = "fake" }),
            cancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        HttpResponseMessage login = await client.PostAsJsonAsync("/auth/login", new { password = "test-password" }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        HttpResponseMessage unknown = await client.PostAsJsonAsync(
            "/api/action-proposals/10000000000000000000000000000000/approve",
            new { repository_id = "foreign", branch = "main", source_sha = "fake" },
            cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        HttpResponseMessage cancel = await client.PostAsync(
            "/api/action-proposals/10000000000000000000000000000000/cancel?branch=main",
            JsonContent.Create(new { owner = "attacker" }),
            cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
    }

    private sealed class EndpointFactory : WebApplicationFactory<Program>
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"loren-http-{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(_directory);
            builder.UseEnvironment("Development");
            builder.UseSetting("LOREN_DATA_DIRECTORY", _directory);
            builder.UseSetting("LOREN_OWNER_PASSWORD", "test-password");
            builder.UseSetting("LOREN_ENABLE_WRITES", "false");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LOREN_DATA_DIRECTORY"] = _directory,
                    ["LOREN_OWNER_PASSWORD"] = "test-password",
                    ["LOREN_ENABLE_WRITES"] = "false",
                }));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
