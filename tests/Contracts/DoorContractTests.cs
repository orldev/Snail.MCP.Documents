using System.Net;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Contracts;

/// <summary>Who the server turns away before a session exists, as the platform meets it at an address.</summary>
public sealed class DoorContractTests : IDisposable
{
    private readonly Folder _volume = new();

    private readonly Folder _outside = new();

    public void Dispose()
    {
        _volume.Dispose();
        _outside.Dispose();
    }

    /// <summary>A setting out of its range stops the program before it listens: one line naming the variable, exit code 1, no trace.</summary>
    [Fact]
    public async Task Start_WithASettingOutOfRange_StopsInOneLineNamingTheVariable()
    {
        var refused = await Assert.ThrowsAsync<ServerExitedException>(() => Served.StartAsync(("MAX_FILE_BYTES", "10")));

        Assert.Equal(1, refused.ExitCode);
        Assert.Single(refused.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Assert.Contains("SNAIL_MCP_DOCUMENTS_MAX_FILE_BYTES (or MaxFileBytes in the settings file)", refused.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", refused.Stderr, StringComparison.Ordinal);
    }

    /// <summary>
    /// A setting that is not even of its type stops the program before it listens: one line naming the variable, exit
    /// code 1, no trace. The settings are bound before there is a host, and that binding refuses instead of crashing with 134.
    /// </summary>
    [Fact]
    public async Task Start_WithASettingThatIsNoNumber_StopsInOneLineNamingTheVariable()
    {
        var refused = await Assert.ThrowsAsync<ServerExitedException>(() => Served.StartAsync(("MAX_FILE_BYTES", "abc")));

        Assert.Equal(1, refused.ExitCode);
        Assert.Single(refused.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Assert.Contains("SNAIL_MCP_DOCUMENTS_MAX_FILE_BYTES", refused.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", refused.Stderr, StringComparison.Ordinal);
    }

    /// <summary>A server given the volume of working copies serves copies in it; a folder outside it is not a copy, whoever wrote the address.</summary>
    [Fact]
    public async Task Session_NamingAFolderOutsideTheConfiguredVolume_IsTurnedAwayAtTheDoor()
    {
        await using var server = await Served.StartAsync(("ROOTS__0", _volume.Root));

        var answer = await PostAsync(server, $"root={_outside.Root}");

        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.Contains("is not one of them", await answer.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>A page in a browser on the machine, or a name rebound to it, reaches a reader of files that has no token.</summary>
    [Fact]
    public async Task Request_CarryingABrowsersOrigin_IsForbidden()
    {
        await using var server = await Served.StartAsync(("ROOTS__0", _volume.Root));

        Assert.Empty(await HttpGuards.ProblemsAsync(server.Endpoint));
    }

    /// <summary>A worker pointed at the server of another machine names a copy this machine does not have.</summary>
    /// <remarks>The copy is named as this system writes an absolute path: on Windows <c>/no/such/copy</c> is a folder of the current drive.</remarks>
    [Fact]
    public async Task Session_NamingACopyThisMachineDoesNotHave_IsTurnedAwayAtTheDoor()
    {
        string missing = Path.GetFullPath("/no/such/copy");
        await using var server = await Served.StartAsync();

        var answer = await PostAsync(server, $"root={Uri.EscapeDataString(missing)}");

        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.Contains($"this machine has no {missing}", await answer.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A session naming no root at a server configured with none would be served nothing, so it hears that once, at the door
    /// — as "this request", since the transport is stateless and every request carries its own address.
    /// </summary>
    [Fact]
    public async Task Session_NamingNoRootAtAServerWithNone_IsTurnedAwayAtTheDoor()
    {
        await using var server = await Served.StartAsync();

        var answer = await PostAsync(server, null);

        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.StartsWith("this request named no root and this server is configured with none", await answer.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_OfARunningServer_AnswersOk()
    {
        await using var server = await Served.StartAsync();
        using var client = new HttpClient();

        var answer = await client.GetAsync(new Uri(server.Address, "healthz"));

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpServerProgram server, string? query)
    {
        using var client = new HttpClient();
        using var content = new StringContent("{}");

        return await client.PostAsync(query is null ? server.Endpoint : new UriBuilder(server.Endpoint) { Query = query }.Uri, content);
    }
}
