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

public sealed class SourceAwareResearchAgentLoopTests
{
    private const string SourceA = "https://example.com/a";
    private const string SourceB = "https://example.org/b";

    [Fact]
    public async Task ResearchCanSearchFetchTwoSourcesAndSeparateFactsFromInference()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SearchHandler searchHandler = new();
        FetchHandler fetchHandler = new();
        IActionExecutor searchExecutor = new OllamaWebSearchExecutor(
            new HttpClient(searchHandler),
            new OllamaWebSearchOptions(new Uri("https://ollama.com/api/web_search")),
            "test-key");
        IActionExecutor fetchExecutor = new OllamaWebFetchExecutor(
            new HttpClient(fetchHandler),
            new OllamaWebFetchOptions(new Uri("https://ollama.com/api/web_fetch")),
            "test-key");
        InMemoryAuditSink audit = new();
        ActionGateway gateway = new(
            [WebActions.Search, WebActions.Fetch],
            [searchExecutor, fetchExecutor],
            new ReadOnlyActionPolicy(),
            audit);
        ResearchBrain brain = new();
        AgentLoop loop = new(brain, gateway, new AgentLoopOptions(MaxTurns: 6, MaxActions: 4));
        LorenRunService service = new(loop, audit);

        LorenRunResult result = await service.RunAsync(
            "Research option A versus option B. Use multiple sources, cite them, and separate facts from your inference.",
            cancellationToken);

        Assert.Equal(4, result.Turns);
        Assert.Equal(3, result.ActionCount);
        Assert.Equal(1, searchHandler.RequestCount);
        Assert.Equal(2, fetchHandler.RequestCount);
        Assert.Contains(SourceA, result.FinalOutput, StringComparison.Ordinal);
        Assert.Contains(SourceB, result.FinalOutput, StringComparison.Ordinal);
        Assert.Contains("Sourced facts", result.FinalOutput, StringComparison.Ordinal);
        Assert.Contains("Inference", result.FinalOutput, StringComparison.Ordinal);
        Assert.Equal(
            3,
            result.Audit.Count(entry =>
                entry.Kind == AuditEventKind.ActionCompleted.ToString()
                && entry.Outcome == "succeeded"));
    }

    private sealed class ResearchBrain : IBrain
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
            Assert.Contains(availableActions, action => action.Name == WebActions.Fetch.Name);

            return _callCount switch
            {
                1 => Task.FromResult(
                    BrainTurnResult.Request(
                        new ActionRequest(
                            WebActions.Search.Name,
                            new Dictionary<string, string>
                            {
                                ["query"] = "option A option B comparison",
                            }))),
                2 => FetchAfterSearch(context, SourceA),
                3 => FetchAfterPage(context, SourceA, SourceB),
                4 => FinalAfterSecondPage(context),
                _ => throw new InvalidOperationException("Unexpected extra research turn."),
            };
        }

        private static Task<BrainTurnResult> FetchAfterSearch(
            BrainContext context,
            string sourceUrl)
        {
            BrainActionObservation search = Assert.IsType<BrainActionObservation>(context.Inputs[^1]);
            Assert.Equal(WebActions.Search.Name, search.Request.Name);
            Assert.True(search.Result.Success);
            Assert.Contains(SourceA, search.Result.Data["sources_json"], StringComparison.Ordinal);
            Assert.Contains(SourceB, search.Result.Data["sources_json"], StringComparison.Ordinal);
            return Task.FromResult(
                BrainTurnResult.Request(
                    new ActionRequest(
                        WebActions.Fetch.Name,
                        new Dictionary<string, string> { ["url"] = sourceUrl })));
        }

        private static Task<BrainTurnResult> FetchAfterPage(
            BrainContext context,
            string expectedFetchedUrl,
            string nextSourceUrl)
        {
            BrainActionObservation page = Assert.IsType<BrainActionObservation>(context.Inputs[^1]);
            Assert.Equal(WebActions.Fetch.Name, page.Request.Name);
            Assert.True(page.Result.Success);
            Assert.Equal(expectedFetchedUrl, page.Result.Data["url"].TrimEnd('/'));
            Assert.Contains("untrusted external evidence", page.Result.Data["evidence_note"], StringComparison.Ordinal);
            return Task.FromResult(
                BrainTurnResult.Request(
                    new ActionRequest(
                        WebActions.Fetch.Name,
                        new Dictionary<string, string> { ["url"] = nextSourceUrl })));
        }

        private static Task<BrainTurnResult> FinalAfterSecondPage(BrainContext context)
        {
            BrainActionObservation page = Assert.IsType<BrainActionObservation>(context.Inputs[^1]);
            Assert.Equal(SourceB, page.Result.Data["url"].TrimEnd('/'));
            Assert.Contains("Option B is easier to operate", page.Result.Data["content"], StringComparison.Ordinal);

            return Task.FromResult(
                BrainTurnResult.Final(
                    $"Sourced facts: Option A reports stronger throughput ({SourceA}); Option B reports easier operations ({SourceB}).\nInference: for a small owner-operated deployment, I would start with B, but that recommendation is my synthesis rather than a quoted source fact."));
        }
    }

    private sealed class SearchHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            return Task.FromResult(Json(
                $$"""
                {
                  "results": [
                    { "title": "Option A benchmark", "url": "{{SourceA}}", "content": "Option A reports higher throughput." },
                    { "title": "Option B operations", "url": "{{SourceB}}", "content": "Option B reports simpler operations." }
                  ]
                }
                """));
        }
    }

    private sealed class FetchHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (body.Contains(SourceA, StringComparison.Ordinal))
            {
                return Json(
                    $$"""
                    {
                      "title": "Option A benchmark",
                      "content": "Option A reports stronger throughput in its published benchmark.",
                      "links": ["{{SourceA}}"]
                    }
                    """);
            }

            if (body.Contains(SourceB, StringComparison.Ordinal))
            {
                return Json(
                    $$"""
                    {
                      "title": "Option B operations",
                      "content": "Option B is easier to operate according to its operations guide.",
                      "links": ["{{SourceB}}"]
                    }
                    """);
            }

            throw new InvalidOperationException("Unexpected fetch URL.");
        }
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
}
