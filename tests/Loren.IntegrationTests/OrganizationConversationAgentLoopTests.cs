using Loren.Core.Actions;
using Loren.Core.Audit;
using Loren.Core.Brains;
using Loren.Core.Organization;
using Loren.Core.Projects;
using Loren.Infrastructure.Audit;
using Loren.Infrastructure.CanonicalState;
using Loren.Runtime;
using Loren.Web;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class OrganizationConversationAgentLoopTests
{
    [Fact]
    public async Task AuthenticatedConversationCanCreateListAndCompleteProjectTaskWithoutExternalApproval()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-m6a4-chat-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string connectionString =
            $"Data Source={Path.Combine(tempDirectory, "loren.db")};Pooling=False";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ProjectId projectId = ProjectId.New();

        try
        {
            await using CanonicalStateDbContext context = CreateContext(connectionString);
            await CanonicalStateDatabase.MigrateAsync(context, cancellationToken);
            SqliteProjectCatalog catalog = new(context);
            await catalog.SaveAsync(
                new ProjectSnapshot(
                    new Project(projectId, "Loren", ["loren"], now, now),
                    []),
                cancellationToken);

            SqliteOrganizationStore store = new(context);
            IActionExecutor[] executors =
            [
                new OrganizationActionExecutor(OrganizationActions.CreateTask.Name, store),
                new OrganizationActionExecutor(OrganizationActions.List.Name, store),
                new OrganizationActionExecutor(OrganizationActions.CompleteTask.Name, store),
            ];
            InMemoryAuditSink audit = new();
            ActionGateway gateway = new(
                [
                    OrganizationActions.CreateTask,
                    OrganizationActions.List,
                    OrganizationActions.CompleteTask,
                ],
                executors,
                new GateDActionPolicy(new FixedWriteSafetyState(isReadOnly: true)),
                audit);
            TaskLifecycleBrain brain = new();
            AgentLoop loop = new(
                brain,
                gateway,
                new AgentLoopOptions(MaxTurns: 5, MaxActions: 4));
            LorenRunService runService = new(
                loop,
                audit,
                new LorenProjectContextBuilder(catalog));

            LorenRunResult result = await runService.RunAsync(
                "Tạo task review auth flow cho project Loren, cho tao xem task open rồi đánh dấu nó xong.",
                "loren",
                history: null,
                ownerPrincipalReference: "owner",
                cancellationToken);

            Assert.Equal(4, result.Turns);
            Assert.Equal(3, result.ActionCount);
            Assert.Contains("completed", result.FinalOutput, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                result.Audit,
                entry => entry.Kind == AuditEventKind.ApprovalEvaluated.ToString());

            OrganizationItem task = Assert.Single(
                await store.ListAsync(
                    projectId,
                    OrganizationItemKind.Task,
                    limit: 10,
                    cancellationToken: cancellationToken));
            Assert.Equal(brain.TaskId, task.Id.ToString());
            Assert.Equal(OrganizationTaskStatus.Completed, task.TaskStatus);
            Assert.Equal(projectId, task.ProjectId);
            Assert.NotNull(task.CompletedAt);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static CanonicalStateDbContext CreateContext(string connectionString)
    {
        DbContextOptions<CanonicalStateDbContext> options =
            new DbContextOptionsBuilder<CanonicalStateDbContext>()
                .UseSqlite(connectionString)
                .Options;
        return new CanonicalStateDbContext(options);
    }

    private sealed class TaskLifecycleBrain : IBrain
    {
        private int _callCount;

        public string? TaskId { get; private set; }

        public Task<BrainTurnResult> ThinkAsync(
            BrainContext context,
            IReadOnlyList<ActionDefinition> availableActions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _callCount++;

            Assert.Contains(availableActions, action => action.Name == OrganizationActions.CreateTask.Name);
            Assert.Contains(availableActions, action => action.Name == OrganizationActions.List.Name);
            Assert.Contains(availableActions, action => action.Name == OrganizationActions.CompleteTask.Name);

            return _callCount switch
            {
                1 => Task.FromResult(BrainTurnResult.Request(
                    new ActionRequest(
                        OrganizationActions.CreateTask.Name,
                        new Dictionary<string, string>
                        {
                            ["title"] = "Review auth flow",
                            ["details"] = "Review authenticated owner flow before v0.1 checkpoint.",
                        }))),
                2 => ObserveCreateThenList(context),
                3 => ObserveListThenComplete(context),
                4 => ObserveCompleteThenFinish(context),
                _ => throw new InvalidOperationException("Unexpected brain turn."),
            };
        }

        private Task<BrainTurnResult> ObserveCreateThenList(BrainContext context)
        {
            BrainActionObservation observation = LastObservation(context);
            Assert.True(observation.Result.Success);
            TaskId = observation.Result.Data["item_id"];

            return Task.FromResult(BrainTurnResult.Request(
                new ActionRequest(
                    OrganizationActions.List.Name,
                    new Dictionary<string, string>
                    {
                        ["kind"] = "task",
                        ["status"] = "open",
                        ["scope"] = "current",
                    })));
        }

        private Task<BrainTurnResult> ObserveListThenComplete(BrainContext context)
        {
            BrainActionObservation observation = LastObservation(context);
            Assert.True(observation.Result.Success);
            Assert.NotNull(TaskId);
            Assert.Contains(TaskId, observation.Result.Data["items_json"], StringComparison.Ordinal);

            return Task.FromResult(BrainTurnResult.Request(
                new ActionRequest(
                    OrganizationActions.CompleteTask.Name,
                    new Dictionary<string, string>
                    {
                        ["task_id"] = TaskId,
                    })));
        }

        private Task<BrainTurnResult> ObserveCompleteThenFinish(BrainContext context)
        {
            BrainActionObservation observation = LastObservation(context);
            Assert.True(observation.Result.Success);
            Assert.Contains("completed", observation.Result.Data["item_json"], StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(BrainTurnResult.Final(
                $"Task {TaskId} is completed and durably stored for Loren."));
        }

        private static BrainActionObservation LastObservation(BrainContext context) =>
            Assert.IsType<BrainActionObservation>(context.Inputs[^1]);
    }
}
