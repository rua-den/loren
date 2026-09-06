using Loren.Core.Actions;
using Loren.Core.Credentials;
using Loren.Runtime;
using Xunit;

namespace Loren.Runtime.Tests;

public sealed class CredentialBoundActionExecutorTests
{
    private static readonly CredentialPurpose Purpose = new("github.write");
    private static readonly CredentialReference Reference = new("github.write.local-v0.1");

    [Fact]
    public async Task MissingCredentialFailsBeforeConsequentialExecutorAttempt()
    {
        CredentialResolutionRequest request = new(Purpose, Reference);
        StubCredentialResolver resolver = new(_ => CredentialResolution.Missing(request));
        RecordingExecutor executor = new(resolver, (_, _, _) =>
            Task.FromResult(Success("test.write")));

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest("test.write", new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, executor.CallCount);
        Assert.Equal("missing", result.Data["credential_status"]);
        Assert.Equal("github.write", result.Data["credential_purpose"]);
        Assert.Equal("github.write.local-v0.1", result.Data["credential_reference"]);
    }

    [Fact]
    public async Task RevokedCredentialOverridesOtherwiseExecutableIntent()
    {
        CredentialResolutionRequest request = new(Purpose, Reference);
        StubCredentialResolver resolver = new(_ => CredentialResolution.Revoked(request));
        RecordingExecutor executor = new(resolver, (_, _, _) =>
            Task.FromResult(Success("test.write")));

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest("test.write", new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, executor.CallCount);
        Assert.Equal("revoked", result.Data["credential_status"]);
        Assert.Contains("revoked", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolvedCredentialIsVisibleOnlyInsideExecutorAndRedactedFromResult()
    {
        const string secret = "slice2-runtime-secret";
        CredentialLease lease = new(Purpose, Reference, secret);
        StubCredentialResolver resolver = new(_ => CredentialResolution.Resolved(lease));
        RecordingExecutor executor = new(
            resolver,
            (actionRequest, observedSecret, _) => Task.FromResult(
                new ActionResult(
                    actionRequest.Name,
                    true,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["credential_echo"] = observedSecret,
                        [$"key-{observedSecret}"] = $"value-{observedSecret}",
                    },
                    $"diagnostic-{observedSecret}")));

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest("test.write", new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, executor.CallCount);
        Assert.Equal(secret, executor.ObservedSecret);
        Assert.DoesNotContain(secret, result.Error, StringComparison.Ordinal);
        Assert.All(result.Data, pair =>
        {
            Assert.DoesNotContain(secret, pair.Key, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, pair.Value, StringComparison.Ordinal);
        });
        Assert.Contains(CredentialLease.RedactedValue, result.Error, StringComparison.Ordinal);
        Assert.Contains(CredentialLease.RedactedValue, result.Data["credential_echo"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecutorExceptionContainingSecretIsSanitizedBeforeReturning()
    {
        const string secret = "slice2-runtime-secret";
        CredentialLease lease = new(Purpose, Reference, secret);
        StubCredentialResolver resolver = new(_ => CredentialResolution.Resolved(lease));
        RecordingExecutor executor = new(
            resolver,
            (_, observedSecret, _) => throw new InvalidOperationException(
                $"remote failure included {observedSecret}"));

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest("test.write", new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(1, executor.CallCount);
        Assert.DoesNotContain(secret, result.Error, StringComparison.Ordinal);
        Assert.Contains(CredentialLease.RedactedValue, result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolverExceptionMessageIsNotPropagated()
    {
        const string secret = "resolver-secret-that-must-not-leak";
        ThrowingCredentialResolver resolver = new(secret);
        RecordingExecutor executor = new(resolver, (_, _, _) =>
            Task.FromResult(Success("test.write")));

        ActionResult result = await executor.ExecuteAsync(
            new ActionRequest("test.write", new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, executor.CallCount);
        Assert.DoesNotContain(secret, result.Error, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", result.Error, StringComparison.Ordinal);
    }

    private static ActionResult Success(string actionName) =>
        new(actionName, true, new Dictionary<string, string>());

    private sealed class StubCredentialResolver(
        Func<CredentialResolutionRequest, CredentialResolution> resolve)
        : IActionCredentialResolver
    {
        public Task<CredentialResolution> ResolveAsync(
            CredentialResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(resolve(request));
        }
    }

    private sealed class ThrowingCredentialResolver(string message) : IActionCredentialResolver
    {
        public Task<CredentialResolution> ResolveAsync(
            CredentialResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException(message);
        }
    }

    private sealed class RecordingExecutor : CredentialBoundActionExecutor
    {
        private readonly Func<ActionRequest, string, CancellationToken, Task<ActionResult>> _execute;

        public RecordingExecutor(
            IActionCredentialResolver resolver,
            Func<ActionRequest, string, CancellationToken, Task<ActionResult>> execute)
            : base(resolver, Purpose, Reference)
        {
            _execute = execute;
        }

        public override string ActionName => "test.write";

        public int CallCount { get; private set; }

        public string? ObservedSecret { get; private set; }

        protected override Task<ActionResult> ExecuteWithCredentialAsync(
            ActionRequest request,
            string credentialSecret,
            CancellationToken cancellationToken)
        {
            CallCount++;
            ObservedSecret = credentialSecret;
            return _execute(request, credentialSecret, cancellationToken);
        }
    }
}
