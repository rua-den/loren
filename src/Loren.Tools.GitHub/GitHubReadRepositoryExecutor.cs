using Loren.Core.Actions;
using Loren.Core.Projects;

namespace Loren.Tools.GitHub;

public sealed class GitHubReadRepositoryExecutor : IActionExecutor
{
    private readonly GitHubRepositoryReadClient _client;

    public GitHubReadRepositoryExecutor(HttpClient httpClient)
        : this(new GitHubRepositoryReadClient(httpClient))
    {
    }

    public GitHubReadRepositoryExecutor(GitHubRepositoryReadClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public string ActionName => GitHubActions.ReadRepository.Name;

    public async Task<ActionResult> ExecuteAsync(ActionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Arguments.TryGetValue("owner", out string? owner)
            || string.IsNullOrWhiteSpace(owner)
            || !request.Arguments.TryGetValue("repository", out string? repository)
            || string.IsNullOrWhiteSpace(repository))
        {
            return Failure(request.Name, "Arguments 'owner' and 'repository' are required.");
        }

        GitHubRepositoryMetadataResult result = await _client.ReadRepositoryAsync(
            new RepositoryLocator("github", owner, repository), cancellationToken);
        return result.Success
            ? new ActionResult(request.Name, true, new Dictionary<string, string>(result.Data))
            : Failure(request.Name, result.Error ?? "GitHub repository read failed.");
    }

    private static ActionResult Failure(string actionName, string error) =>
        new(actionName, false, new Dictionary<string, string>(), error);
}
