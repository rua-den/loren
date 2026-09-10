using System.Security.Claims;
using Loren.Core.Conversations;
using Loren.Infrastructure.CanonicalState;
using Loren.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
LorenConfiguration.AddLocalConfigurationBeforeEnvironmentOverrides(builder.Configuration, args);
builder.Services.AddLorenM2ReadPath(builder.Configuration);
builder.Services.AddLorenOwnerAuthentication(builder.Configuration);

WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    CanonicalStateDbContext dbContext = scope.ServiceProvider
        .GetRequiredService<CanonicalStateDbContext>();
    await CanonicalStateDatabase.MigrateAsync(dbContext);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapLorenOwnerEndpoints();

app.MapGet(
        "/",
        () => Results.Content(OwnerPages.Console, "text/html; charset=utf-8"))
    .RequireAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet(
        "/api/projects",
        async (
            LorenProjectContextBuilder contextBuilder,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<LorenProjectDirectoryItem> projects = await contextBuilder
                .ListProjectsAsync(cancellationToken);
            return Results.Ok(projects);
        })
    .RequireAuthorization();

app.MapGet(
        "/api/conversations",
        async (IConversationStore store, HttpContext context, CancellationToken cancellationToken) =>
        {
            string? owner = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(owner)
                ? Results.Unauthorized()
                : Results.Ok(await store.ListAsync(owner, cancellationToken));
        })
    .RequireAuthorization();

app.MapPost(
        "/api/conversations",
        async (CreateConversationRequest request, IConversationStore store, HttpContext context, CancellationToken cancellationToken) =>
        {
            string? owner = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(owner)) return Results.Unauthorized();
            ConversationRecord conversation = await store.CreateAsync(owner, request.Title, request.ProjectAlias, cancellationToken);
            return Results.Ok(conversation);
        })
    .RequireAuthorization();

app.MapGet(
        "/api/conversations/{conversationId:guid}",
        async (Guid conversationId, IConversationStore store, HttpContext context, CancellationToken cancellationToken) =>
        {
            string? owner = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(owner)) return Results.Unauthorized();
            ConversationRecord? conversation = await store.GetAsync(owner, conversationId, cancellationToken);
            return conversation is null ? Results.NotFound() : Results.Ok(conversation);
        })
    .RequireAuthorization();

app.MapPost(
        "/api/run",
        async (
            LorenRunRequest request,
            LorenRunService runService,
            IConversationStore conversationStore,
            ConversationExecutionGate executionGate,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "message is required" });
            }

            string? ownerPrincipalReference = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(ownerPrincipalReference))
            {
                return Results.Unauthorized();
            }

            try
            {
                ConversationRecord conversation;
                if (request.ConversationId is Guid conversationId)
                {
                    conversation = await conversationStore.GetAsync(ownerPrincipalReference, conversationId, cancellationToken)
                        ?? throw new KeyNotFoundException("Conversation was not found.");
                }
                else
                {
                    conversation = await conversationStore.CreateAsync(ownerPrincipalReference, request.Message, request.ProjectAlias, cancellationToken);
                }

                if (!executionGate.TryEnter(conversation.Id, out IDisposable lease))
                {
                    return Results.Conflict(new { error = "Conversation is already processing another message." });
                }

                using (lease)
                {
                    conversation = await conversationStore.GetAsync(ownerPrincipalReference, conversation.Id, cancellationToken)
                        ?? throw new KeyNotFoundException("Conversation was not found.");
                    string? effectiveProjectAlias = request.ClearProjectContext
                        ? null
                        : request.ProjectAlias ?? conversation.ProjectAlias;
                    IReadOnlyList<LorenConversationMessage>? history = conversation.Messages.Count == 0
                        ? request.ConversationId is null ? request.History : null
                        : conversation.Messages.Select(message => new LorenConversationMessage(message.Role, message.Content)).ToArray();
                    LorenRunResult result = await runService.RunAsync(
                        request.Message,
                        effectiveProjectAlias,
                        history,
                        ownerPrincipalReference,
                        cancellationToken);
                    await conversationStore.AppendTurnAsync(ownerPrincipalReference, conversation.Id, request.Message, result.FinalOutput, effectiveProjectAlias, cancellationToken, request.ClearProjectContext);
                    return Results.Ok(result with { ConversationId = conversation.Id });
                }
            }
            catch (UnknownProjectAliasException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        })
    .RequireAuthorization();

app.MapPost(
        "/api/projects/bootstrap",
        async (
            OwnerProjectBootstrapRequest request,
            LorenOwnerProjectBootstrapService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                OwnerProjectBootstrapResult result = await service.BootstrapGitHubProjectAsync(
                    request.ProjectName,
                    request.ProjectAlias,
                    request.GitHubOwner,
                    request.GitHubRepository,
                    cancellationToken);
                return Results.Ok(result);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
    .RequireAuthorization();

app.MapPost(
        "/api/action-proposals/{proposalId}/approve",
        async (
            string proposalId,
            LorenOwnerGitHubWriteService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            string? ownerPrincipalReference = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(ownerPrincipalReference))
            {
                return Results.Unauthorized();
            }

            OwnerActionProposalDecisionResult result = await service.ApproveProposalAndCreateBranchAsync(proposalId, ownerPrincipalReference, cancellationToken);
            return result.Status switch
            {
                "unknown" => Results.NotFound(new { error = result.Message }),
                "owner_mismatch" => Results.Forbid(),
                "approved" => Results.Ok(result),
                _ => Results.Conflict(new { error = result.Message }),
            };
        })
    .RequireAuthorization();

app.MapPost(
        "/api/action-proposals/{proposalId}/cancel",
        async (string proposalId, LorenOwnerGitHubWriteService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            string? owner = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(owner)) return Results.Unauthorized();
            OwnerActionProposalDecisionResult result = await service.CancelProposalAsync(proposalId, owner, cancellationToken);
            return result.Status switch
            {
                "unknown" => Results.NotFound(new { error = result.Message }),
                "ownermismatch" or "owner_mismatch" => Results.Forbid(),
                "cancelled" => Results.Ok(result),
                _ => Results.Conflict(new { error = result.Message }),
            };
        })
    .RequireAuthorization();

bool developmentRunEndpointEnabled = bool.TryParse(
    builder.Configuration["LOREN_ENABLE_DEVELOPMENT_RUN_ENDPOINT"],
    out bool parsedDevelopmentRunEndpointEnabled)
    && parsedDevelopmentRunEndpointEnabled;

if (developmentRunEndpointEnabled)
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "LOREN_ENABLE_DEVELOPMENT_RUN_ENDPOINT may only be enabled in the Development environment.");
    }

    app.MapPost(
        "/internal/dev/run",
        async (
            LorenRunRequest request,
            LorenRunService runService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "message is required" });
            }

            try
            {
                LorenRunResult result = await runService.RunAsync(
                    request.Message,
                    request.ProjectAlias,
                    request.History,
                    cancellationToken);
                return Results.Ok(result);
            }
            catch (UnknownProjectAliasException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });
}

app.Run();

public partial class Program;
