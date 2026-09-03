using System.Net;
using System.Text;
using System.Text.Json;
using Loren.Brain.Ollama;
using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Infrastructure.Audit;
using Loren.Runtime;
using Loren.Tools.GitHub;
using Loren.Web;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class ProductionReadPathTests
{
    [Fact]
    public async Task ProductionComponentsCompleteOllamaToGitHubReadRoundTrip()
    {
        const string apiKey = "test-provider-secret";
        OllamaSequenceHandler ollamaHandler = new(apiKey);
        GitHubHandler gitHubHandler = new();

        OllamaBrain brain = new(
            new HttpClient(ollamaHandler),
            new OllamaBrainOptions(
                "test-model",
                new Uri("https://ollama.example/api/chat")),
            apiKey);
        GitHubReadRepositoryExecutor gitHubExecutor = new(new HttpClient(gitHubHandler));
        InMemoryAuditSink audit = new();
        ActionGateway gateway = new(
            [GitHubActions.ReadRepository],
            [gitHubExecutor],
            new ReadOnlyActionPolicy(),
            audit);
        AgentLoop loop = new(brain, gateway, new AgentLoopOptions());
        LorenRunService runService = new(loop, audit);

        LorenRunResult result = await runService.RunAsync(
            "Loren, check repo rua-den/loren.",
            CancellationToken.None);

        Assert.Equal("Repository rua-den/loren is on main.", result.FinalOutput);
        Assert.Equal(2, result.Turns);
        Assert.Equal(1, result.ActionCount);
        Assert.Equal(3, result.Audit.Count);
        Assert.Equal(
            [
                AuditEventKind.ActionRequested.ToString(),
                AuditEventKind.PolicyEvaluated.ToString(),
                AuditEventKind.ActionCompleted.ToString(),
            ],
            result.Audit.Select(entry => entry.Kind));
        Assert.Equal("succeeded", result.Audit[^1].Outcome);
        Assert.Equal(2, ollamaHandler.CallCount);
        Assert.Equal(1, gitHubHandler.CallCount);
    }

    private sealed class OllamaSequenceHandler(string expectedApiKey) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal(expectedApiKey, request.Headers.Authorization?.Parameter);

            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Assert.DoesNotContain(expectedApiKey, body, StringComparison.Ordinal);

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            if (CallCount == 1)
            {
                Assert.Equal(
                    "github.read_repository",
                    root.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString());

                return JsonResponse("""
                    {
                      "message": {
                        "role": "assistant",
                        "content": "",
                        "tool_calls": [
                          {
                            "type": "function",
                            "function": {
                              "name": "github.read_repository",
                              "arguments": {
                                "owner": "rua-den",
                                "repository": "loren"
                              }
                            }
                          }
                        ]
                      },
                      "done": true
                    }
                    """);
            }

            JsonElement messages = root.GetProperty("messages");
            int lastMessageIndex = messages.GetArrayLength() - 1;
            Assert.Equal("tool", messages[lastMessageIndex].GetProperty("role").GetString());
            string toolResult = Assert.IsType<string>(
                messages[lastMessageIndex].GetProperty("content").GetString());
            Assert.Contains("rua-den/loren", toolResult, StringComparison.Ordinal);
            Assert.Contains("main", toolResult, StringComparison.Ordinal);

            return JsonResponse("""
                {
                  "message": {
                    "role": "assistant",
                    "content": "Repository rua-den/loren is on main."
                  },
                  "done": true
                }
                """);
        }
    }

    private sealed class GitHubHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.github.com/repos/rua-den/loren", request.RequestUri?.ToString());
            Assert.Null(request.Headers.Authorization);

            return Task.FromResult(JsonResponse("""
                {
                  "full_name": "rua-den/loren",
                  "default_branch": "main",
                  "private": false,
                  "archived": false,
                  "open_issues_count": 2,
                  "pushed_at": "2026-09-03T09:47:46Z",
                  "html_url": "https://github.com/rua-den/loren"
                }
                """));
        }
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };
}
