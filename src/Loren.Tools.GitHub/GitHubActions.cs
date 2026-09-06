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

    public static readonly ActionDefinition CreateBranch = new(
        "github.create_branch",
        "Create a non-default GitHub branch from an exact approved source commit SHA.",
        ActionAccessClass.ReversibleWrite,
        [
            new ActionParameterDefinition(
                "branch",
                "New non-default branch name.",
                ActionParameterType.Text,
                true),
            new ActionParameterDefinition(
                "source_sha",
                "Exact 40-character source commit SHA.",
                ActionParameterType.Text,
                true),
        ]);
}
