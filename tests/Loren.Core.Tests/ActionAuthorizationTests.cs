using Loren.Core.Actions;
using Loren.Core.Projects;
using Xunit;

namespace Loren.Core.Tests;

public sealed class ActionAuthorizationTests
{
    [Fact]
    public void LegacyReadOnlyFlagMapsToTypedAccessClass()
    {
        ActionDefinition read = new("read", "read", true);
        ActionDefinition write = new("write", "write", false);

        Assert.Equal(ActionAccessClass.Read, read.AccessClass);
        Assert.True(read.IsReadOnly);
        Assert.Equal(ActionAccessClass.ExternalWrite, write.AccessClass);
        Assert.False(write.IsReadOnly);
    }

    [Fact]
    public void FingerprintIsStableAcrossDictionaryInsertionOrder()
    {
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        RepositoryLocator locator = new("github", "rua-den", "loren");
        ActionDefinition definition = new(
            "github.update_file",
            "Update a file",
            ActionAccessClass.ExternalWrite);

        ActionAuthorizationContext firstContext = new(
            projectId,
            repositoryId,
            locator,
            "owner:session-1",
            new Dictionary<string, string>
            {
                ["path"] = "README.md",
                ["branch"] = "feat/test",
            });
        ActionAuthorizationContext secondContext = new(
            projectId,
            repositoryId,
            locator,
            "owner:session-1",
            new Dictionary<string, string>
            {
                ["branch"] = "feat/test",
                ["path"] = "README.md",
            });

        ActionRequest firstRequest = new(
            definition.Name,
            new Dictionary<string, string>
            {
                ["content_digest"] = "ABC",
                ["message"] = "Update docs",
            });
        ActionRequest secondRequest = new(
            definition.Name,
            new Dictionary<string, string>
            {
                ["message"] = "Update docs",
                ["content_digest"] = "ABC",
            });

        string first = ActionIntentFingerprint.Compute(definition, firstRequest, firstContext);
        string second = ActionIntentFingerprint.Compute(definition, secondRequest, secondContext);

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void FingerprintChangesWhenSecurityRelevantIntentChanges()
    {
        ProjectId projectId = ProjectId.New();
        RepositoryId repositoryId = RepositoryId.New();
        RepositoryLocator locator = new("github", "rua-den", "loren");
        ActionDefinition definition = new(
            "github.update_file",
            "Update a file",
            ActionAccessClass.ExternalWrite);
        ActionRequest request = new(
            definition.Name,
            new Dictionary<string, string>
            {
                ["content_digest"] = "ABC",
            });
        ActionAuthorizationContext approved = new(
            projectId,
            repositoryId,
            locator,
            "owner:session-1",
            new Dictionary<string, string>
            {
                ["branch"] = "feat/approved",
                ["path"] = "README.md",
            });
        ActionAuthorizationContext changedBranch = new(
            projectId,
            repositoryId,
            locator,
            "owner:session-1",
            new Dictionary<string, string>
            {
                ["branch"] = "main",
                ["path"] = "README.md",
            });
        ActionRequest changedContent = new(
            definition.Name,
            new Dictionary<string, string>
            {
                ["content_digest"] = "DEF",
            });

        string baseline = ActionIntentFingerprint.Compute(definition, request, approved);

        Assert.NotEqual(
            baseline,
            ActionIntentFingerprint.Compute(definition, request, changedBranch));
        Assert.NotEqual(
            baseline,
            ActionIntentFingerprint.Compute(definition, changedContent, approved));
    }
}
