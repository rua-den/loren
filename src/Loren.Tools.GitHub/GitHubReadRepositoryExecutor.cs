using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Loren.Core.Actions;

namespace Loren.Tools.GitHub;

public sealed class GitHubReadRepositoryExecutor : IActionExecutor
{
    private readonly HttpClient _httpClient;

    public GitHubReadRepositoryExecutor(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string ActionName => GitHubActions.ReadRepository.Name;

    public async Task<ActionResult> ExecuteAsync(
        ActionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Arguments.TryGetValue("owner", out string? owner)
            || string.IsNullOrWhiteSpace(owner)
            || !request.Arguments.TryGetValue("repository", out string? repository)
            || string.IsNullOrWhiteSpace(repository))
        {
            return Failure(request.Name, "Arguments 'owner' and 'repository' are required.");
        }

        string path = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}";
        Uri uri = new($"https://api.github.com/{path}");

        using HttpRequestMessage message = new(HttpMethod.Get, uri);
        message.Headers.UserAgent.ParseAdd("Loren/0.1");
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        message.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");

        using HttpResponseMessage response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Failure(
                request.Name,
                $"GitHub returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }

        try
        {
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
            JsonElement root = document.RootElement;

            Dictionary<string, string> data = new(StringComparer.Ordinal)
            {
                ["full_name"] = RequiredString(root, "full_name"),
                ["default_branch"] = RequiredString(root, "default_branch"),
                ["private"] = RequiredBoolean(root, "private").ToString().ToLowerInvariant(),
                ["archived"] = RequiredBoolean(root, "archived").ToString().ToLowerInvariant(),
                ["open_issues_count"] = RequiredInt32(root, "open_issues_count").ToString(CultureInfo.InvariantCulture),
                ["pushed_at"] = RequiredString(root, "pushed_at"),
                ["html_url"] = RequiredString(root, "html_url"),
            };

            return new ActionResult(request.Name, true, data);
        }
        catch (JsonException)
        {
            return Failure(request.Name, "GitHub response could not be parsed.");
        }
        catch (InvalidOperationException)
        {
            return Failure(request.Name, "GitHub response was missing required repository metadata.");
        }
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind is JsonValueKind.String
            && value.GetString() is string text
            && !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        throw new InvalidOperationException($"Missing property: {propertyName}");
    }

    private static bool RequiredBoolean(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        throw new InvalidOperationException($"Missing property: {propertyName}");
    }

    private static int RequiredInt32(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out JsonElement value)
            && value.TryGetInt32(out int number))
        {
            return number;
        }

        throw new InvalidOperationException($"Missing property: {propertyName}");
    }

    private static ActionResult Failure(string actionName, string error) =>
        new(actionName, false, new Dictionary<string, string>(), error);
}
