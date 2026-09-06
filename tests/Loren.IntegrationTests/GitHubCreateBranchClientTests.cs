using System.Net;
using Loren.Core.Projects;
using Loren.Tools.GitHub;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class GitHubCreateBranchClientTests
{
    private const string Secret = "github-write-secret-that-must-not-leak";
    private const string SourceSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task CreateBranchUsesPreflightMutationAndIndependentVerification()
    {
        SequenceHandler handler = new(
            Json(HttpStatusCode.OK, "{\"default_branch\":\"main\"}"),
            Json(HttpStatusCode.Created, "{\"ref\":\"refs/heads/feat/slice3\"}"),
            Json(HttpStatusCode.OK, $"{{\"object\":{{\"sha\":\"{SourceSha}\"}}}}"));
        GitHubCreateBranchClient client = new(new HttpClient(handler));

        GitHubCreateBranchOperationResult result = await client.CreateAndVerifyAsync(
            new RepositoryLocator("github", "rua-den", "loren"),
            "feat/slice3",
            SourceSha,
            Secret,
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(SourceSha, result.VerifiedSha);
        Assert.Equal("main", result.DefaultBranch);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("/repos/rua-den/loren", handler.Requests[0].Path);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("/repos/rua-den/loren/git/refs", handler.Requests[1].Path);
        Assert.Contains("refs/heads/feat/slice3", handler.Requests[1].Body, StringComparison.Ordinal);
        Assert.Contains(SourceSha, handler.Requests[1].Body, StringComparison.Ordinal);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
        Assert.Contains("/git/ref/heads/", handler.Requests[2].Path, StringComparison.Ordinal);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal(Secret, request.AuthorizationParameter);
        });
        Assert.DoesNotContain(Secret, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DefaultBranchIsRejectedBeforeMutation()
    {
        SequenceHandler handler = new(
            Json(HttpStatusCode.OK, "{\"default_branch\":\"main\"}"));
        GitHubCreateBranchClient client = new(new HttpClient(handler));

        GitHubCreateBranchOperationResult result = await client.CreateAndVerifyAsync(
            new RepositoryLocator("github", "rua-den", "loren"),
            "main",
            SourceSha,
            Secret,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Contains("default branch", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerificationMismatchIsNeverReportedAsSuccess()
    {
        const string unexpectedSha = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        SequenceHandler handler = new(
            Json(HttpStatusCode.OK, "{\"default_branch\":\"main\"}"),
            Json(HttpStatusCode.Created, "{}"),
            Json(HttpStatusCode.OK, $"{{\"object\":{{\"sha\":\"{unexpectedSha}\"}}}}"));
        GitHubCreateBranchClient client = new(new HttpClient(handler));

        GitHubCreateBranchOperationResult result = await client.CreateAndVerifyAsync(
            new RepositoryLocator("github", "rua-den", "loren"),
            "feat/mismatch",
            SourceSha,
            Secret,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(unexpectedSha, result.VerifiedSha);
        Assert.Contains("different", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("refs/heads/unsafe")]
    [InlineData("main..other")]
    [InlineData("bad branch")]
    [InlineData(".hidden")]
    public async Task UnsafeBranchNameNeverMakesExternalCall(string branch)
    {
        SequenceHandler handler = new();
        GitHubCreateBranchClient client = new(new HttpClient(handler));

        GitHubCreateBranchOperationResult result = await client.CreateAndVerifyAsync(
            new RepositoryLocator("github", "rua-den", "loren"),
            branch,
            SourceSha,
            Secret,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };

    private sealed record CapturedRequest(
        HttpMethod Method,
        string Path,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        string Body);

    private sealed class SequenceHandler(params HttpResponseMessage[] responses)
        : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri?.AbsolutePath ?? string.Empty,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                body));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("Unexpected HTTP request.");
            }

            return _responses.Dequeue();
        }
    }
}
