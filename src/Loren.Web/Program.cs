using Loren.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddLorenM2ReadPath(builder.Configuration);

WebApplication app = builder.Build();

app.MapGet("/", () => Results.Text("Loren v0.1 M2 host"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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

            LorenRunResult result = await runService.RunAsync(
                request.Message,
                cancellationToken);
            return Results.Ok(result);
        });
}

app.Run();

public partial class Program;
