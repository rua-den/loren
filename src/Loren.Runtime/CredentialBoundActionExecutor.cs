using Loren.Core.Actions;
using Loren.Core.Credentials;

namespace Loren.Runtime;

public abstract class CredentialBoundActionExecutor : IActionExecutor
{
    private readonly IActionCredentialResolver _credentialResolver;
    private readonly CredentialPurpose _credentialPurpose;
    private readonly CredentialReference _credentialReference;

    protected CredentialBoundActionExecutor(
        IActionCredentialResolver credentialResolver,
        CredentialPurpose credentialPurpose,
        CredentialReference credentialReference)
    {
        _credentialResolver = credentialResolver
            ?? throw new ArgumentNullException(nameof(credentialResolver));
        _credentialPurpose = credentialPurpose;
        _credentialReference = credentialReference;
    }

    public abstract string ActionName { get; }

    public async Task<ActionResult> ExecuteAsync(
        ActionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        CredentialResolutionRequest resolutionRequest = new(
            _credentialPurpose,
            _credentialReference);

        CredentialResolution resolution;
        try
        {
            resolution = await _credentialResolver.ResolveAsync(
                resolutionRequest,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure(
                request.Name,
                "error",
                $"Credential resolution failed with {exception.GetType().Name}.");
        }

        if (!resolution.IsResolved || resolution.Lease is null)
        {
            return Failure(
                request.Name,
                resolution.Status.ToString().ToLowerInvariant(),
                ResolutionFailureReason(resolution.Status));
        }

        CredentialLease lease = resolution.Lease;
        return await lease.UseAsync(
            async (secret, useCancellationToken) =>
            {
                try
                {
                    ActionResult result = await ExecuteWithCredentialAsync(
                        request,
                        secret,
                        useCancellationToken);
                    return Redact(result, lease);
                }
                catch (OperationCanceledException) when (useCancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    string sanitizedMessage = lease.Redact(exception.Message);
                    string detail = string.IsNullOrWhiteSpace(sanitizedMessage)
                        ? $"Credential-bound executor failed with {exception.GetType().Name}."
                        : $"Credential-bound executor failed with {exception.GetType().Name}: {sanitizedMessage}";

                    return Failure(
                        request.Name,
                        "executor_error",
                        detail);
                }
            },
            cancellationToken);
    }

    protected abstract Task<ActionResult> ExecuteWithCredentialAsync(
        ActionRequest request,
        string credentialSecret,
        CancellationToken cancellationToken);

    private ActionResult Failure(
        string actionName,
        string credentialStatus,
        string error) =>
        new(
            actionName,
            false,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["credential_status"] = credentialStatus,
                ["credential_purpose"] = _credentialPurpose.ToString(),
                ["credential_reference"] = _credentialReference.ToString(),
            },
            error);

    private static string ResolutionFailureReason(CredentialResolutionStatus status) =>
        status switch
        {
            CredentialResolutionStatus.Missing =>
                "Required write credential is missing.",
            CredentialResolutionStatus.Revoked =>
                "Required write credential is revoked or has an invalid revocation state.",
            CredentialResolutionStatus.NotConfigured =>
                "Required write credential purpose/reference is not configured.",
            _ => "Required write credential could not be resolved.",
        };

    private static ActionResult Redact(
        ActionResult result,
        CredentialLease lease)
    {
        ArgumentNullException.ThrowIfNull(result);

        Dictionary<string, string> data = new(StringComparer.Ordinal);
        foreach ((string key, string value) in result.Data)
        {
            data[lease.Redact(key)] = lease.Redact(value);
        }

        return new ActionResult(
            lease.Redact(result.ActionName),
            result.Success,
            data,
            lease.Redact(result.Error));
    }
}
