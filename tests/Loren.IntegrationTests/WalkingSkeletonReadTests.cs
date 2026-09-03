using System.Net;
using System.Text;
using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Infrastructure.Audit;
using Loren.Tools.GitHub;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class WalkingSkeletonReadTests
{
    [Fact]
    public async Task FakeBrainReadsGitHubThroughGatewayAndReceivesStructuredResult()
    {
        const string repositoryJson = """
            {
              "full_name": "rua-den/loren",
              "default_branch": "main",
              "private": false,
              "archived": false,
              "open_issues_count": 2,
              "pushed_at": "2026-09-03T08:39:56Z",
              "html_url": "https://github.com/rua-den/loren"
            }
            """;

        HttpClient httpClient = new(new StubHttpMessageHandler(repositoryJson));
        GitHubReadRepositoryExecutor executor = new(httpClient);
        InMemoryAuditSink audit = new();
        ActionGateway gateway = new(
            [GitHubActions.ReadRepository],
            [executor],
            new ReadOnlyActionPolicy(),
            audit);
        VerifyingBrain brain = new();
        AgentLoop loop = new(brain, gateway, new AgentLoopOptions());

        AgentRunResult result = await loop.RunAsync(
            BrainContext.FromUser("Loren, check repo rua-den/loren."),
            [GitHubActions.ReadRepository],
            CancellationToken.None);

        Assert.Equal("Repository rua-den/loren is on main.", result.FinalOutput);
        IReadOnlyList<AuditEvent> events = audit.Snapshot();
        Assert.Equal(3, events.Count);
        Assert.All(events, auditEvent => Assert.Equal(result.RunId, auditEvent.RunId));
        Assert.Equal("github.read_repository", events[0].ActionName);
        Assert.Equal("succeeded", events[^1].Outcome);
    }

    private sealed class VerifyingBrain : IBrain
    {
        private int _turn;

        public Task<BrainTurnResult> ThinkAsync(
            BrainContext context,
            IReadOnlyList<ActionDefinition> availableActions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _turn++;

            if (_turn == 1)
            {
                Assert.Contains(
                    availableActions,
                    action => action.Name == GitHubActions.ReadRepository.Name && action.IsReadOnly);
                return Task.FromResult(BrainTurnResult.Request(new ActionRequest(
                    GitHubActions.ReadRepository.Name,
                    new Dictionary<string, string>
                    {
                        ["owner"] = "rua-den",
                        ["repository"] = "loren",
                    })));
            }

            BrainActionObservation observation = Assert.IsType<BrainActionObservation>(context.Inputs[^1]);
            Assert.True(observation.Result.Success);
            Assert.Equal("rua-den/loren", observation.Result.Data["full_name"]);
            Assert.Equal("main", observation.Result.Data["default_branch"]);
            return Task.FromResult(BrainTurnResult.Final("Repository rua-den/loren is on main."));
        }
    }

    private sealed class StubHttpMessageHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.github.com/repos/rua-den/loren", request.RequestUri?.ToString());
            Assert.Contains(request.Headers.UserAgent, value => value.Product?.Name == "Loren");

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
