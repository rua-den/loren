using System.Text.Json;
using Loren.Core.Brains;
using Loren.Core.Projects;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.Web;

public sealed class LorenProjectContextBuilder
{
    private readonly IProjectCatalog _projectCatalog;

    public LorenProjectContextBuilder(IProjectCatalog projectCatalog)
    {
        _projectCatalog = projectCatalog ?? throw new ArgumentNullException(nameof(projectCatalog));
    }

    public async Task<PreparedLorenContext> BuildAsync(
        string message,
        string? projectAlias,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (string.IsNullOrWhiteSpace(projectAlias))
        {
            return new PreparedLorenContext(BrainContext.FromUser(message), null);
        }

        ProjectSnapshot? snapshot = await _projectCatalog.FindByAliasAsync(
            projectAlias,
            cancellationToken);

        if (snapshot is null)
        {
            throw new UnknownProjectAliasException(projectAlias);
        }

        LorenProjectContext projectContext = ToProjectContext(snapshot);
        string systemContext = BuildSystemContext(projectContext);
        BrainContext brainContext = new(
            [
                new BrainMessage(BrainRole.System, systemContext),
                new BrainMessage(BrainRole.User, message),
            ]);

        return new PreparedLorenContext(brainContext, projectContext);
    }

    private static LorenProjectContext ToProjectContext(ProjectSnapshot snapshot) => new(
        snapshot.Project.Id.ToString(),
        snapshot.Project.Name,
        snapshot.Project.Aliases,
        snapshot.Repositories
            .Select(ToRepositoryContext)
            .ToArray());

    private static LorenRepositoryContext ToRepositoryContext(CanonicalRepository repository) => new(
        repository.Id.ToString(),
        repository.Name,
        repository.Locator.Provider,
        repository.Locator.FullName);

    private static string BuildSystemContext(LorenProjectContext projectContext)
    {
        string payload = JsonSerializer.Serialize(
            projectContext,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            });

        return $"""
            Loren canonical project context follows. This is trusted configured identity/context, not live external state.
            Use repository locators from this context to resolve project identity. Fetch current external facts through authorized tools instead of assuming they are current.
            {payload}
            """;
    }
}

public sealed record PreparedLorenContext(
    BrainContext BrainContext,
    LorenProjectContext? Project);

public sealed record LorenProjectContext(
    string ProjectId,
    string Name,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<LorenRepositoryContext> Repositories);

public sealed record LorenRepositoryContext(
    string RepositoryId,
    string Name,
    string Provider,
    string ExternalFullName);

public sealed class UnknownProjectAliasException : InvalidOperationException
{
    public UnknownProjectAliasException(string projectAlias)
        : base($"No canonical project is configured for alias '{projectAlias}'.")
    {
        ProjectAlias = projectAlias;
    }

    public string ProjectAlias { get; }
}
