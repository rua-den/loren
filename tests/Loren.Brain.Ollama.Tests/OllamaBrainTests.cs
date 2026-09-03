using System.Net;
using System.Text;
using System.Text.Json;
using Loren.Brain.Ollama;
using Loren.Core.Actions;
using Loren.Core.Brains;
using Xunit;

namespace Loren.Brain.Ollama.Tests;

public sealed class OllamaBrainTests
{
    private static readonly ActionDefinition ReadRepository = new(
        "github.read_repository",
        "Read repository metadata.",
        true,
        [
            new("owner", "Repository owner.", ActionParameterType.String, true),
            new("repository", "Repository name.", ActionParameterType.String, true),
        ]);

    [Fact]
    public async Task TranslatesLorenActionSchemaAndParsesOneToolCall()
    {
        const string apiKey = "test-secret-that-must-not-enter-body";
        DelegateHandler handler = new(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://ollama.example/api/chat", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal(apiKey, request.Headers.Authorization?.Parameter);

            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Assert.DoesNotContain(apiKey, body, StringComparison.Ordinal);

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            Assert.Equal("test-model", root.GetProperty("model").GetString());
            Assert.False(root.GetProperty("stream").GetBoolean());

            JsonElement function = root
                .GetProperty("tools")[0]
                .GetProperty("function");
            Assert.Equal("github.read_repository", function.GetProperty("name").GetString());
            JsonElement parameters = function.GetProperty("parameters");
            Assert.Equal("string", parameters.GetProperty("properties").GetProperty("owner").GetProperty("type").GetString());
            Assert.Contains(
                parameters.GetProperty("required").EnumerateArray(),
                value => value.GetString() == "repository");

            return JsonResponse("""
                {
                  "message": {
                    "role": "assistant",
                    "content": "",
                    "tool_calls": [
                      {
                        "type": "function",
                        "function": {
                          "name": "github.read_repository",
                          "arguments": {
                            "owner": "rua-den",
                            "repository": "loren"
                          }
                        }
                      }
                    ]
                  },
                  "done": true
                }
                """);
        });

        OllamaBrain brain = CreateBrain(handler, apiKey);
        BrainTurnResult result = await brain.ThinkAsync(
            BrainContext.FromUser("Check rua-den/loren."),
            [ReadRepository],
            CancellationToken.None);

        Assert.False(result.IsFinal);
        Assert.NotNull(result.ActionRequest);
        Assert.Equal("github.read_repository", result.ActionRequest.Name);
        Assert.Equal("rua-den", result.ActionRequest.Arguments["owner"]);
        Assert.Equal("loren", result.ActionRequest.Arguments["repository"]);
    }

    [Fact]
    public async Task ReconstructsToolObservationAndParsesFinalAnswer()
    {
        ActionRequest actionRequest = new(
            "github.read_repository",
            new Dictionary<string, string>
            {
                ["owner"] = "rua-den",
                ["repository"] = "loren",
            });
        ActionResult actionResult = new(
            actionRequest.Name,
            true,
            new Dictionary<string, string>
            {
                ["full_name"] = "rua-den/loren",
                ["default_branch"] = "main",
            });
        BrainContext context = BrainContext
            .FromUser("Check rua-den/loren.")
            .Append(new BrainActionObservation(actionRequest, actionResult));

        DelegateHandler handler = new(async (request, cancellationToken) =>
        {
            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement messages = document.RootElement.GetProperty("messages");
            Assert.Equal(3, messages.GetArrayLength());
            Assert.Equal("user", messages[0].GetProperty("role").GetString());
            Assert.Equal("assistant", messages[1].GetProperty("role").GetString());
            Assert.Equal(
                "github.read_repository",
                messages[1].GetProperty("tool_calls")[0].GetProperty("function").GetProperty("name").GetString());
            Assert.Equal("tool", messages[2].GetProperty("role").GetString());
            Assert.Equal("github.read_repository", messages[2].GetProperty("tool_name").GetString());
            Assert.Contains("rua-den/loren", messages[2].GetProperty("content").GetString(), StringComparison.Ordinal);

            return JsonResponse("""
                {
                  "message": {
                    "role": "assistant",
                    "content": "Repository rua-den/loren uses main."
                  },
                  "done": true
                }
                """);
        });

        OllamaBrain brain = CreateBrain(handler);
        BrainTurnResult result = await brain.ThinkAsync(
            context,
            [ReadRepository],
            CancellationToken.None);

        Assert.True(result.IsFinal);
        Assert.Equal("Repository rua-den/loren uses main.", result.FinalOutput);
    }

    [Fact]
    public async Task RejectsParallelToolCallsUntilRuntimeSupportsThem()
    {
        DelegateHandler handler = new((_, _) => Task.FromResult(JsonResponse("""
            {
              "message": {
                "role": "assistant",
                "content": "",
                "tool_calls": [
                  { "function": { "name": "one", "arguments": {} } },
                  { "function": { "name": "two", "arguments": {} } }
                ]
              },
              "done": true
            }
            """)));
        OllamaBrain brain = CreateBrain(handler);

        OllamaBrainException exception = await Assert.ThrowsAsync<OllamaBrainException>(() =>
            brain.ThinkAsync(
                BrainContext.FromUser("parallel"),
                [ReadRepository],
                CancellationToken.None));

        Assert.Contains("parallel tool calls", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PropagatesCancellationAtProviderAwait()
    {
        DelegateHandler handler = new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        });
        OllamaBrain brain = CreateBrain(handler);
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            brain.ThinkAsync(
                BrainContext.FromUser("cancel"),
                [ReadRepository],
                cancellation.Token));
    }

    [Fact]
    public async Task ProviderFailureDoesNotExposeResponseBody()
    {
        const string providerBody = "provider-internal-sensitive-text";
        DelegateHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent(providerBody, Encoding.UTF8, "text/plain"),
        }));
        OllamaBrain brain = CreateBrain(handler);

        OllamaBrainException exception = await Assert.ThrowsAsync<OllamaBrainException>(() =>
            brain.ThinkAsync(
                BrainContext.FromUser("fail"),
                [ReadRepository],
                CancellationToken.None));

        Assert.DoesNotContain(providerBody, exception.Message, StringComparison.Ordinal);
        Assert.Contains("502", exception.Message, StringComparison.Ordinal);
    }

    private static OllamaBrain CreateBrain(HttpMessageHandler handler, string? apiKey = null)
    {
        HttpClient httpClient = new(handler);
        OllamaBrainOptions options = new(
            "test-model",
            new Uri("https://ollama.example/api/chat"),
            apiKey);
        return new OllamaBrain(httpClient, options);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => callback(request, cancellationToken);
    }
}
