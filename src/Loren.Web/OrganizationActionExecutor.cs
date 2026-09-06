using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Loren.Core.Actions;
using Loren.Core.Organization;
using Loren.Core.Projects;

namespace Loren.Web;

public sealed class OrganizationActionExecutor : ITrustedActionExecutor
{
    private const int MaxModelVisibleContentCharacters = 1200;

    private readonly string _actionName;
    private readonly IOrganizationStore _store;
    private readonly TimeProvider _timeProvider;

    public OrganizationActionExecutor(
        string actionName,
        IOrganizationStore store,
        TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        _actionName = actionName;
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string ActionName => _actionName;

    public Task<ActionResult> ExecuteAsync(
        ActionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Failure(
            request.Name,
            "Organization state requires authenticated trusted owner execution context."));
    }

    public async Task<ActionResult> ExecuteTrustedAsync(
        ActionExecutionRequest execution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(execution.Request);

        if (!string.Equals(execution.Request.Name, _actionName, StringComparison.Ordinal))
        {
            return Failure(execution.Request.Name, "Organization executor action identity mismatch.");
        }

        AuthenticatedOwnerContext? ownerContext = execution.OwnerContext;
        if (ownerContext is null)
        {
            return Failure(
                execution.Request.Name,
                "Organization state requires authenticated owner context.");
        }

        return execution.Request.Name switch
        {
            var name when name == OrganizationActions.CreateNote.Name =>
                await CreateAsync(
                    execution.Request,
                    ownerContext,
                    OrganizationItemKind.Note,
                    cancellationToken),
            var name when name == OrganizationActions.RecordDecision.Name =>
                await CreateAsync(
                    execution.Request,
                    ownerContext,
                    OrganizationItemKind.Decision,
                    cancellationToken),
            var name when name == OrganizationActions.CreateTask.Name =>
                await CreateTaskAsync(execution.Request, ownerContext, cancellationToken),
            var name when name == OrganizationActions.List.Name =>
                await ListAsync(execution.Request, ownerContext, cancellationToken),
            var name when name == OrganizationActions.CompleteTask.Name =>
                await SetTaskStatusAsync(
                    execution.Request,
                    OrganizationTaskStatus.Completed,
                    cancellationToken),
            var name when name == OrganizationActions.ReopenTask.Name =>
                await SetTaskStatusAsync(
                    execution.Request,
                    OrganizationTaskStatus.Open,
                    cancellationToken),
            _ => Failure(execution.Request.Name, "Unsupported organization action."),
        };
    }

    private async Task<ActionResult> CreateAsync(
        ActionRequest request,
        AuthenticatedOwnerContext ownerContext,
        OrganizationItemKind kind,
        CancellationToken cancellationToken)
    {
        if (!TryRequiredArgument(request, "content", out string? content, out ActionResult? failure))
        {
            return failure!;
        }

        string? title = OptionalArgument(request, "title");
        DateTimeOffset now = _timeProvider.GetUtcNow();
        OrganizationItem item = new(
            OrganizationItemId.New(),
            kind,
            title,
            content,
            ownerContext.ProjectId,
            null,
            SourceReference(ownerContext),
            now,
            now);

        await _store.AddAsync(item, cancellationToken);
        return Success(request.Name, item);
    }

    private async Task<ActionResult> CreateTaskAsync(
        ActionRequest request,
        AuthenticatedOwnerContext ownerContext,
        CancellationToken cancellationToken)
    {
        if (!TryRequiredArgument(request, "title", out string? title, out ActionResult? failure))
        {
            return failure!;
        }

        string? details = OptionalArgument(request, "details");
        DateTimeOffset now = _timeProvider.GetUtcNow();
        OrganizationItem item = new(
            OrganizationItemId.New(),
            OrganizationItemKind.Task,
            title,
            details ?? title,
            ownerContext.ProjectId,
            OrganizationTaskStatus.Open,
            SourceReference(ownerContext),
            now,
            now);

        await _store.AddAsync(item, cancellationToken);
        return Success(request.Name, item);
    }

    private async Task<ActionResult> ListAsync(
        ActionRequest request,
        AuthenticatedOwnerContext ownerContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseKind(OptionalArgument(request, "kind"), out OrganizationItemKind? kind))
        {
            return Failure(request.Name, "Argument 'kind' must be note, decision, or task.");
        }

        if (!TryParseStatus(OptionalArgument(request, "status"), out OrganizationTaskStatus? status))
        {
            return Failure(request.Name, "Argument 'status' must be open or completed.");
        }

