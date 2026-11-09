using Snail.MCP.Documents.Tools.Prompts;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Hosting.Sessions;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Contracts;

/// <summary>The server at an address, as the platform meets it: one server for the machine, each session confined to its copy.</summary>
public sealed class HttpContractTests : IDisposable
{
    private readonly Folder _copy = new();

    private readonly Folder _other = new();

    public void Dispose()
    {
        _copy.Dispose();
        _other.Dispose();
    }

    [Fact]
    public async Task Server_AtAnAddress_AnnouncesItselfAndListsExactlyTheToolsTheAssemblyDeclares()
    {
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}");

        var listed = await client.ListToolsAsync();

        Assert.NotNull(client.ServerInfo);
        Assert.Equal(ServerIdentity.VersionOf(typeof(ServerGuidance).Assembly), client.ServerInfo.Version);
        Assert.Equal(ServerGuidance.Instructions, client.ServerInstructions);
        Assert.Equal(Catalog.Names, [.. listed.Select(tool => tool.Name).Order(StringComparer.Ordinal)]);
    }

    /// <summary>The guard rules of every Snail server, on the list a session at an address receives.</summary>
    [Fact]
    public async Task Tools_OverHttp_KeepTheGuardRules()
    {
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}");

        var listed = await client.ListToolsAsync();

        Assert.Empty(ToolGuards.Problems(listed.Select(tool => tool.ProtocolTool)));
    }

    [Fact]
    public async Task Read_InASessionNamingItsCopy_ReadsTheCopysAttachment()
    {
        _copy.Write(".swarm/attachments/report.docx", Samples.Word());
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}");

        var answer = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = ".swarm/attachments/report.docx" });

        Assert.True(answer.IsOk(), answer.Text());
        Assert.StartsWith("# Отчёт за неделю", answer.Data()!["text"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>One server serves every task of the machine: a session reaches its own copy and nobody else's.</summary>
    [Fact]
    public async Task Read_OfAnotherTasksCopy_IsRefusedAsAToolError()
    {
        string secret = _other.Write("secret.txt", "чужое");
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}");

        var refused = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = secret });

        Assert.Equal(true, refused.IsError);
        Assert.Equal($"'{secret}' is outside this request's root {_copy.Root}, which its address names", refused.Error());
        Assert.Equal(Reached.Outside, refused.Type());
    }

    /// <summary>A role given some of the tools sees those alone: a tool left out of the address is not listed, and a call to it is refused.</summary>
    [Fact]
    public async Task Call_ToAToolTheSessionWasNotGranted_IsHiddenAndRefusedAtTheDoor()
    {
        _copy.Write("a.txt", "x");
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}&tool=documents_outline");

        var listed = await client.ListToolsAsync();
        var refused = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = "a.txt" });

        Assert.Equal(["documents_outline"], listed.Select(tool => tool.Name));
        Assert.Equal(true, refused.IsError);
        Assert.Contains("'documents_read' was not granted", refused.Error(), StringComparison.Ordinal);
        Assert.NotNull(refused.Hint());
    }

    /// <summary>A failure nobody foresaw names itself; the protocol's own «an error occurred» tells the model nothing to act on.</summary>
    [Fact]
    public async Task Call_WithAnArgumentOfTheWrongType_IsAnsweredWithWhatFailed()
    {
        _copy.Write("a.txt", "x");
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}");

        var answer = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = "a.txt", ["from"] = "the start" });

        Assert.Equal(true, answer.IsError);
        Assert.False(answer.IsOk());
        Assert.DoesNotContain("An error occurred", answer.Error(), StringComparison.Ordinal);
        Assert.NotNull(answer.Hint());
    }

    /// <summary>A parameter the tool does not take is refused by name, not dropped: the piece read from the start would look like the one asked for.</summary>
    [Fact]
    public async Task Call_WithAParameterTheToolDoesNotTake_IsRefusedNamingIt()
    {
        _copy.Write("a.txt", "x");
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}");

        var refused = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = "a.txt", ["offset"] = 1 });

        Assert.Equal(true, refused.IsError);
        Assert.Equal(ToolFailures.Arguments, refused.Type());
        Assert.Contains("'offset'", refused.Error(), StringComparison.Ordinal);
    }

    /// <summary>The text is paid for once: a structured copy beside it would hand a model every piece twice.</summary>
    [Fact]
    public async Task Answer_OfAnyTool_CarriesItsPayloadOnce()
    {
        _copy.Write("a.txt", "x");
        await using var server = await Served.StartAsync(("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.SessionAsync($"root={_copy.Root}");

        var answer = await client.CallToolAsync("documents_outline", new Dictionary<string, object?> { ["path"] = "a.txt" });

        Assert.Single(answer.Content);
        Assert.Null(answer.StructuredContent);
    }
}
