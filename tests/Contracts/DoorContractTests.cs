using System.Net;
using Snail.MCP.Documents.Extensions;

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

    /// <summary>A server given the volume of working copies serves copies in it; a folder outside it is not a copy, whoever wrote the address.</summary>
    [Fact]
    public async Task Session_NamingAFolderOutsideTheConfiguredVolume_IsTurnedAwayAtTheDoor()
    {
        await using var server = await ListeningServer.StartAsync(("PORT", "0"), ("ROOTS__0", _volume.Root));
        using var client = new HttpClient();

        var answer = await client.PostAsync(new Uri($"{server.Address}{McpServerHost.Path}?root={_outside.Root}"), new StringContent("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.Contains("is outside", await answer.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>A page in a browser on the machine, or a name rebound to it, reaches a reader of files that has no token.</summary>
    [Fact]
    public async Task Request_CarryingABrowsersOrigin_IsForbidden()
    {
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{server.Address}{McpServerHost.Path}?root={_volume.Root}"))
        {
            Content = new StringContent("{}")
        };
        request.Headers.Add("Origin", "https://pages.example");

        var answer = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, answer.StatusCode);
    }

    /// <summary>A worker pointed at the server of another machine names a copy this machine does not have.</summary>
    [Fact]
    public async Task Session_NamingACopyThisMachineDoesNotHave_IsTurnedAwayAtTheDoor()
    {
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        using var client = new HttpClient();

        var answer = await client.PostAsync(new Uri($"{server.Address}{McpServerHost.Path}?root=/no/such/copy"), new StringContent("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.Contains("this machine has no /no/such/copy", await answer.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>A session naming no root at a server configured with none would be served nothing, so it hears that once, at the door.</summary>
    [Fact]
    public async Task Session_NamingNoRootAtAServerWithNone_IsTurnedAwayAtTheDoor()
    {
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        using var client = new HttpClient();

        var answer = await client.PostAsync(new Uri($"{server.Address}{McpServerHost.Path}"), new StringContent("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.StartsWith("this session named no root and this server is configured with none", await answer.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_OfARunningServer_AnswersOk()
    {
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        using var client = new HttpClient();

        var answer = await client.GetAsync(new Uri($"{server.Address}{McpServerHost.HealthPath}"));

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
    }
}
