using System.Security.Cryptography;
using System.Text;
using Loren.Core.Projects;

namespace Loren.Core.Actions;

public sealed record ActionAuthorizationContext
{
    public ActionAuthorizationContext(
        ProjectId projectId,
        RepositoryId repositoryId,
        RepositoryLocator repositoryLocator,
        string ownerPrincipalReference,
        IReadOnlyDictionary<string, string>? normalizedTarget = null)
    {
        ArgumentNullException.ThrowIfNull(repositoryLocator);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPrincipalReference);

        ProjectId = projectId;
        RepositoryId = repositoryId;
        RepositoryLocator = repositoryLocator;
        OwnerPrincipalReference = ownerPrincipalReference.Trim();
        NormalizedTarget = Normalize(normalizedTarget);
    }

    public ProjectId ProjectId { get; }

    public RepositoryId RepositoryId { get; }

    public RepositoryLocator RepositoryLocator { get; }

    public string OwnerPrincipalReference { get; }

    public IReadOnlyDictionary<string, string> NormalizedTarget { get; }

    private static Dictionary<string, string> Normalize(
        IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        Dictionary<string, string> normalized = new(StringComparer.Ordinal);
        foreach ((string key, string value) in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            normalized.Add(key.Trim(), value?.Trim() ?? string.Empty);
        }

        return normalized;
    }
}

public interface IWriteSafetyState
{
    bool IsReadOnly { get; }
}

public static class ActionIntentFingerprint
{
    public static string Compute(
        ActionDefinition definition,
        ActionRequest request,
        ActionAuthorizationContext authorizationContext)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(authorizationContext);

        StringBuilder canonical = new();
        Append(canonical, "action", definition.Name);
        Append(canonical, "access", definition.AccessClass.ToString());
        Append(canonical, "project", authorizationContext.ProjectId.ToString());
        Append(canonical, "repository", authorizationContext.RepositoryId.ToString());
        Append(canonical, "provider", authorizationContext.RepositoryLocator.Provider);
        Append(canonical, "namespace", authorizationContext.RepositoryLocator.ExternalNamespace);
        Append(canonical, "name", authorizationContext.RepositoryLocator.ExternalName);
        Append(canonical, "principal", authorizationContext.OwnerPrincipalReference);
        AppendDictionary(canonical, "target", authorizationContext.NormalizedTarget);
        AppendDictionary(canonical, "argument", request.Arguments);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(hash);
    }

    private static void AppendDictionary(
        StringBuilder builder,
        string prefix,
        IReadOnlyDictionary<string, string> values)
    {
        foreach ((string key, string value) in values.OrderBy(
                     pair => pair.Key,
                     StringComparer.Ordinal))
        {
            Append(builder, $"{prefix}-key", key);
            Append(builder, $"{prefix}-value", value ?? string.Empty);
        }
    }

    private static void Append(StringBuilder builder, string label, string value)
    {
        builder
            .Append(label.Length)
            .Append(':')
            .Append(label)
            .Append('=')
            .Append(value.Length)
            .Append(':')
            .Append(value)
            .Append(';');
    }
}
