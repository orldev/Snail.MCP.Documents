using System.Net;
using Snail.MCP.Documents.Extensions;

namespace Snail.MCP.Documents.Tests.Contracts;

/// <summary>Who the server turns away before a session exists, as the platform meets it at an address.</summary>
public sealed class DoorContractTests
{
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

    [Fact]
    public async Task Health_OfARunningServer_AnswersOk()
    {
        await using var server = await ListeningServer.StartAsync(("PORT", "0"));
        using var client = new HttpClient();

        var answer = await client.GetAsync(new Uri($"{server.Address}{McpServerHost.HealthPath}"));

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
    }
}
