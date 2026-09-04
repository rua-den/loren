using System.Text.Json;
using Loren.Core.Brains;
using Loren.Core.Projects;
using CanonicalRepository = Loren.Core.Projects.Repository;

namespace Loren.Web;

public sealed class LorenProjectContextBuilder
{
    private static readonly JsonSerializerOptions ContextJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IProjectCatalog _projectCatalog;
    private readonly LorenMemoryContextBuilder? _memoryContextBuilder;

    public LorenProjectContextBuilder(IProjectCatalog projectCatalog)
        : this(projectCatalog, null)
    {
    }

    public LorenProjectContextBuilder(
        IProjectCatalog projectCatalog,
        LorenMemoryContextBuilder? memoryContextBuilder)
    {
        _projectCatalog = projectCatalog ?? throw new ArgumentNullException(nameof(projectCatalog));
        _memoryContextBuilder = memoryContextBuilder;
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
        string projectSystemContext = BuildSystemContext(projectContext);
        PreparedMemoryContext? memoryContext = _memoryContextBuilder is null
            ? null
            : await _memoryContextBuilder.BuildAsync(
                snapshot.Project.Id,
                cancellationToken);

        List<BrainInput> inputs =
        [
            new BrainMessage(BrainRole.System, projectSystemContext),
        ];

        if (memoryContext?.SystemContext is string memorySystemContext)
        {
            inputs.Add(new BrainMessage(BrainRole.System, memorySystemContext));
        }

        inputs.Add(new BrainMessage(BrainRole.User, message));
        BrainContext brainContext = new(inputs);

        return new PreparedLorenContext(
            brainContext,
            projectContext,
            memoryContext);
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
        string payload = JsonSerializer.Serialize(projectContext, ContextJsonOptions);

        return $"""
            Loren canonical project context follows. This is trusted configured identity/context, not live external state.
            Use repository locators from this context to resolve project identity. Fetch current external facts through authorized tools instead of assuming they are current.
            {payload}
            """;
    }
}

public sealed record PreparedLorenContext(
    BrainContext BrainContext,
    LorenProjectContext? Project,
    PreparedMemoryContext? Memory = null);

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
