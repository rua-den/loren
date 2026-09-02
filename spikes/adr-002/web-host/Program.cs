using Loren.Spike.Web.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddSingleton<FakeBrain>();

var app = builder.Build();

app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/brain", async (
    string input,
    FakeBrain brain,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Running M0 fake brain request");
    string result = await brain.ThinkAsync(input, cancellationToken);
    return Results.Text(result);
});

app.MapRazorComponents<App>();
app.Run();

internal sealed class FakeBrain
{
    public Task<string> ThinkAsync(string input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult($"fake-brain:{input}");
    }
}
