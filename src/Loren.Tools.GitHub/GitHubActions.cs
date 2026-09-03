using Loren.Core.Actions;

namespace Loren.Tools.GitHub;

public static class GitHubActions
{
    public static readonly ActionDefinition ReadRepository = new(
        "github.read_repository",
        "Read current GitHub repository metadata.",
        true,
        [
            new ActionParameterDefinition(
                "owner",
                "GitHub repository owner or organization name.",
                ActionParameterType.Text,
                true),
            new ActionParameterDefinition(
                "repository",
                "GitHub repository name without the owner prefix.",
                ActionParameterType.Text,
                true),
        ]);
}
