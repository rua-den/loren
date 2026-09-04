using Loren.Infrastructure.CanonicalState;
using Loren.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
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

app.MapPost(
        "/api/run",
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
                    cancellationToken);
                return Results.Ok(result);
            }
            catch (UnknownProjectAliasException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
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
                    cancellationToken);
                return Results.Ok(result);
            }
            catch (UnknownProjectAliasException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
}

app.Run();

public partial class Program;
