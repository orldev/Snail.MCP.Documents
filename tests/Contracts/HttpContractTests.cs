using Snail.MCP.Documents.Tools.Prompts;
using Snail.Sdk.Mcp.Hosting;

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
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        await using var client = await server.ConnectAsync($"?root={_copy.Root}");

        var listed = await client.ListToolsAsync();

        Assert.Equal(ServerIdentity.VersionOf(typeof(ServerGuidance).Assembly), client.ServerInfo.Version);
        Assert.Equal(ServerGuidance.Instructions, client.ServerInstructions);
        Assert.Equal(Catalog.Names, [.. listed.Select(tool => tool.Name).Order(StringComparer.Ordinal)]);
    }

    [Fact]
    public async Task Read_InASessionNamingItsCopy_ReadsTheCopysAttachment()
    {
        _copy.Write(".swarm/attachments/report.docx", Samples.Word());
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        await using var client = await server.ConnectAsync($"?root={_copy.Root}");

        var answer = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = ".swarm/attachments/report.docx" });

        Assert.NotEqual(true, answer.IsError);
        Assert.StartsWith("# Отчёт за неделю", answer.Data()["text"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>One server serves every task of the machine: a session reaches its own copy and nobody else's.</summary>
    [Fact]
    public async Task Read_OfAnotherTasksCopy_IsRefusedAsAToolError()
    {
        string secret = _other.Write("secret.txt", "чужое");
        await using var server = await ListeningServer.StartAsync(("PORT", "0"), ("ROOTS__0", Path.GetTempPath()));
        await using var client = await server.ConnectAsync($"?root={_copy.Root}");

        var refused = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = secret });

        Assert.Equal(true, refused.IsError);
        Assert.Contains("outside this session", refused.Text(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Call_ToAToolTheSessionWasNotGranted_IsRefusedAtTheDoor()
    {
        _copy.Write("a.txt", "x");
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        await using var client = await server.ConnectAsync($"?root={_copy.Root}&tool=documents_outline");

        var refused = await client.CallToolAsync("documents_read", new Dictionary<string, object?> { ["path"] = "a.txt" });

        Assert.Equal(true, refused.IsError);
        Assert.Contains("was not granted", refused.Text(), StringComparison.Ordinal);
    }

    /// <summary>The text is paid for once: a structured copy beside it would hand a model every piece twice.</summary>
    [Fact]
    public async Task Answer_OfAnyTool_CarriesItsPayloadOnce()
    {
        _copy.Write("a.txt", "x");
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        await using var client = await server.ConnectAsync($"?root={_copy.Root}");

        var answer = await client.CallToolAsync("documents_outline", new Dictionary<string, object?> { ["path"] = "a.txt" });

        Assert.Single(answer.Content);
        Assert.Null(answer.StructuredContent);
    }
}
