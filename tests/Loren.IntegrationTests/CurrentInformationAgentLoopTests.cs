using System.Net;
using System.Text;
using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Infrastructure.Audit;
using Loren.Runtime;
using Loren.Tools.Web;
using Loren.Web;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CurrentInformationAgentLoopTests
{
    [Fact]
    public async Task ConversationCanSearchCurrentWebAndGroundFinalAnswerInReturnedSource()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string sourceUrl = "https://example.com/current-release";
        FixedJsonHandler handler = new(
            $$"""
            {
              "results": [
                {
                  "title": "Current release notes",
                  "url": "{{sourceUrl}}",
                  "content": "Version 10.0.8 is the current stable release according to this source."
                }
              ]
            }
            """);
        OllamaWebSearchExecutor executor = new(
            new HttpClient(handler),
            new OllamaWebSearchOptions(new Uri("https://ollama.com/api/web_search")),
            "test-key");
        InMemoryAuditSink audit = new();
        ActionGateway gateway = new(
            [WebActions.Search],
            [executor],
            new ReadOnlyActionPolicy(),
            audit);
        SearchThenAnswerBrain brain = new(sourceUrl);
        AgentLoop loop = new(brain, gateway, new AgentLoopOptions());
        LorenRunService runService = new(loop, audit);

        LorenRunResult result = await runService.RunAsync(
            "What is the latest stable version? Search the web and cite the source.",
            cancellationToken);

        Assert.Equal(2, result.Turns);
        Assert.Equal(1, result.ActionCount);
        Assert.Contains("10.0.8", result.FinalOutput, StringComparison.Ordinal);
        Assert.Contains(sourceUrl, result.FinalOutput, StringComparison.Ordinal);
        Assert.Equal(1, handler.RequestCount);
        Assert.Contains(
            result.Audit,
            entry => entry.ActionName == WebActions.Search.Name
                && entry.Kind == AuditEventKind.ActionCompleted.ToString()
                && entry.Outcome == "succeeded");
    }

    private sealed class SearchThenAnswerBrain(string expectedSourceUrl) : IBrain
    {
        private int _callCount;

        public Task<BrainTurnResult> ThinkAsync(
            BrainContext context,
            IReadOnlyList<ActionDefinition> availableActions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _callCount++;

            Assert.Contains(availableActions, action => action.Name == WebActions.Search.Name);

            if (_callCount == 1)
            {
                return Task.FromResult(
                    BrainTurnResult.Request(
                        new ActionRequest(
                            WebActions.Search.Name,
                            new Dictionary<string, string>
                            {
                                ["query"] = "latest stable version current release",
                            })));
            }

            BrainActionObservation observation = Assert.IsType<BrainActionObservation>(context.Inputs[^1]);
            Assert.True(observation.Result.Success);
            Assert.Contains(expectedSourceUrl, observation.Result.Data["sources_json"], StringComparison.Ordinal);
            Assert.Contains("untrusted external evidence", observation.Result.Data["evidence_note"], StringComparison.Ordinal);

            return Task.FromResult(
                BrainTurnResult.Final(
                    $"The current stable version is 10.0.8. Source: {expectedSourceUrl}"));
        }
    }

    private sealed class FixedJsonHandler(string json) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
        }
    }
}
