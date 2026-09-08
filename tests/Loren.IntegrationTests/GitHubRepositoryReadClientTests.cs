using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Loren.Core.Projects;
using Loren.Tools.GitHub;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class GitHubRepositoryReadClientTests
{
    private const string Sha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task ResolvesDefaultBranchAndExactCommitWithoutCredentials()
    {
        SequenceHandler handler = new(
            Metadata(),
            Json(HttpStatusCode.OK, $"{{\"ref\":\"refs/heads/main\",\"object\":{{\"type\":\"commit\",\"sha\":\"{Sha}\"}}}}"));
        GitHubRepositoryReadClient client = new(new HttpClient(handler));

        GitHubRepositoryResolutionResult result = await client.ResolveSourceAsync(
            new RepositoryLocator("github", "rua-den", "loren"),
            null,
            CancellationToken.None);

        Assert.True(result.Success, result.Error);
        Assert.Equal("rua-den/loren", result.Repository.FullName);
        Assert.Equal("main", result.DefaultBranch);
        Assert.Equal("refs/heads/main", result.SourceRef);
        Assert.Equal(Sha, result.SourceSha);
        Assert.Equal(["/repos/rua-den/loren", "/repos/rua-den/loren/git/ref/heads/main"], handler.Paths);
        Assert.All(handler.Requests, request => Assert.Null(request.Headers.Authorization));
    }

    [Fact]
    public async Task ReadsMetadataWithOneUnauthenticatedGet()
    {
        SequenceHandler handler = new(Metadata());

        GitHubRepositoryMetadataResult result = await new GitHubRepositoryReadClient(new HttpClient(handler))
            .ReadRepositoryAsync(new RepositoryLocator("github", "rua-den", "loren"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Null(handler.Requests[0].Headers.Authorization);
    }

    [Fact]
    public async Task ResolvesExplicitSlashBranchWithEscapedPathSegment()
    {
        SequenceHandler handler = new(
            Metadata(),
            Json(HttpStatusCode.OK, $"{{\"ref\":\"refs/heads/feat/release\",\"object\":{{\"type\":\"commit\",\"sha\":\"{Sha}\"}}}}"));
        GitHubRepositoryReadClient client = new(new HttpClient(handler));

        GitHubRepositoryResolutionResult result = await client.ResolveSourceAsync(
            new RepositoryLocator("github", "rua-den", "loren"),
            "refs/heads/feat/release",
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("refs/heads/feat/release", result.SourceRef);
        Assert.Equal("/repos/rua-den/loren/git/ref/heads/feat%2Frelease", handler.Paths[1]);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
    }

    [Fact]
    public async Task ResolvesShortHexBranchAsBranchOnly()
    {
        SequenceHandler handler = new(
            Metadata(),
            Json(HttpStatusCode.OK, $"{{\"ref\":\"refs/heads/deadbee\",\"object\":{{\"type\":\"commit\",\"sha\":\"{Sha}\"}}}}"));

        GitHubRepositoryResolutionResult result = await new GitHubRepositoryReadClient(new HttpClient(handler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), "deadbee", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("/repos/rua-den/loren/git/ref/heads/deadbee", handler.Paths[1]);
        Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
    }

    [Theory]
    [InlineData("refs/tags/v1")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("main..other")]
    [InlineData("bad branch")]
    public async Task RejectsUnsafeOrNonBranchSourceWithoutHttp(string source)
    {
        SequenceHandler handler = new();
        GitHubRepositoryReadClient client = new(new HttpClient(handler));

        GitHubRepositoryResolutionResult result = await client.ResolveSourceAsync(
            new RepositoryLocator("github", "rua-den", "loren"), source, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task RejectsIdentityRefAndObjectTypeMismatches()
    {
        SequenceHandler identityHandler = new(
            Json(HttpStatusCode.OK, "{\"full_name\":\"other/repo\",\"default_branch\":\"main\"}"));
        GitHubRepositoryResolutionResult identity = await new GitHubRepositoryReadClient(new HttpClient(identityHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(identity.Success);

        SequenceHandler refHandler = new(
            Metadata(),
            Json(HttpStatusCode.OK, $"{{\"ref\":\"refs/heads/other\",\"object\":{{\"type\":\"commit\",\"sha\":\"{Sha}\"}}}}"));
        GitHubRepositoryResolutionResult mismatch = await new GitHubRepositoryReadClient(new HttpClient(refHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), "main", CancellationToken.None);
        Assert.False(mismatch.Success);

        SequenceHandler typeHandler = new(
            Metadata(),
            Json(HttpStatusCode.OK, $"{{\"ref\":\"refs/heads/main\",\"object\":{{\"type\":\"tag\",\"sha\":\"{Sha}\"}}}}"));
        GitHubRepositoryResolutionResult typeMismatch = await new GitHubRepositoryReadClient(new HttpClient(typeHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), "main", CancellationToken.None);
        Assert.False(typeMismatch.Success);
    }

    [Fact]
    public async Task RejectsMissingObjectAndMalformedMetadataWithoutBranchRequest()
    {
        SequenceHandler missingObjectHandler = new(
            Metadata(), Json(HttpStatusCode.OK, "{\"ref\":\"refs/heads/main\"}"));
        GitHubRepositoryResolutionResult missingObject = await new GitHubRepositoryReadClient(new HttpClient(missingObjectHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(missingObject.Success);
        Assert.Equal(2, missingObjectHandler.Requests.Count);

        SequenceHandler malformedHandler = new(Json(HttpStatusCode.OK, "{not-json"));
        GitHubRepositoryResolutionResult malformed = await new GitHubRepositoryReadClient(new HttpClient(malformedHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(malformed.Success);
        Assert.Single(malformedHandler.Requests);
        Assert.Contains("could not be parsed", malformed.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectsNonGitHubArchivedAndHttpFailuresWithoutUnsafeDetails()
    {
        SequenceHandler nonGitHubHandler = new();
        GitHubRepositoryResolutionResult nonGitHub = await new GitHubRepositoryReadClient(new HttpClient(nonGitHubHandler))
            .ResolveSourceAsync(new RepositoryLocator("gitlab", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(nonGitHub.Success);
        Assert.Empty(nonGitHubHandler.Requests);

        SequenceHandler archivedHandler = new(Metadata(archived: true));
        GitHubRepositoryResolutionResult archived = await new GitHubRepositoryReadClient(new HttpClient(archivedHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(archived.Success);
        Assert.Single(archivedHandler.Requests);

        SequenceHandler metadataFailureHandler = new(Json(HttpStatusCode.NotFound, "secret metadata body"));
        GitHubRepositoryResolutionResult metadataFailure = await new GitHubRepositoryReadClient(new HttpClient(metadataFailureHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(metadataFailure.Success);
        Assert.DoesNotContain("secret", metadataFailure.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Single(metadataFailureHandler.Requests);

        SequenceHandler branchFailureHandler = new(Metadata(), Json(HttpStatusCode.NotFound, "secret branch body"));
        GitHubRepositoryResolutionResult branchFailure = await new GitHubRepositoryReadClient(new HttpClient(branchFailureHandler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(branchFailure.Success);
        Assert.DoesNotContain("secret", branchFailure.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, branchFailureHandler.Requests.Count);
    }

    [Fact]
    public async Task RejectsMalformedOrInvalidShaResponseAndSuppressesBody()
    {
        SequenceHandler handler = new(
            Metadata(),
            Json(HttpStatusCode.OK, "{\"ref\":\"refs/heads/main\",\"object\":{\"type\":\"commit\",\"sha\":\"short\"}}"));
        GitHubRepositoryResolutionResult result = await new GitHubRepositoryReadClient(new HttpClient(handler))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.DoesNotContain("short", result.Error ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreservesCancellation()
    {
        CancellationTokenSource cancellation = new();
        SequenceHandler handler = new(onRequest: cancellation.Cancel);
        GitHubRepositoryReadClient client = new(new HttpClient(handler));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ResolveSourceAsync(
            new RepositoryLocator("github", "rua-den", "loren"), null, cancellation.Token));
    }

    [Fact]
    public async Task ConvertsNonCallerTransportCancellationAndHttpErrorsToSafeFailures()
    {
        const string secret = "transport-secret";
        SequenceHandler timeoutHandler = new(onRequest: () => throw new OperationCanceledException(secret));
        GitHubRepositoryMetadataResult timeout = await new GitHubRepositoryReadClient(new HttpClient(timeoutHandler))
            .ReadRepositoryAsync(new RepositoryLocator("github", "rua-den", "loren"), CancellationToken.None);
        Assert.False(timeout.Success);
        Assert.Contains("timed out", timeout.Error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(secret, timeout.Error, StringComparison.Ordinal);

        SequenceHandler errorHandler = new(onRequest: () => throw new HttpRequestException(secret));
        GitHubRepositoryMetadataResult error = await new GitHubRepositoryReadClient(new HttpClient(errorHandler))
            .ReadRepositoryAsync(new RepositoryLocator("github", "rua-den", "loren"), CancellationToken.None);
        Assert.False(error.Success);
        Assert.DoesNotContain(secret, error.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("io")]
    [InlineData("timeout")]
    public async Task ConvertsMetadataResponseBodyFaultsToSafeFailures(string fault)
    {
        GitHubRepositoryResolutionResult result = await new GitHubRepositoryReadClient(
                new HttpClient(new SequenceHandler(ThrowingResponse(fault))))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.DoesNotContain("body-secret", result.Error, StringComparison.Ordinal);
        Assert.Contains(fault == "timeout" ? "timed out" : "could not", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("io")]
    [InlineData("timeout")]
    public async Task ConvertsBranchResponseBodyFaultsToSafeFailures(string fault)
    {
        GitHubRepositoryResolutionResult result = await new GitHubRepositoryReadClient(
                new HttpClient(new SequenceHandler(Metadata(), ThrowingResponse(fault))))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.DoesNotContain("body-secret", result.Error, StringComparison.Ordinal);
        Assert.Contains(fault == "timeout" ? "timed out" : "could not", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreservesCallerCancellationFromMetadataResponseBody()
    {
        using CancellationTokenSource cancellation = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new GitHubRepositoryReadClient(
                new HttpClient(new SequenceHandler(ThrowingResponse("caller-cancel", cancellation))))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, cancellation.Token));
    }

    [Fact]
    public async Task PreservesCallerCancellationFromBranchResponseBody()
    {
        using CancellationTokenSource cancellation = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new GitHubRepositoryReadClient(
                new HttpClient(new SequenceHandler(Metadata(), ThrowingResponse("caller-cancel", cancellation))))
            .ResolveSourceAsync(new RepositoryLocator("github", "rua-den", "loren"), null, cancellation.Token));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage Metadata(bool archived = false) => Json(HttpStatusCode.OK,
        $"{{\"full_name\":\"rua-den/loren\",\"default_branch\":\"main\",\"private\":false,\"archived\":{archived.ToString().ToLowerInvariant()},\"open_issues_count\":2,\"pushed_at\":\"2026-09-03T09:47:46Z\",\"html_url\":\"https://github.com/rua-den/loren\"}}");

    private static HttpResponseMessage ThrowingResponse(
        string fault,
        CancellationTokenSource? cancellation = null) => new(HttpStatusCode.OK)
        {
            Content = new ThrowingContent(fault, cancellation),
        };

    private sealed record Request(HttpMethod Method, Uri? RequestUri, HttpRequestHeaders Headers);

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        private readonly Action? _onRequest;

        public SequenceHandler(params HttpResponseMessage[] responses) : this(null, responses) { }

        public SequenceHandler(Action onRequest) : this(onRequest, []) { }

        private SequenceHandler(Action? onRequest, params HttpResponseMessage[] responses)
        {
            _onRequest = onRequest;
            _responses = new(responses);
        }

        public List<Request> Requests { get; } = [];

        public List<string> Paths => Requests.Select(request => request.RequestUri?.AbsolutePath ?? string.Empty).ToList();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _onRequest?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(new(request.Method, request.RequestUri, request.Headers));
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("Unexpected HTTP request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class ThrowingContent(string fault, CancellationTokenSource? cancellation) : HttpContent
    {
        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new ThrowingStream(fault, cancellation));

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.CompletedTask;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }
    }

    private sealed class ThrowingStream(string fault, CancellationTokenSource? cancellation) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => Throw<int>();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            new(Throw<int>(cancellationToken));

        private T Throw<T>(CancellationToken cancellationToken = default)
        {
            if (fault == "caller-cancel")
            {
                cancellation!.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledException(cancellation.Token);
            }

            if (fault == "http")
            {
                throw new HttpRequestException("body-secret");
            }

            if (fault == "io")
            {
                throw new IOException("body-secret");
            }

            throw new OperationCanceledException("body-secret");
        }
    }
}
