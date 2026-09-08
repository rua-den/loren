using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Loren.Core.Projects;

namespace Loren.Tools.GitHub;

public sealed class GitHubRepositoryReadClient
{
    private readonly HttpClient _httpClient;

    public GitHubRepositoryReadClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<GitHubRepositoryMetadataResult> ReadRepositoryAsync(
        RepositoryLocator repository,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsGitHub(repository))
        {
            return GitHubRepositoryMetadataResult.Failure(repository, "Canonical repository provider is not GitHub.");
        }

        HttpResponseMessage response;
        try
        {
            response = await SendGetAsync(
                $"repos/{Uri.EscapeDataString(repository.ExternalNamespace)}/{Uri.EscapeDataString(repository.ExternalName)}",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return GitHubRepositoryMetadataResult.Failure(repository, "GitHub repository metadata could not be read.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return GitHubRepositoryMetadataResult.Failure(repository, "GitHub repository metadata read timed out.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return GitHubRepositoryMetadataResult.Failure(
                    repository,
                    $"GitHub returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            try
            {
                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                JsonElement root = document.RootElement;
                string fullName = RequiredString(root, "full_name");
                string defaultBranch = RequiredString(root, "default_branch");
                if (!string.Equals(fullName, repository.FullName, StringComparison.OrdinalIgnoreCase)
                    || !IsValidBranchName(defaultBranch))
                {
                    return GitHubRepositoryMetadataResult.Failure(repository, "GitHub repository identity or default branch did not match the canonical target.");
                }

                Dictionary<string, string> data = new(StringComparer.Ordinal)
                {
                    ["full_name"] = fullName,
                    ["default_branch"] = defaultBranch,
                    ["private"] = RequiredBoolean(root, "private").ToString().ToLowerInvariant(),
                    ["archived"] = RequiredBoolean(root, "archived").ToString().ToLowerInvariant(),
                    ["open_issues_count"] = RequiredInt32(root, "open_issues_count").ToString(CultureInfo.InvariantCulture),
                    ["pushed_at"] = RequiredString(root, "pushed_at"),
                    ["html_url"] = RequiredString(root, "html_url"),
                };
                return new GitHubRepositoryMetadataResult(true, repository, defaultBranch, data, null);
            }
            catch (JsonException)
            {
                return GitHubRepositoryMetadataResult.Failure(repository, "GitHub response could not be parsed.");
            }
            catch (InvalidOperationException)
            {
                return GitHubRepositoryMetadataResult.Failure(repository, "GitHub response was missing required repository metadata.");
            }
            catch (HttpRequestException)
            {
                return GitHubRepositoryMetadataResult.Failure(repository, "GitHub repository metadata could not be read.");
            }
            catch (IOException)
            {
                return GitHubRepositoryMetadataResult.Failure(repository, "GitHub repository metadata could not be read.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return GitHubRepositoryMetadataResult.Failure(repository, "GitHub repository metadata read timed out.");
            }
        }
    }

    public async Task<GitHubRepositoryResolutionResult> ResolveSourceAsync(
        RepositoryLocator repository,
        string? sourceBranchOrRef,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsGitHub(repository))
        {
            return GitHubRepositoryResolutionResult.Failure(repository, "Canonical repository provider is not GitHub.");
        }

        string? requestedBranch = NormalizeBranch(sourceBranchOrRef);
        if (requestedBranch is null)
        {
            return GitHubRepositoryResolutionResult.Failure(repository, "Source ref must be a safe branch name.");
        }

        GitHubRepositoryMetadataResult metadata = await ReadRepositoryAsync(repository, cancellationToken);
        if (!metadata.Success)
        {
            return GitHubRepositoryResolutionResult.Failure(repository, metadata.Error ?? "Repository metadata read failed.");
        }
        if (metadata.Data.TryGetValue("archived", out string? archived)
            && string.Equals(archived, "true", StringComparison.Ordinal))
        {
            return GitHubRepositoryResolutionResult.Failure(repository, "Archived GitHub repositories cannot be used as live sources.", metadata.DefaultBranch);
        }

        string branch = string.IsNullOrEmpty(requestedBranch) ? metadata.DefaultBranch! : requestedBranch;
        string canonicalRef = $"refs/heads/{branch}";
        string path = $"repos/{Uri.EscapeDataString(repository.ExternalNamespace)}/{Uri.EscapeDataString(repository.ExternalName)}/git/ref/heads/{Uri.EscapeDataString(branch)}";
        HttpResponseMessage response;
        try
        {
            response = await SendGetAsync(path, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref could not be read.", metadata.DefaultBranch);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref read timed out.", metadata.DefaultBranch);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref could not be read.", metadata.DefaultBranch);
            }

            try
            {
                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                JsonElement root = document.RootElement;
                string returnedRef = RequiredString(root, "ref");
                if (!root.TryGetProperty("object", out JsonElement gitObject)
                    || gitObject.ValueKind is not JsonValueKind.Object)
                {
                    return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref response was malformed.", metadata.DefaultBranch);
                }
                string type = RequiredString(gitObject, "type");
                string sha = RequiredString(gitObject, "sha");
                if (!string.Equals(returnedRef, canonicalRef, StringComparison.Ordinal)
                    || !string.Equals(type, "commit", StringComparison.Ordinal)
                    || !IsFullCommitSha(sha))
                {
                    return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref response did not match the requested commit branch.", metadata.DefaultBranch);
                }

                return new GitHubRepositoryResolutionResult(true, repository, metadata.DefaultBranch!, canonicalRef, sha.ToLowerInvariant(), null);
            }
            catch (JsonException)
            {
                return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref response could not be parsed.", metadata.DefaultBranch);
            }
            catch (InvalidOperationException)
            {
                return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref response was malformed.", metadata.DefaultBranch);
            }
            catch (HttpRequestException)
            {
                return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref could not be read.", metadata.DefaultBranch);
            }
            catch (IOException)
            {
                return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref could not be read.", metadata.DefaultBranch);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return GitHubRepositoryResolutionResult.Failure(repository, "GitHub branch ref read timed out.", metadata.DefaultBranch);
            }
        }
    }

    private async Task<HttpResponseMessage> SendGetAsync(string path, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"https://api.github.com/{path}"));
        request.Headers.UserAgent.ParseAdd("Loren/0.1");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static bool IsGitHub(RepositoryLocator repository) => string.Equals(repository.Provider, "github", StringComparison.Ordinal);

