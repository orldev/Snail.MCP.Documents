using ModelContextProtocol.Client;
using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Tools.Prompts;

namespace Snail.MCP.Documents.Tests.Contracts;

/// <summary>The server as a program: the real process, started the way a client starts it.</summary>
/// <remarks>
/// Only a process shows the one thing stdio can get wrong and nothing in-process can — a log line on stdout,
/// which is a broken JSON-RPC stream to the client and an unexplained hang to the person behind it.
/// </remarks>
public sealed class StdioContractTests : IDisposable
{
    private readonly Folder _copy = new();

    private static string Server => Path.Combine(AppContext.BaseDirectory, "Snail.MCP.Documents.dll");

    public void Dispose() => _copy.Dispose();

    [Fact]
    public async Task Server_AsAProgram_AnnouncesItselfAndListsItsTools()
    {
        await using var client = await StartedAsync();

        var listed = await client.ListToolsAsync();

        Assert.Equal(ThisAssembly.Name, client.ServerInfo.Name);
        Assert.Equal(ServerGuidance.Instructions, client.ServerInstructions);
        Assert.Equal(Catalog.Names, [.. listed.Select(tool => tool.Name).Order(StringComparer.Ordinal)]);
    }

    [Fact]
    public async Task Read_AsAProgram_ReadsAFileOfItsConfiguredRoot()
    {
        _copy.Write("a.md", "# Заголовок\n\nтекст");
        await using var client = await StartedAsync();

        var answer = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = "a.md" });

        Assert.NotEqual(true, answer.IsError);
        Assert.Equal("# Заголовок\n\nтекст", answer.Data()["text"]!.GetValue<string>());
    }

    private Task<McpClient> StartedAsync() =>
        McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = [Server],
            EnvironmentVariables = Deployment.Variables(("ROOTS__0", _copy.Root)).ToDictionary(variable => variable.Key, variable => (string?)variable.Value)
        }));
}
