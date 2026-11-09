using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Extensions;

/// <summary>Composition root: lists the features to wire; how a format is read is known one level down.</summary>
public static class McpServerRegistrationExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>Configures the server to answer sessions at an address.</summary>
        public IHostApplicationBuilder ConfigureMcpServerAtAnAddress(ServerConfig config)
        {
            builder.Services
                .AddSingleton(config)
                .AddReading()
                .AddMcpTransportAtAnAddress();

            return builder;
        }

        /// <summary>Configures the server to speak stdio to the process that started it.</summary>
        public IHostApplicationBuilder ConfigureMcpServerAsAProgram(ServerConfig config)
        {
            builder.Services
                .AddSingleton(config)
                .AddReading()
                .AddMcpTransportAsAProgram();

            return builder;
        }
    }
}