    private static string? NormalizeBranch(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string branch = value?.Trim() ?? string.Empty;
        if (branch.StartsWith("refs/heads/", StringComparison.Ordinal))
        {
            branch = branch["refs/heads/".Length..];
        }
        else if (branch.StartsWith("refs/", StringComparison.Ordinal))
        {
            return null;
        }

        if (!IsValidBranchName(branch) || IsFullCommitSha(branch))
        {
            return null;
        }

        return branch;
    }

    private static string RequiredString(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value)
            && value.ValueKind is JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw new InvalidOperationException();

    private static bool RequiredBoolean(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw new InvalidOperationException();

    private static int RequiredInt32(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out int result)
            ? result
            : throw new InvalidOperationException();

    private static bool IsFullCommitSha(string value) => value.Length == 40 && value.All(Uri.IsHexDigit);

    private static bool IsValidBranchName(string branch) =>
        !string.IsNullOrWhiteSpace(branch)
        && branch.Length <= 255
        && !branch.StartsWith('/')
        && !branch.EndsWith('/')
        && !branch.EndsWith('.')
        && !branch.Contains("..", StringComparison.Ordinal)
        && !branch.Contains("@{", StringComparison.Ordinal)
        && !branch.Contains("//", StringComparison.Ordinal)
        && !branch.Any(c => char.IsControl(c) || char.IsWhiteSpace(c) || c is '~' or '^' or ':' or '?' or '*' or '[' or '\\')
        && branch.Split('/').All(component => component.Length > 0 && !component.StartsWith('.') && !component.EndsWith(".lock", StringComparison.OrdinalIgnoreCase));
}

public sealed record GitHubRepositoryMetadataResult(
    bool Success,
    RepositoryLocator Repository,
    string? DefaultBranch,
    IReadOnlyDictionary<string, string> Data,
    string? Error)
{
    public static GitHubRepositoryMetadataResult Failure(RepositoryLocator repository, string error) =>
        new(false, repository, null, new Dictionary<string, string>(), error);
}

public sealed record GitHubRepositoryResolutionResult(
    bool Success,
    RepositoryLocator Repository,
    string? DefaultBranch,
    string? SourceRef,
    string? SourceSha,
    string? Error)
{
    public static GitHubRepositoryResolutionResult Failure(RepositoryLocator repository, string error, string? defaultBranch = null) =>
        new(false, repository, defaultBranch, null, null, error);
}
