using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Documents.Configuration;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Http;

namespace Snail.MCP.Documents.Extensions;

/// <summary>The server as a running host: listening at an address when it was given a port, a child process otherwise.</summary>
public static class McpServerHost
{
    /// <summary>Where HTTP listens when <c>Transport</c> says Http and neither <c>Url</c> nor <c>Port</c> names another: the image's port, on loopback.</summary>
    private static readonly Uri DefaultUrl = new("http://127.0.0.1:8823/");

    /// <summary>Builds whichever of the two shapes the configuration asks for.</summary>
    public static IHost Of(string[] args)
    {
        var settings = ServerConfigExtensions.Settings();
        var config = ServerConfigExtensions.Read(settings);
        var endpoint = Endpoint(settings);

        return endpoint.Transport is McpTransport.Http ? Listening(args, endpoint, settings, config) : Spoken(args, settings, config);
    }

    /// <summary>Where the settings say the server answers: a <c>Port</c> means HTTP on it, nothing means stdio.</summary>
    /// <remarks>
    /// Inside a container — the image — on every interface, since the callers are the workers of the machine, each in a
    /// container of its own, and a published port does not reach a container's loopback. Anywhere else on loopback alone
    /// (<see cref="McpReach.LoopbackOrContainer"/>): there is no token, so the network is the whole of the access control,
    /// and an address beyond loopback is refused at start rather than opened. The keys are read from the
    /// <see cref="ServerConfig.SectionName"/> section of the settings file as well as from its root, as the rest of the
    /// server's settings are, and a refusal names the server's variable even where the entry assembly is not the server's.
    /// </remarks>
    internal static McpEndpoint Endpoint(IConfiguration settings) =>
        McpEndpoint.Read(settings, DefaultUrl, new McpEndpointOptions { FallbackSection = ServerConfig.SectionName, Reach = McpReach.LoopbackOrContainer, Server = ServerConfigExtensions.Name });

    /// <summary>The server answering sessions at the endpoint's address.</summary>
    /// <remarks>
    /// There is no token: the network the server stands in, and the root each session names inside the configured volume,
    /// are the whole of its access control. A session naming a root this machine does not have or the volume does not hold
    /// is turned away before it exists, and every request a browser makes is refused: a page open on the machine, or a name
    /// rebound to it, would otherwise reach a reader of files with no token.
    /// <para>
    /// The host is built over the settings already read, so they are read once and the log levels they name are the
    /// host's; Kestrel is given the endpoint's address directly.
    /// </para>
    /// </remarks>
    private static WebApplication Listening(string[] args, McpEndpoint endpoint, IConfiguration settings, ServerConfig config)
    {
        var builder = McpHttpHost.CreateBuilder(args, endpoint, settings);

        builder.ConfigureMcpServerAtAnAddress(config);

        var app = builder.Build();

        app.UseSessionRoots(TransportRegistrationExtensions.Volumes);
        app.MapLocalMcp();
        app.MapHealth();

        return app;
    }

    private static IHost Spoken(string[] args, IConfiguration settings, ServerConfig config)
    {
        var builder = McpHost.CreateBuilder(args, settings);

        builder.ConfigureMcpServerAsAProgram(config);

        return builder.Build();
    }
}
