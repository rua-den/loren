using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Loren.Core.Projects;

namespace Loren.Tools.GitHub;

public sealed record GitHubCreateBranchOperationResult(
    bool Success,
    string RepositoryFullName,
    string Branch,
    string SourceSha,
    string? DefaultBranch,
    string? VerifiedSha,
    string? Error);

public sealed class GitHubCreateBranchClient
{
    private readonly HttpClient _httpClient;

    public GitHubCreateBranchClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<GitHubCreateBranchOperationResult> CreateAndVerifyAsync(
        RepositoryLocator repository,
        string branch,
        string sourceSha,
        string credentialSecret,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialSecret);
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedBranch = branch?.Trim() ?? string.Empty;
        string normalizedSourceSha = sourceSha?.Trim().ToLowerInvariant() ?? string.Empty;
        string repositoryFullName = repository.FullName;

        if (!string.Equals(repository.Provider, "github", StringComparison.Ordinal))
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                "Canonical repository provider is not GitHub.");
        }

        if (!IsValidBranchName(normalizedBranch))
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                "Branch name is not a safe Git reference name.");
        }

        if (!IsFullCommitSha(normalizedSourceSha))
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                "Source SHA must be an exact 40-character hexadecimal commit SHA.");
        }

        string repositoryPath =
            $"repos/{Uri.EscapeDataString(repository.ExternalNamespace)}/{Uri.EscapeDataString(repository.ExternalName)}";

        string? defaultBranch = await ReadDefaultBranchAsync(
            repositoryPath,
            credentialSecret,
            cancellationToken);
        if (defaultBranch is null)
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                "GitHub repository preflight could not confirm the default branch.");
        }

        if (string.Equals(normalizedBranch, defaultBranch, StringComparison.Ordinal))
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                "Creating or replacing the repository default branch is forbidden.",
                defaultBranch);
        }

        using HttpRequestMessage createRequest = CreateRequest(
            HttpMethod.Post,
            $"{repositoryPath}/git/refs",
            credentialSecret);
        createRequest.Content = JsonContent.Create(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ref"] = $"refs/heads/{normalizedBranch}",
                ["sha"] = normalizedSourceSha,
            });

        using HttpResponseMessage createResponse = await _httpClient.SendAsync(
            createRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                $"GitHub create-ref returned HTTP {(int)createResponse.StatusCode} ({createResponse.StatusCode}).",
                defaultBranch);
        }

        string? verifiedSha = await ReadBranchShaAsync(
            repositoryPath,
            normalizedBranch,
            credentialSecret,
            cancellationToken);
        if (verifiedSha is null)
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                "Branch creation response was not accepted as success because post-write verification failed.",
                defaultBranch);
        }

        if (!string.Equals(verifiedSha, normalizedSourceSha, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                repositoryFullName,
                normalizedBranch,
                normalizedSourceSha,
                "Post-write verification returned a different branch commit SHA.",
                defaultBranch,
                verifiedSha);
        }

        return new GitHubCreateBranchOperationResult(
            true,
            repositoryFullName,
            normalizedBranch,
            normalizedSourceSha,
            defaultBranch,
            verifiedSha.ToLowerInvariant(),
            null);
    }

    private async Task<string?> ReadDefaultBranchAsync(
        string repositoryPath,
        string credentialSecret,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Get,
            repositoryPath,
            credentialSecret);
        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("default_branch", out JsonElement value)
                && value.ValueKind is JsonValueKind.String
                && value.GetString() is string defaultBranch
                && !string.IsNullOrWhiteSpace(defaultBranch))
            {
                return defaultBranch;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private async Task<string?> ReadBranchShaAsync(
        string repositoryPath,
        string branch,
        string credentialSecret,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Get,
            $"{repositoryPath}/git/ref/heads/{Uri.EscapeDataString(branch)}",
            credentialSecret);
        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("object", out JsonElement gitObject)
                && gitObject.ValueKind is JsonValueKind.Object
                && gitObject.TryGetProperty("sha", out JsonElement sha)
                && sha.ValueKind is JsonValueKind.String
                && sha.GetString() is string value
                && IsFullCommitSha(value))
            {
                return value;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string relativePath,
        string credentialSecret)
    {
        HttpRequestMessage request = new(
            method,
            new Uri($"https://api.github.com/{relativePath}"));
        request.Headers.UserAgent.ParseAdd("Loren/0.1");
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            credentialSecret);
        return request;
    }

    private static bool IsFullCommitSha(string value) =>
        value.Length == 40 && value.All(Uri.IsHexDigit);

    private static bool IsValidBranchName(string branch)
    {
        if (string.IsNullOrWhiteSpace(branch)
            || branch.Length > 255
            || branch.StartsWith("refs/", StringComparison.Ordinal)
            || branch.StartsWith('/')
            || branch.EndsWith('/')
            || branch.EndsWith('.')
            || branch.Contains("..", StringComparison.Ordinal)
            || branch.Contains("@{", StringComparison.Ordinal)
            || branch.Contains("//", StringComparison.Ordinal)
            || branch.Any(character =>
                char.IsControl(character)
                || char.IsWhiteSpace(character)
                || character is '~' or '^' or ':' or '?' or '*' or '[' or '\\'))
        {
            return false;
        }

        foreach (string component in branch.Split('/'))
        {
            if (component.Length == 0
                || component.StartsWith('.')
                || component.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static GitHubCreateBranchOperationResult Failure(
        string repositoryFullName,
        string branch,
        string sourceSha,
        string error,
        string? defaultBranch = null,
        string? verifiedSha = null) =>
        new(
            false,
            repositoryFullName,
            branch,
            sourceSha,
            defaultBranch,
            verifiedSha,
            error);
}
