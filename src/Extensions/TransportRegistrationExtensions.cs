using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Documents.Application.Prompts;
using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Tools;
using Snail.MCP.Documents.Tools.Prompts;
using Snail.Sdk.Mcp.Hosting;
using Snail.Sdk.Mcp.Hosting.Discovery;
using Snail.Sdk.Mcp.Hosting.Sessions;
using Snail.Sdk.Mcp.Http;

namespace Snail.MCP.Documents.Extensions;

/// <summary>Driving adapter: the tools reach the model over the transport the deployment asked for.</summary>
/// <remarks>
/// Which transport is wanted is said by calling for it, never inferred inside, so the two differ in one thing
/// only: at an address a session carries its root and its grant in its URL, as a program there is no URL and
/// the configured roots and every tool are the caller's.
/// </remarks>
public static class TransportRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>The server answering sessions at an address, which names the session's roots and tools.</summary>
        public IServiceCollection AddMcpTransportAtAnAddress()
        {
            services.AddMcpTools().WithHttpTransport().WithSessionAddress();

            return services;
        }

        /// <summary>The server speaking stdio to the process that started it.</summary>
        public IServiceCollection AddMcpTransportAsAProgram()
        {
            services.AddMcpTools().WithStdioServerTransport();

            return services;
        }

        /// <remarks>
        /// The grant is held where calls arrive rather than inside each tool, and a tool the session was not granted is
        /// left out of its list. A call that sends a parameter its tool does not take, or leaves out one it needs, is
        /// refused naming it, rather than the parameter dropped without a word. The roots are the ones its address names
        /// inside the configured volume, and a session with none is told which variable configures it. A failure nobody
        /// foresaw is worded by <see cref="Explained"/>.
        /// </remarks>
        private IMcpServerBuilder AddMcpTools() =>
            services
                .AddMcpServer(server =>
                {
                    server.ServerInfo = ServerIdentity.Of(typeof(TransportRegistrationExtensions).Assembly);
                    server.ServerInstructions = ServerGuidance.Instructions;
                })
                .ExplainFailures(Explained)
                .WithSessionGrant()
                .WithArgumentCheck()
                .WithSessionRoots(Volumes, roots => roots.RootsSetting = RootsSetting)
                .WithTools<DocumentTools>();
    }

    /// <summary>A failure nobody foresaw, named as the server names it: the tool, the kind of failure and its message.</summary>
    /// <remarks>
    /// Still <c>internal</c>, and logged with its trace, but not "try once more": a document that broke a reader breaks it
    /// the same way the next time, so the model is told the fault is the server's and to report the file instead of
    /// repeating the call, while the other documents still read.
    /// </remarks>
    private static void Explained(FailureOptions failures)
    {
        failures.Error = static (tool, failure) => Messages.Failed(tool, failure.GetType().Name, failure.Message);
        failures.Hint = static (_, _) => Messages.FailedHint;
    }

    /// <summary>The variable that configures the volume, named to a session that was given no root.</summary>
    /// <remarks>
    /// With its index: <c>Roots</c> is a list, and a plain <c>SNAIL_MCP_DOCUMENTS_ROOTS=/path</c> binds to none of it, so a
    /// hint naming the bare name sent the operator to a setting that leaves the server configured with nothing.
    /// </remarks>
    internal const string RootsSetting = "SNAIL_MCP_DOCUMENTS_ROOTS__0";

    /// <summary>The volume the server was configured to serve: the roots a session may name a copy inside, and the ones it reads when it names none.</summary>
    internal static IReadOnlyList<string> Volumes(IServiceProvider services) => services.GetRequiredService<ServerConfig>().Roots;
}
