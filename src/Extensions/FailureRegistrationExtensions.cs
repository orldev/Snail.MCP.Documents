using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Snail.MCP.Documents.Application.Prompts;
using Snail.MCP.Documents.Tools;

namespace Snail.MCP.Documents.Extensions;

/// <summary>A failure nobody foresaw, answered with its reason rather than with the protocol's empty apology.</summary>
public static partial class FailureRegistrationExtensions
{
    extension(IMcpServerBuilder builder)
    {
        /// <summary>Answers a call that failed inside the server with what failed, and logs it with its trace.</summary>
        /// <remarks>
        /// Every failure the server expects is an answer already; this catches the rest. Without it the SDK answers
        /// «An error occurred invoking …», which tells the model neither what went wrong nor whether to try again, and
        /// the trace behind it is written nowhere.
        /// </remarks>
        public IMcpServerBuilder ExplainFailures()
        {
            builder.WithRequestFilters(filters => filters.AddCallToolFilter(Explaining));

            return builder;
        }
    }

    private static McpRequestHandler<CallToolRequestParams, CallToolResult> Explaining(McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (context, cancellationToken) =>
        {
            try
            {
                return await next(context, cancellationToken);
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                string tool = context.Params?.Name ?? "the tool";

                if (context.Server.Services?.GetService<ILoggerFactory>() is { } loggers)
                    Failed(loggers.CreateLogger(typeof(FailureRegistrationExtensions)), failure, tool);

                return ToolResponse.Failure(Messages.Failed(tool, failure.GetType().Name, failure.Message), Messages.FailedHint);
            }
        };

    [LoggerMessage(Level = LogLevel.Error, Message = "{Tool} failed inside the server")]
    private static partial void Failed(ILogger logger, Exception failure, string tool);
}
