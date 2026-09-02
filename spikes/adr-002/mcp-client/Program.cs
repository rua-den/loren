using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "Everything",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-everything"],
});

await using var client = await McpClient.CreateAsync(transport);

IList<McpClientTool> tools = await client.ListToolsAsync();
var normalized = tools
    .Select(tool => new LorenActionDefinition(tool.Name, tool.Description ?? string.Empty))
    .ToArray();

if (!normalized.Any(tool => tool.Name == "echo"))
{
    throw new InvalidOperationException("Reference MCP server did not expose the expected echo tool.");
}

CallToolResult result = await LorenMcpGateway.ExecuteReadOnlyAsync(
    client,
    "echo",
    new Dictionary<string, object?> { ["message"] = "Hello from Loren" },
    CancellationToken.None);

string? text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
if (!string.Equals(text, "Hello from Loren", StringComparison.Ordinal))
{
    throw new InvalidOperationException($"Unexpected MCP result: '{text}'.");
}

Console.WriteLine($"[spike] normalized-tools={normalized.Length}");
Console.WriteLine("[spike] PASS: MCP metadata normalized -> Loren gateway -> read-only MCP call -> structured result");

internal sealed record LorenActionDefinition(string Name, string Description);

internal static class LorenMcpGateway
{
    private static readonly HashSet<string> AllowedReadOnlyActions =
        new(StringComparer.Ordinal) { "echo" };

    public static Task<CallToolResult> ExecuteReadOnlyAsync(
        McpClient client,
        string action,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        if (!AllowedReadOnlyActions.Contains(action))
        {
            throw new InvalidOperationException($"MCP action '{action}' is not allowed by the M0 read-only gateway.");
        }

        return client.CallToolAsync(
            action,
            arguments.ToDictionary(pair => pair.Key, pair => pair.Value),
            cancellationToken: cancellationToken);
    }
}
