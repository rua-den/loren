using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Loren.Core.Actions;
using Loren.Core.Brains;

namespace Loren.Brain.Ollama;

public sealed class OllamaBrain : IBrain
{
    private readonly HttpClient _httpClient;
    private readonly OllamaBrainOptions _options;
    private readonly string? _apiKey;

    public OllamaBrain(
        HttpClient httpClient,
        OllamaBrainOptions options,
        string? apiKey = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _apiKey = apiKey;
    }

    public async Task<BrainTurnResult> ThinkAsync(
        BrainContext context,
        IReadOnlyList<ActionDefinition> availableActions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(availableActions);
        cancellationToken.ThrowIfCancellationRequested();

        JsonObject payload = new()
        {
            ["model"] = _options.Model,
            ["messages"] = BuildMessages(context),
            ["stream"] = false,
        };

        if (availableActions.Count > 0)
        {
            payload["tools"] = BuildTools(availableActions);
        }

        using HttpRequestMessage request = new(HttpMethod.Post, _options.Endpoint)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.UserAgent.ParseAdd("Loren/0.1");
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new OllamaBrainException(
                $"Ollama returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        JsonNode? responseNode;
        try
        {
            responseNode = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new OllamaBrainException("Ollama returned invalid JSON.", exception);
        }

        JsonObject root = responseNode as JsonObject
            ?? throw new OllamaBrainException("Ollama returned an invalid response object.");
        JsonObject message = root["message"] as JsonObject
            ?? throw new OllamaBrainException("Ollama response did not contain a message object.");

        JsonArray? toolCalls = message["tool_calls"] as JsonArray;
        if (toolCalls is { Count: > 1 })
        {
            throw new OllamaBrainException(
                "Ollama returned parallel tool calls, but the current Loren runtime accepts one action per brain turn.");
        }

        if (toolCalls is { Count: 1 })
        {
            return BrainTurnResult.Request(ParseActionRequest(toolCalls[0]));
        }

        string? content = message["content"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new OllamaBrainException(
                "Ollama response contained neither one tool call nor final content.");
        }

        return BrainTurnResult.Final(content);
    }

    private static JsonArray BuildMessages(BrainContext context)
    {
        JsonArray messages = [];

        foreach (BrainInput input in context.Inputs)
        {
            switch (input)
            {
                case BrainMessage message:
                    messages.Add(new JsonObject
                    {
                        ["role"] = MapRole(message.Role),
                        ["content"] = message.Content,
                    });
                    break;

                case BrainActionObservation observation:
                    messages.Add(BuildAssistantToolCall(observation.Request));
                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_name"] = observation.Request.Name,
                        ["content"] = SerializeActionResult(observation.Result),
                    });
                    break;

                default:
                    throw new OllamaBrainException(
                        $"Unsupported brain input type '{input.GetType().Name}'.");
            }
        }

        return messages;
    }

    private static JsonObject BuildAssistantToolCall(ActionRequest request)
    {
        JsonObject arguments = [];
        foreach ((string key, string value) in request.Arguments)
        {
            arguments[key] = value;
        }

        return new JsonObject
        {
            ["role"] = "assistant",
            ["tool_calls"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = request.Name,
                        ["arguments"] = arguments,
                    },
                },
            },
        };
    }

    private static JsonArray BuildTools(IReadOnlyList<ActionDefinition> actions)
    {
        JsonArray tools = [];
        foreach (ActionDefinition action in actions)
        {
            JsonObject properties = [];
            JsonArray required = [];

            foreach (ActionParameterDefinition parameter in action.Parameters)
            {
                properties[parameter.Name] = new JsonObject
                {
                    ["type"] = MapParameterType(parameter.Type),
                    ["description"] = parameter.Description,
                };

                if (parameter.IsRequired)
                {
                    required.Add(parameter.Name);
                }
            }

            tools.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = action.Name,
                    ["description"] = action.Description,
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = properties,
                        ["required"] = required,
                    },
                },
            });
        }

        return tools;
    }

    private static ActionRequest ParseActionRequest(JsonNode? toolCallNode)
    {
        JsonObject toolCall = toolCallNode as JsonObject
            ?? throw new OllamaBrainException("Ollama returned an invalid tool call object.");
        JsonObject function = toolCall["function"] as JsonObject
            ?? throw new OllamaBrainException("Ollama tool call did not contain a function object.");
        string? name = function["name"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OllamaBrainException("Ollama tool call function name is missing.");
        }

        JsonObject argumentsObject = function["arguments"] as JsonObject
            ?? throw new OllamaBrainException("Ollama tool call arguments are missing or invalid.");
        Dictionary<string, string> arguments = new(StringComparer.Ordinal);
        foreach ((string key, JsonNode? value) in argumentsObject)
        {
            arguments[key] = ScalarToString(value, key);
        }

        return new ActionRequest(name, arguments);
    }

    private static string ScalarToString(JsonNode? value, string argumentName)
    {
        if (value is null)
        {
            throw new OllamaBrainException(
                $"Ollama tool argument '{argumentName}' cannot be null.");
        }

        if (value is not JsonValue jsonValue)
        {
            throw new OllamaBrainException(
                $"Ollama tool argument '{argumentName}' must be a scalar value.");
        }

        if (jsonValue.TryGetValue<string>(out string? stringValue))
        {
            return stringValue;
        }

        if (jsonValue.TryGetValue<bool>(out bool boolValue))
        {
            return boolValue ? "true" : "false";
        }

        if (jsonValue.TryGetValue<long>(out long integerValue))
        {
            return integerValue.ToString(CultureInfo.InvariantCulture);
        }

        if (jsonValue.TryGetValue<double>(out double numberValue))
        {
            return numberValue.ToString(CultureInfo.InvariantCulture);
        }

        throw new OllamaBrainException(
            $"Ollama tool argument '{argumentName}' has an unsupported scalar type.");
    }

    private static string SerializeActionResult(ActionResult result) =>
        JsonSerializer.Serialize(new
        {
            action_name = result.ActionName,
            success = result.Success,
            data = result.Data,
            error = result.Error,
        });

    private static string MapRole(BrainRole role) => role switch
    {
        BrainRole.System => "system",
        BrainRole.User => "user",
        BrainRole.Assistant => "assistant",
        _ => throw new OllamaBrainException($"Unsupported brain role '{role}'."),
    };

    private static string MapParameterType(ActionParameterType type) => type switch
    {
        ActionParameterType.Text => "string",
        ActionParameterType.WholeNumber => "integer",
        ActionParameterType.DecimalNumber => "number",
        ActionParameterType.Flag => "boolean",
        _ => throw new OllamaBrainException($"Unsupported action parameter type '{type}'."),
    };
}

public sealed class OllamaBrainOptions
{
    public OllamaBrainOptions(string model, Uri endpoint)
    {
        Model = model;
        Endpoint = endpoint;
        Validate();
    }

    public string Model { get; }

    public Uri Endpoint { get; }

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Model);
        ArgumentNullException.ThrowIfNull(Endpoint);

        if (!Endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("Ollama endpoint must be an absolute URI.", nameof(Endpoint));
        }

        if (Endpoint.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Ollama endpoint must use HTTP or HTTPS.", nameof(Endpoint));
        }
    }
}

public sealed class OllamaBrainException : InvalidOperationException
{
    public OllamaBrainException(string message)
        : base(message)
    {
    }

    public OllamaBrainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
