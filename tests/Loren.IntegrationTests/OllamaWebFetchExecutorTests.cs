using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Loren.Core.Actions;
using Loren.Tools.Web;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class OllamaWebFetchExecutorTests
{
    private const string Secret = "ollama-fetch-secret-must-not-leak";
    private static readonly DateTimeOffset RetrievedAt =
        new(2026, 9, 6, 16, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task FetchUsesBearerCredentialAndReturnsBoundedPageEvidence()
    {
        string longContent = new('x', 800);
        RecordingHandler handler = new(_ => Json(
            HttpStatusCode.OK,
            $$"""
            {
              "title": "Source page",
              "content": "{{longContent}}",
              "links": [
                "https://example.com/next",
                "https://example.com/next",
                "http://127.0.0.1/private",
                "javascript:alert(1)"
              ]
            }
            """));
        OllamaWebFetchExecutor executor = new(
            new HttpClient(handler),
            new OllamaWebFetchOptions(
                new Uri("https://ollama.com/api/web_fetch"),
                MaxContentCharacters: 500,
                MaxLinks: 4),
            Secret,
            new FixedTimeProvider(RetrievedAt));

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest(
                WebActions.Fetch.Name,
                new Dictionary<string, string>
                {
                    ["url"] = "https://example.com/research",
                }),
            TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("https://ollama.com/api/web_fetch", handler.LastUri?.AbsoluteUri);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", Secret), handler.LastAuthorization);
        Assert.Contains("https://example.com/research", handler.LastBody, StringComparison.Ordinal);
        Assert.Equal("Source page", result.Data["title"]);
        Assert.Equal("https://example.com/research", result.Data["url"].TrimEnd('/'));
        Assert.Equal("ollama_web_fetch", result.Data["provider"]);
        Assert.Equal(RetrievedAt.ToString("O"), result.Data["retrieved_at_utc"]);
        Assert.Equal(500, result.Data["content"].Length);
        Assert.EndsWith("…", result.Data["content"], StringComparison.Ordinal);
        string[] links = JsonSerializer.Deserialize<string[]>(result.Data["links_json"])!;
        Assert.Single(links);
        Assert.Equal("https://example.com/next", links[0].TrimEnd('/'));
        Assert.Contains("untrusted external evidence", result.Data["evidence_note"], StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, result.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://localhost/admin")]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://10.1.2.3/admin")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]/admin")]
    [InlineData("https://user:pass@example.com/private")]
    [InlineData("https://example.com:8443/private")]
    [InlineData("file:///etc/passwd")]
    public async Task NonPublicOrPrivilegedLookingUrlsFailBeforeExternalCall(string url)
    {
        RecordingHandler handler = new(_ => throw new InvalidOperationException("HTTP must not run."));
        OllamaWebFetchExecutor executor = new(
            new HttpClient(handler),
            new OllamaWebFetchOptions(new Uri("https://ollama.com/api/web_fetch")),
            Secret);

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest(
                WebActions.Fetch.Name,
                new Dictionary<string, string> { ["url"] = url }),
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(0, handler.RequestCount);
        Assert.Contains("public http/https URL", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingCredentialFailsBeforeExternalCall()
    {
        RecordingHandler handler = new(_ => throw new InvalidOperationException("HTTP must not run."));
        OllamaWebFetchExecutor executor = new(
            new HttpClient(handler),
            new OllamaWebFetchOptions(new Uri("https://ollama.com/api/web_fetch")),
            null);

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest(
                WebActions.Fetch.Name,
                new Dictionary<string, string> { ["url"] = "https://example.com" }),
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(0, handler.RequestCount);
        Assert.Contains("OLLAMA_API_KEY", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FailureBodyAndSecretAreNeverSurfaced()
    {
        string hostileBody = $"provider echoed Authorization: Bearer {Secret}";
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(hostileBody, Encoding.UTF8, "text/plain"),
        });
        OllamaWebFetchExecutor executor = new(
            new HttpClient(handler),
            new OllamaWebFetchOptions(new Uri("https://ollama.com/api/web_fetch")),
            Secret);

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest(
                WebActions.Fetch.Name,
                new Dictionary<string, string> { ["url"] = "https://example.com" }),
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.DoesNotContain(hostileBody, result.Error, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, result.Error, StringComparison.Ordinal);
        Assert.Contains("403", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OversizedResponseFailsClosed()
    {
        byte[] oversized = new byte[(4 * 1024 * 1024) + 1];
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(oversized),
        });
        OllamaWebFetchExecutor executor = new(
            new HttpClient(handler),
            new OllamaWebFetchOptions(new Uri("https://ollama.com/api/web_fetch")),
            Secret);

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest(
                WebActions.Fetch.Name,
                new Dictionary<string, string> { ["url"] = "https://example.com" }),
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("safety bound", result.Error, StringComparison.Ordinal);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public HttpMethod? LastMethod { get; private set; }

        public Uri? LastUri { get; private set; }

        public AuthenticationHeaderValue? LastAuthorization { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            LastMethod = request.Method;
            LastUri = request.RequestUri;
            LastAuthorization = request.Headers.Authorization;
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}
