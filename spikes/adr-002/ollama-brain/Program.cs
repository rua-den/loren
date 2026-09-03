using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const int MaxTurns = 6;
const int DefaultTimeoutSeconds = 60;

string? apiKey = Environment.GetEnvironmentVariable("OLLAMA_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("OLLAMA_API_KEY is required for the live ADR-002 Ollama brain spike.");
    Environment.ExitCode = 2;
    return;
}

string model = Environment.GetEnvironmentVariable("LOREN_OLLAMA_MODEL") ?? "gpt-oss:120b";
string endpoint = Environment.GetEnvironmentVariable("LOREN_OLLAMA_ENDPOINT") ?? "https://ollama.com/api/chat";
int timeoutSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("LOREN_OLLAMA_TIMEOUT_SECONDS"),
    out int configuredTimeoutSeconds)
    ? configuredTimeoutSeconds
    : DefaultTimeoutSeconds;

if (timeoutSeconds <= 0)
{
    throw new InvalidOperationException("LOREN_OLLAMA_TIMEOUT_SECONDS must be greater than zero.");
}

int? cancelAfterMs = null;
string? cancelAfterRaw = Environment.GetEnvironmentVariable("LOREN_OLLAMA_CANCEL_AFTER_MS");
if (!string.IsNullOrWhiteSpace(cancelAfterRaw))
{
    if (!int.TryParse(cancelAfterRaw, out int parsedCancelAfterMs) || parsedCancelAfterMs <= 0)
    {
        throw new InvalidOperationException("LOREN_OLLAMA_CANCEL_AFTER_MS must be a positive integer when provided.");
    }

    cancelAfterMs = parsedCancelAfterMs;
}

bool expectCancellation = bool.TryParse(
    Environment.GetEnvironmentVariable("LOREN_EXPECT_CANCELLATION"),
    out bool parsedExpectCancellation) && parsedExpectCancellation;

if (expectCancellation && cancelAfterMs is null)
{
    throw new InvalidOperationException(
        "LOREN_EXPECT_CANCELLATION=true requires LOREN_OLLAMA_CANCEL_AFTER_MS.");
}

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
Console.CancelKeyPress += cancelHandler;

try
{
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

    JsonObject tool = new()
    {
        ["type"] = "function",
        ["function"] = new JsonObject
        {
            ["name"] = "get_project_status",
            ["description"] = "Read the current status of a Loren project. This is a read-only action.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["project"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Canonical project name or alias"
                    }
                },
                ["required"] = new JsonArray("project")
            }
        }
    };

    JsonArray messages =
    [
        new JsonObject
        {
            ["role"] = "user",
            ["content"] =
                "Check project Loren. You must use get_project_status before answering. " +
                "After observing the tool result, answer in one concise sentence."
        }
    ];

    bool observedGatewayAction = false;
    bool cancellationArmed = false;

    Console.WriteLine($"[ollama-spike] model={model}");
    Console.WriteLine($"[ollama-spike] endpoint={endpoint}");
    Console.WriteLine($"[ollama-spike] timeout-seconds={timeoutSeconds}");
    if (cancelAfterMs is not null)
    {
        Console.WriteLine($"[ollama-spike] cancel-after-ms={cancelAfterMs}");
    }

    for (int turn = 1; turn <= MaxTurns; turn++)
    {
        cancellation.Token.ThrowIfCancellationRequested();
        Console.WriteLine($"[ollama-spike] turn={turn}");

        JsonObject request = new()
        {
            ["model"] = model,
            ["messages"] = messages.DeepClone(),
            ["tools"] = new JsonArray(tool.DeepClone()),
            ["stream"] = false
        };

        if (!cancellationArmed && cancelAfterMs is not null)
        {
            cancellation.CancelAfter(TimeSpan.FromMilliseconds(cancelAfterMs.Value));
            cancellationArmed = true;
        }

        using var content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
        string responseBody;
        try
        {
            using HttpResponseMessage response = await http.PostAsync(endpoint, content, cancellation.Token);
            responseBody = await response.Content.ReadAsStringAsync(cancellation.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Ollama provider returned HTTP {(int)response.StatusCode} ({response.StatusCode}). Body: {responseBody}");
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested && expectCancellation)
        {
            Console.WriteLine("[ollama-spike] PASS: cancellation was observed at the live Ollama provider await.");
            return;
        }

        JsonObject root = JsonNode.Parse(responseBody)?.AsObject()
            ?? throw new InvalidOperationException("Ollama returned an empty or invalid JSON response.");
        JsonObject message = root["message"]?.AsObject()
            ?? throw new InvalidOperationException("Ollama response did not contain a message object.");

        JsonArray? toolCalls = message["tool_calls"]?.AsArray();
        if (toolCalls is { Count: > 0 })
        {
            messages.Add(message.DeepClone());

            foreach (JsonNode? toolCallNode in toolCalls)
            {
                JsonObject toolCall = toolCallNode?.AsObject()
                    ?? throw new InvalidOperationException("Ollama returned an invalid tool call.");
                JsonObject function = toolCall["function"]?.AsObject()
                    ?? throw new InvalidOperationException("Ollama tool call did not contain a function object.");
                string functionName = function["name"]?.GetValue<string>()
                    ?? throw new InvalidOperationException("Ollama tool call function name is missing.");
                JsonObject arguments = function["arguments"]?.AsObject()
                    ?? throw new InvalidOperationException("Ollama tool call arguments are missing.");

                Console.WriteLine($"[brain] requested action={functionName}");
                string actionResult = ActionGateway.Execute(functionName, arguments);
                observedGatewayAction = true;
                Console.WriteLine($"[gateway] result={actionResult}");

                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_name"] = functionName,
                    ["content"] = actionResult
                });
            }

            continue;
        }

        string finalText = message["content"]?.GetValue<string>() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(finalText))
        {
            if (expectCancellation)
            {
                throw new InvalidOperationException(
                    "Expected provider cancellation, but Ollama returned a final response first.");
            }

            if (!observedGatewayAction)
            {
                throw new InvalidOperationException(
                    "Ollama returned a final message before any action crossed Loren's ActionGateway.");
            }

            Console.WriteLine($"[assistant] {finalText}");
            Console.WriteLine("[ollama-spike] PASS: model -> action request -> Loren gateway -> action result -> model final answer");
            return;
        }

        throw new InvalidOperationException("Ollama response contained neither tool calls nor final content.");
    }

    throw new InvalidOperationException($"Ollama brain spike exceeded the hard limit of {MaxTurns} turns.");
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine(
        expectCancellation
            ? "[ollama-spike] FAIL: cancellation occurred outside the live provider await."
            : "[ollama-spike] CANCELLED: cancellation propagated through the Ollama provider call.");
    Environment.ExitCode = 3;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

internal static class ActionGateway
{
    public static string Execute(string functionName, JsonObject arguments)
    {
        if (!string.Equals(functionName, "get_project_status", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Action '{functionName}' is not registered.");
        }

        string? project = arguments["project"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(project))
        {
            throw new InvalidOperationException("The project argument is required and cannot be empty.");
        }

        return JsonSerializer.Serialize(new
        {
            project,
            repository = "rua-den/loren",
            branch = "main",
            status = "planning",
            source = "fake-m0-executor"
        });
    }
}
