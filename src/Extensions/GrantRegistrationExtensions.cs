using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using Snail.MCP.Documents.Application;

namespace Snail.MCP.Documents.Extensions;

/// <summary>The grant of a session, checked where calls arrive rather than inside each tool.</summary>
public static class GrantRegistrationExtensions
{
    extension(IMcpServerBuilder builder)
    {
        /// <summary>Refuses a tool the session was not granted, whoever forwarded the call.</summary>
        /// <remarks>
        /// At the door, and not tool by tool: a defence that lives in the tools holds only until somebody
        /// adds one that forwards calls. Here every call passes the same check.
        /// </remarks>
        public IMcpServerBuilder RefuseWhatWasNotGranted()
        {
            builder.WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var grant = context.Server.Services?.GetService<SessionGrant>();

                if (context.Params?.Name is not { } name || grant is null || grant.Allows(name))
                    return await next(context, cancellationToken);

                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = grant.Refusal(name) }]
                };
            }));

            return builder;
        }
    }
}
