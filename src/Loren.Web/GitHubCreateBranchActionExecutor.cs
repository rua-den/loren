using Loren.Core.Actions;
using Loren.Core.Credentials;
using Loren.Runtime;
using Loren.Tools.GitHub;

namespace Loren.Web;

public sealed class GitHubCreateBranchActionExecutor : CredentialBoundActionExecutor
{
    public const string BranchTargetKey = "branch";
    public const string SourceShaTargetKey = "source_sha";

    private readonly GitHubCreateBranchClient _client;

    public GitHubCreateBranchActionExecutor(
        IActionCredentialResolver credentialResolver,
        GitHubCreateBranchClient client)
        : base(
            credentialResolver,
            GitHubCredentials.WritePurpose,
            GitHubCredentials.LocalV01WriteReference)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public override string ActionName => GitHubActions.CreateBranch.Name;

    protected override async Task<ActionResult> ExecuteWithCredentialAsync(
        ActionExecutionRequest execution,
        string credentialSecret,
        CancellationToken cancellationToken)
    {
        ActionAuthorizationContext authorization = execution.AuthorizationContext
            ?? throw new InvalidOperationException(
                "Trusted authorization context disappeared before execution.");
        ActionRequest request = execution.Request;

        if (!string.Equals(
                authorization.RepositoryLocator.Provider,
                "github",
                StringComparison.Ordinal))
        {
            return Failure(request.Name, "Canonical repository provider is not GitHub.");
        }

        if (!TryReadTarget(
                authorization.NormalizedTarget,
                BranchTargetKey,
                out string trustedBranch)
            || !TryReadTarget(
                authorization.NormalizedTarget,
                SourceShaTargetKey,
                out string trustedSourceSha))
        {
            return Failure(
                request.Name,
                "Trusted create-branch target is missing branch or source SHA.");
        }

        if (!TryReadTarget(request.Arguments, BranchTargetKey, out string proposedBranch)
            || !TryReadTarget(request.Arguments, SourceShaTargetKey, out string proposedSourceSha)
            || !string.Equals(trustedBranch, proposedBranch, StringComparison.Ordinal)
            || !string.Equals(trustedSourceSha, proposedSourceSha, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                request.Name,
                "Model-visible create-branch arguments do not match the trusted approved target.");
        }

        GitHubCreateBranchOperationResult operation = await _client.CreateAndVerifyAsync(
            authorization.RepositoryLocator,
            trustedBranch,
            trustedSourceSha,
            credentialSecret,
            cancellationToken);

        Dictionary<string, string> data = new(StringComparer.Ordinal)
        {
            ["repository"] = operation.RepositoryFullName,
            ["branch"] = operation.Branch,
            ["source_sha"] = operation.SourceSha,
            ["verification"] = operation.Success ? "verified" : "failed",
        };

        if (!string.IsNullOrWhiteSpace(operation.DefaultBranch))
        {
            data["default_branch"] = operation.DefaultBranch;
        }

        if (!string.IsNullOrWhiteSpace(operation.VerifiedSha))
        {
            data["verified_sha"] = operation.VerifiedSha;
        }

        return new ActionResult(
            request.Name,
            operation.Success,
            data,
            operation.Error);
    }

    private static bool TryReadTarget(
        IReadOnlyDictionary<string, string> values,
        string key,
        out string value)
    {
        if (values.TryGetValue(key, out string? candidate)
            && !string.IsNullOrWhiteSpace(candidate))
        {
            value = candidate.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static ActionResult Failure(string actionName, string error) =>
        new(actionName, false, new Dictionary<string, string>(), error);
}
