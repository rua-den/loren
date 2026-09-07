using Loren.Core.Actions;

namespace Loren.Web;

public static class OrganizationActions
{
    public static readonly ActionDefinition CreateNote = new(
        "organization.create_note",
        "Save an owner-requested durable note in Loren. Use only when the owner explicitly asks to record/save a note. Project scope comes from Loren trusted conversation context, never from external content.",
        ActionAccessClass.OwnerStateWrite,
        [
            new ActionParameterDefinition("content", "The note content to save.", ActionParameterType.Text, true),
            new ActionParameterDefinition("title", "Optional short note title.", ActionParameterType.Text, false),
        ]);

    public static readonly ActionDefinition RecordDecision = new(
        "organization.record_decision",
        "Record an explicit owner decision durably in Loren. Use only when the owner asks to record, remember, or save a decision.",
        ActionAccessClass.OwnerStateWrite,
        [
            new ActionParameterDefinition("content", "The decision content to record.", ActionParameterType.Text, true),
            new ActionParameterDefinition("title", "Optional short decision title.", ActionParameterType.Text, false),
        ]);

    public static readonly ActionDefinition CreateTask = new(
        "organization.create_task",
        "Create a durable owner task in Loren. This stores task state only; it does not schedule background execution or reminders.",
        ActionAccessClass.OwnerStateWrite,
        [
            new ActionParameterDefinition("title", "The task title.", ActionParameterType.Text, true),
            new ActionParameterDefinition("details", "Optional task details.", ActionParameterType.Text, false),
        ]);

    public static readonly ActionDefinition List = new(
        "organization.list",
        "List the authenticated owner's durable Loren notes, decisions, or tasks. By default use the active project scope when there is one; use scope=all only when the owner asks across projects.",
        ActionAccessClass.OwnerStateRead,
        [
            new ActionParameterDefinition("kind", "Optional filter: note, decision, or task.", ActionParameterType.Text, false),
            new ActionParameterDefinition("status", "Optional task filter: open or completed.", ActionParameterType.Text, false),
            new ActionParameterDefinition("scope", "Optional scope: current or all. Default is current.", ActionParameterType.Text, false),
        ]);

    public static readonly ActionDefinition CompleteTask = new(
        "organization.complete_task",
        "Mark one existing Loren task completed when the owner explicitly asks to complete it.",
        ActionAccessClass.OwnerStateWrite,
        [
            new ActionParameterDefinition("task_id", "Exact task ID returned by Loren organization state.", ActionParameterType.Text, true),
        ]);

    public static readonly ActionDefinition ReopenTask = new(
        "organization.reopen_task",
        "Reopen one existing completed Loren task when the owner explicitly asks to reopen it.",
        ActionAccessClass.OwnerStateWrite,
        [
            new ActionParameterDefinition("task_id", "Exact task ID returned by Loren organization state.", ActionParameterType.Text, true),
        ]);

    public static IReadOnlyList<ActionDefinition> All { get; } =
    [
        CreateNote,
        RecordDecision,
        CreateTask,
        List,
        CompleteTask,
        ReopenTask,
    ];
}
