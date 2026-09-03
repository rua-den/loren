using Loren.Core.Actions;

namespace Loren.Tools.GitHub;

public static class GitHubActions
{
    public static readonly ActionDefinition ReadRepository = new(
        "github.read_repository",
        "Read current GitHub repository metadata.",
        true);
}
