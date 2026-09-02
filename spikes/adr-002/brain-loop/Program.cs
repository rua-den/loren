using OpenAI.Responses;
using System.Text.Json;

#pragma warning disable OPENAI001

const int MaxTurns = 6;

string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("OPENAI_API_KEY is required for the live ADR-002 brain spike.");
    Environment.ExitCode = 2;
    return;
}

string model = Environment.GetEnvironmentVariable("LOREN_OPENAI_MODEL") ?? "gpt-5-mini";

FunctionTool getProjectStatusTool = ResponseTool.CreateFunctionTool(
    functionName: "get_project_status",
    functionDescription: "Read the current status of a Loren project. This is a read-only action.",
    functionParameters: BinaryData.FromBytes("""
        {
          "type": "object",
          "properties": {
            "project": {
              "type": "string",
              "description": "Canonical project name or alias"
            }
          },
          "required": ["project"],
          "additionalProperties": false
        }
        """u8.ToArray()),
    strictModeEnabled: true);

ResponsesClient client = new(apiKey: apiKey);
List<ResponseItem> inputItems =
[
    ResponseItem.CreateUserMessageItem(
        "Check project Loren. You must use get_project_status before answering. " +
        "After observing the tool result, answer in one concise sentence."),
];

bool observedGatewayAction = false;

Console.WriteLine($"[spike] model={model}");
Console.WriteLine("[spike] starting Loren-controlled brain/tool loop");

for (int turn = 1; turn <= MaxTurns; turn++)
{
    Console.WriteLine($"[spike] turn={turn}");

    CreateResponseOptions options = new(model, inputItems)
    {
        Tools = { getProjectStatusTool },
    };

    ResponseResult response = client.CreateResponse(options);
    inputItems.AddRange(response.OutputItems);

    FunctionCallResponseItem? functionCall = response.OutputItems
        .OfType<FunctionCallResponseItem>()
        .FirstOrDefault();

    if (functionCall is not null)
    {
        Console.WriteLine($"[brain] requested action={functionCall.FunctionName}");

        string actionResult = ActionGateway.Execute(
            functionCall.FunctionName,
            functionCall.FunctionArguments);

        observedGatewayAction = true;
        Console.WriteLine($"[gateway] result={actionResult}");
        inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, actionResult));
        continue;
    }

    MessageResponseItem? message = response.OutputItems
        .OfType<MessageResponseItem>()
        .LastOrDefault();

    if (message is not null)
    {
        if (!observedGatewayAction)
        {
            throw new InvalidOperationException(
                "Brain returned a final message before any action crossed Loren's ActionGateway.");
        }

        string text = string.Join(
            Environment.NewLine,
            message.Content.Select(part => part.Text).Where(text => !string.IsNullOrWhiteSpace(text)));

        Console.WriteLine($"[assistant] {text}");
        Console.WriteLine("[spike] PASS: model -> action request -> Loren gateway -> action result -> model final answer");
        return;
    }
}

throw new InvalidOperationException($"Brain spike exceeded the hard limit of {MaxTurns} turns.");

internal static class ActionGateway
{
    public static string Execute(string functionName, BinaryData arguments)
    {
        if (!string.Equals(functionName, "get_project_status", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Action '{functionName}' is not registered.");
        }

        using JsonDocument document = JsonDocument.Parse(arguments.ToString());
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("project", out JsonElement projectElement))
        {
            throw new InvalidOperationException("The project argument is required.");
        }

        string? project = projectElement.GetString();
        if (string.IsNullOrWhiteSpace(project))
        {
            throw new InvalidOperationException("The project argument cannot be empty.");
        }

        // Deliberately fake/read-only for M0. The point is to prove that the provider
        // requests an action and Loren code intercepts it before any side effect.
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

#pragma warning restore OPENAI001
