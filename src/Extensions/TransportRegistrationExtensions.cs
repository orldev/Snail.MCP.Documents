using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Tools;
using Snail.MCP.Documents.Tools.Prompts;
using Snail.Sdk.Mcp.Hosting;

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
        /// <summary>The server answering sessions at an address.</summary>
        public IServiceCollection AddMcpTransportAtAnAddress()
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<ISessionAddress, HttpAddress>();
            services.AddMcpTools().WithHttpTransport();

            return services;
        }

        /// <summary>The server speaking stdio to the process that started it.</summary>
        public IServiceCollection AddMcpTransportAsAProgram()
        {
            services.AddSingleton<ISessionAddress, StdioAddress>();
            services.AddMcpTools().WithStdioServerTransport();

            return services;
        }

        private IMcpServerBuilder AddMcpTools()
        {
            services.AddSingleton<SessionGrant>();

            return services
                .AddMcpServer(server =>
                {
                    server.ServerInfo = ServerIdentity.Of(typeof(TransportRegistrationExtensions).Assembly);
                    server.ServerInstructions = ServerGuidance.Instructions;
                })
                .RefuseWhatWasNotGranted()
                .ExplainFailures()
                .WithTools<DocumentTools>();
        }
    }
}
