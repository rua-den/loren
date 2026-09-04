namespace Loren.Core.Projects;

public sealed record RepositoryLocator
{
    public RepositoryLocator(string provider, string externalNamespace, string externalName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalName);

        Provider = provider.Trim().ToLowerInvariant();
        ExternalNamespace = externalNamespace.Trim();
        ExternalName = externalName.Trim();
    }

    public string Provider { get; }

    public string ExternalNamespace { get; }

    public string ExternalName { get; }

    public string FullName => $"{ExternalNamespace}/{ExternalName}";
}
