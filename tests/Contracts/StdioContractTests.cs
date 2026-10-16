using ModelContextProtocol.Client;
using Snail.MCP.Documents.Tools.Prompts;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Contracts;

/// <summary>The server as a program: the real process, started the way a client starts it.</summary>
/// <remarks>
/// Only a process shows the one thing stdio can get wrong and nothing in-process can — a log line on stdout,
/// which is a broken JSON-RPC stream to the client and an unexplained hang to the person behind it.
/// </remarks>
public sealed class StdioContractTests : IDisposable
{
    private readonly Folder _copy = new();

    public void Dispose() => _copy.Dispose();

    [Fact]
    public async Task Server_AsAProgram_AnnouncesItselfAndListsItsTools()
    {
        await using var client = await StartedAsync();

        var listed = await client.ListToolsAsync();

        Assert.Equal("s3l-mcp-documents", client.ServerInfo.Name);
        Assert.Equal(ServerGuidance.Instructions, client.ServerInstructions);
        Assert.Equal(Catalog.Names, [.. listed.Select(tool => tool.Name).Order(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// The guard rules of every Snail server, on the list a model receives: names in snake case and listed once, the tool
    /// and every parameter described, read-only or destructive stated, every allowed value named, English.
    /// </summary>
    [Fact]
    public async Task Tools_AsListed_KeepTheGuardRules()
    {
        await using var client = await StartedAsync();

        var listed = await client.ListToolsAsync();

        Assert.Empty(ToolGuards.Problems(listed.Select(tool => tool.ProtocolTool)));
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

    /// <summary>Every format through the program itself: a native build fails at the call that needs a type it was not given.</summary>
    [Theory]
    [InlineData("report.docx")]
    [InlineData("book.xlsx")]
    [InlineData("deck.pptx")]
    [InlineData("scan.pdf")]
    [InlineData("rows.csv")]
    public async Task EveryFormat_AsAProgram_IsAnsweredByEveryTool(string name)
    {
        _copy.Write(name, Path.GetExtension(name) switch
        {
            ".docx" => Samples.Word(),
            ".xlsx" => Samples.Excel(),
            ".pptx" => Samples.Slides(),
            ".pdf" => Samples.Pdf(),
            _ => Samples.Csv()
        });
        await using var client = await StartedAsync();

        foreach (var tool in new[] { "documents_read", "documents_outline", "documents_table" })
        {
            var answer = await client.CallToolAsync(tool, new Dictionary<string, object?> { ["path"] = name });

            Assert.NotEmpty(answer.Content);
        }

        var read = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = name });

        Assert.NotEqual(true, read.IsError);
        Assert.False(string.IsNullOrWhiteSpace(read.Data()["text"]?.GetValue<string>()), name);
    }

    /// <summary>
    /// A stack overflow is not an exception: the process ends, and with it every session of the machine. The HTML reader
    /// once walked the tree by recursion, and three thousand nested divs — fifteen kilobytes — were enough.
    /// </summary>
    [Fact]
    public async Task Read_PageNestedFiveThousandDeep_LeavesTheServerAnswering()
    {
        _copy.Write("deep.html", $"<html><body>{string.Concat(Enumerable.Repeat("<div>", 5_000))}x</body></html>");
        await using var client = await StartedAsync();

        var answer = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = "deep.html" });
        var listed = await client.ListToolsAsync();

        Assert.NotEqual(true, answer.IsError);
        Assert.NotEmpty(listed);
    }

    private Task<McpClient> StartedAsync()
    {
        var (command, arguments) = ServerProgram.Command("Snail.MCP.Documents", "SNAIL_DOCUMENTS_SERVER");

        return McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = command,
            Arguments = arguments,
            EnvironmentVariables = Deployment.Variables(("ROOTS__0", _copy.Root), ("CONFIG", ServerProgram.NoSettingsFile)).ToDictionary(variable => variable.Key, variable => (string?)variable.Value)
        }));
    }
}