        if (status is not null && kind is not null && kind is not OrganizationItemKind.Task)
        {
            return Failure(request.Name, "Task status can only be used with kind=task.");
        }

        string scope = OptionalArgument(request, "scope")?.ToLowerInvariant() ?? "current";
        if (scope is not ("current" or "all"))
        {
            return Failure(request.Name, "Argument 'scope' must be current or all.");
        }

        ProjectId? projectFilter = scope == "current"
            ? ownerContext.ProjectId
            : null;
        IReadOnlyList<OrganizationItem> items = await _store.ListAsync(
            projectFilter,
            kind,
            status,
            limit: 50,
            cancellationToken);

        OrganizationItemView[] views = items.Select(ToView).ToArray();
        return new ActionResult(
            request.Name,
            true,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["scope"] = scope,
                ["project_id"] = projectFilter?.ToString() ?? string.Empty,
                ["item_count"] = views.Length.ToString(CultureInfo.InvariantCulture),
                ["items_json"] = JsonSerializer.Serialize(views),
            });
    }

    private async Task<ActionResult> SetTaskStatusAsync(
        ActionRequest request,
        OrganizationTaskStatus status,
        CancellationToken cancellationToken)
    {
        if (!TryRequiredArgument(request, "task_id", out string? taskIdText, out ActionResult? failure))
        {
            return failure!;
        }

        OrganizationItemId taskId;
        try
        {
            taskId = OrganizationItemId.Parse(taskIdText);
        }
        catch (FormatException)
        {
            return Failure(request.Name, "Argument 'task_id' is not a valid Loren task ID.");
        }

        OrganizationItem updated = await _store.SetTaskStatusAsync(
            taskId,
            status,
            _timeProvider.GetUtcNow(),
            cancellationToken);
        return Success(request.Name, updated);
    }

    private static bool TryRequiredArgument(
        ActionRequest request,
        string name,
        [NotNullWhen(true)] out string? value,
        out ActionResult? failure)
    {
        value = null;
        failure = null;
        if (!request.Arguments.TryGetValue(name, out string? raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            failure = Failure(request.Name, $"Argument '{name}' is required.");
            return false;
        }

        value = raw.Trim();
        return true;
    }

    private static string? OptionalArgument(ActionRequest request, string name) =>
        request.Arguments.TryGetValue(name, out string? raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw.Trim()
            : null;

    private static bool TryParseKind(string? value, out OrganizationItemKind? kind)
    {
        kind = value?.ToLowerInvariant() switch
        {
            null => null,
            "note" => OrganizationItemKind.Note,
            "decision" => OrganizationItemKind.Decision,
            "task" => OrganizationItemKind.Task,
            _ => (OrganizationItemKind?)null,
        };

        return value is null || kind is not null;
    }

    private static bool TryParseStatus(string? value, out OrganizationTaskStatus? status)
    {
        status = value?.ToLowerInvariant() switch
        {
            null => null,
            "open" => OrganizationTaskStatus.Open,
            "completed" => OrganizationTaskStatus.Completed,
            _ => (OrganizationTaskStatus?)null,
        };

        return value is null || status is not null;
    }

    private static string SourceReference(AuthenticatedOwnerContext ownerContext) =>
        $"owner:{ownerContext.OwnerPrincipalReference}";

    private static ActionResult Success(string actionName, OrganizationItem item) =>
        new(
            actionName,
            true,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["item_id"] = item.Id.ToString(),
                ["kind"] = item.Kind.ToString().ToLowerInvariant(),
                ["item_json"] = JsonSerializer.Serialize(ToView(item)),
            });

    private static OrganizationItemView ToView(OrganizationItem item) => new(
        item.Id.ToString(),
        item.Kind.ToString().ToLowerInvariant(),
        item.Title,
        Truncate(item.Content, MaxModelVisibleContentCharacters),
        item.ProjectId?.ToString(),
        item.TaskStatus?.ToString().ToLowerInvariant(),
        item.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
        item.UpdatedAt.ToString("O", CultureInfo.InvariantCulture),
        item.CompletedAt?.ToString("O", CultureInfo.InvariantCulture));

    private static string Truncate(string value, int maxCharacters) =>
        value.Length <= maxCharacters
            ? value
            : value[..(maxCharacters - 1)] + "…";

    private static ActionResult Failure(string actionName, string error) =>
        new(actionName, false, new Dictionary<string, string>(), error);

    private sealed record OrganizationItemView(
        string ItemId,
        string Kind,
        string? Title,
        string Content,
        string? ProjectId,
        string? TaskStatus,
        string CreatedAtUtc,
        string UpdatedAtUtc,
        string? CompletedAtUtc);
}
