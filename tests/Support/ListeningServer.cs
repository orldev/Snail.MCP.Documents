using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Extensions;

namespace Snail.MCP.Documents.Tests.Support;

/// <summary>The real server answering at an address, on a port the machine picked.</summary>
internal sealed class ListeningServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ListeningServer(WebApplication app) => _app = app;

    public IServiceProvider Services => _app.Services;

    public static async Task<ListeningServer> StartAsync(params (string Name, string Value)[] own)
    {
        var settings = Deployment.Settings(own);
        var app = McpServerHost.Listening([], ServerConfigExtensions.Read(settings), settings);

        await app.StartAsync();

        return new ListeningServer(app);
    }

    /// <summary>The server listens on every interface, and «*» is not an address a client can call.</summary>
    public string Address
    {
        get
        {
            string bound = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

            return $"http://127.0.0.1:{new Uri(bound.Replace("://*:", "://localhost:", StringComparison.Ordinal)).Port}";
        }
    }

    public Task<McpClient> ConnectAsync(string query = "") =>
        McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{Address}{McpServerHost.Path}{query}")
        }));

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
