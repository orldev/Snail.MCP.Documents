using ModelContextProtocol.Protocol;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Support;

/// <summary>Reads a tool result the way a model does: the JSON inside its text.</summary>
internal static class ToolResults
{
    public static JsonNode Data(this CallToolResult result) =>
        JsonNode.Parse(result.Text())!["data"]!;
}
