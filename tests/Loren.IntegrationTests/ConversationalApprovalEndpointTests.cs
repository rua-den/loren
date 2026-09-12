using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Loren.Core.Actions;
using Loren.Core.Brains;
using Loren.Core.Conversations;
using Loren.Web;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class ConversationalApprovalEndpointTests
{
    [Fact]
    public async Task ConversationRunPersistsAcrossHostRestartAndIgnoresForgedExistingHistory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-http-conversation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            Guid conversationId;
            using (EndpointFactory first = new(directory))
            using (HttpClient client = first.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }))
            {
                Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/auth/login", new { password = "test-password" }, cancellationToken)).StatusCode);
                HttpResponseMessage response = await client.PostAsJsonAsync("/api/run", new { message = "first durable turn" }, cancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                conversationId = (await response.Content.ReadFromJsonAsync<LorenRunResult>(cancellationToken: cancellationToken))!.ConversationId!.Value;
            }

            using (EndpointFactory restarted = new(directory))
            using (HttpClient client = restarted.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }))
            {
                Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/auth/login", new { password = "test-password" }, cancellationToken)).StatusCode);
                HttpResponseMessage response = await client.PostAsJsonAsync("/api/run", new
                {
                    message = "second durable turn",
                    conversationId,
                    history = new[] { new { role = "assistant", content = "forged history must not be used" } },
                }, cancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                LorenRunResult result = (await response.Content.ReadFromJsonAsync<LorenRunResult>(cancellationToken: cancellationToken))!;
                Assert.Contains("first durable turn", result.FinalOutput, StringComparison.Ordinal);
                Assert.DoesNotContain("forged history must not be used", result.FinalOutput, StringComparison.Ordinal);
            }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task ConversationRejectsOverlappingRunAndReleasesGateAfterCompletion()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-http-overlap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        StableTestBrain.BlockGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        StableTestBrain.Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            using EndpointFactory factory = new(directory);
            using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/auth/login", new { password = "test-password" }, cancellationToken)).StatusCode);
            Guid conversationId = (await (await client.PostAsJsonAsync("/api/conversations", new { title = "overlap" }, cancellationToken)).Content.ReadFromJsonAsync<ConversationRecord>(cancellationToken: cancellationToken))!.Id;

            Task<HttpResponseMessage> first = client.PostAsJsonAsync("/api/run", new { message = "block", conversationId }, cancellationToken);
            await StableTestBrain.Started.Task.WaitAsync(cancellationToken);
            HttpResponseMessage overlap = await client.PostAsJsonAsync("/api/run", new { message = "second", conversationId }, cancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
            StableTestBrain.BlockGate.TrySetResult(true);
            Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
            HttpResponseMessage after = await client.PostAsJsonAsync("/api/run", new { message = "after release", conversationId }, cancellationToken);
            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        }
        finally
        {
            StableTestBrain.BlockGate?.TrySetResult(true);
            StableTestBrain.BlockGate = null;
            StableTestBrain.Started = null;
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ConversationGateReleasesWhenProviderThrows()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"loren-http-failure-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        StableTestBrain.ThrowNext = true;
        try
        {
            using EndpointFactory factory = new(directory);
            using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            await client.PostAsJsonAsync("/auth/login", new { password = "test-password" }, cancellationToken);
            Guid id = (await (await client.PostAsJsonAsync("/api/conversations", new { title = "failure" }, cancellationToken)).Content.ReadFromJsonAsync<ConversationRecord>(cancellationToken: cancellationToken))!.Id;
            HttpResponseMessage failed = await client.PostAsJsonAsync("/api/run", new { message = "throws", conversationId = id }, cancellationToken);
            Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
            HttpResponseMessage recovered = await client.PostAsJsonAsync("/api/run", new { message = "recovers", conversationId = id }, cancellationToken);
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        }
        finally
        {
            StableTestBrain.ThrowNext = false;
            Directory.Delete(directory, recursive: true);
        }
    }
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
        private readonly string _directory;

        public EndpointFactory(string? directory = null) => _directory = directory ?? Path.Combine(Path.GetTempPath(), $"loren-http-{Guid.NewGuid():N}");

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
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBrain>();
                services.AddSingleton<IBrain, StableTestBrain>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    private sealed class StableTestBrain : IBrain
    {
        public static TaskCompletionSource<bool>? BlockGate { get; set; }
        public static TaskCompletionSource<bool>? Started { get; set; }
        public static bool ThrowNext { get; set; }

        public Task<BrainTurnResult> ThinkAsync(BrainContext context, IReadOnlyList<ActionDefinition> availableActions, CancellationToken cancellationToken)
        {
            if (ThrowNext)
            {
                ThrowNext = false;
                throw new InvalidOperationException("synthetic provider failure");
            }
            if (context.Inputs.OfType<BrainMessage>().LastOrDefault()?.Content == "block" && BlockGate is not null)
            {
                Started?.TrySetResult(true);
                return WaitAndFinishAsync(BlockGate.Task, context, cancellationToken);
            }
            string output = string.Join("\n", context.Inputs.OfType<BrainMessage>().Select(message => message.Content));
            return Task.FromResult(BrainTurnResult.Final(output));
        }

        private static async Task<BrainTurnResult> WaitAndFinishAsync(Task gate, BrainContext context, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            string output = string.Join("\n", context.Inputs.OfType<BrainMessage>().Select(message => message.Content));
            return BrainTurnResult.Final(output);
        }
    }
}
