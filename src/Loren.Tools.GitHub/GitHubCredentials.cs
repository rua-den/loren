using Loren.Core.Credentials;

namespace Loren.Tools.GitHub;

public static class GitHubCredentials
{
    public static CredentialPurpose WritePurpose { get; } = new("github.write");

    public static CredentialReference LocalV01WriteReference { get; } =
        new("github.write.local-v0.1");
}
