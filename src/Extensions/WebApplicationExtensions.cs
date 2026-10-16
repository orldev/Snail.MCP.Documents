using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Extensions;

/// <summary>What a session must satisfy before the protocol is allowed to see it.</summary>
public static partial class WebApplicationExtensions
{
    /// <summary>Turns away a session this server cannot serve, before a session exists to serve it.</summary>
    public static WebApplication RefuseASessionItCannotServe(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments(McpServerHost.Path, StringComparison.Ordinal))
            {
                await next(context);
                return;
            }

            var named = context.Request.Query[SessionRoots.Parameter].Select(root => root ?? string.Empty).ToList();

            if (Refusal(app, named) is not { } refusal)
            {
                await next(context);
                return;
            }

            TurnedAway(app.Logger, refusal);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain; charset=utf-8";

            await context.Response.WriteAsync(refusal);
        });

        return app;
    }

    /// <summary>Why this session may not be opened, or null when it may.</summary>
    /// <remarks>
    /// Three refusals. A session naming no root at a server configured with none would be served nothing
    /// anyway, and hearing that once at the door beats hearing it from every call for the life of the step.
    /// <para>
    /// The second is what pays for the whole arrangement: a session naming a root this machine does not
    /// have. That is the worst thing that can go wrong once servers answer at addresses — discovery hands
    /// the worker of one machine the server of another, and nothing in a call shows it. The server answers,
    /// about a folder that is not the task's copy, and a wrong answer wearing the shape of a right one is
    /// far worse than an error.
    /// </para>
    /// <para>
    /// The third keeps a session inside the volume the server was given: a root that exists but lies outside every
    /// configured root is not a working copy, whoever wrote the address.
    /// </para>
    /// </remarks>
    private static string? Refusal(WebApplication app, IReadOnlyList<string> named)
    {
        var configured = app.Services.GetRequiredService<ServerConfig>().Roots;
        var disk = app.Services.GetRequiredService<IDisk>();

        if (named.All(string.IsNullOrWhiteSpace) && configured.Length == 0)
            return "this session named no root and this server is configured with none, so it can read nothing: "
                   + "open the session at /mcp?root=/path/to/the/copy";

        if (SessionRoots.Absent(named, disk) is { Count: > 0 } absent)
            return $"this machine has no {string.Join(", ", absent)}: the session named a working copy that is not on the host this server runs on";

        return SessionRoots.Outside(named, configured, disk) is { Count: > 0 } outside
            ? $"{string.Join(", ", outside)} is outside {string.Join(", ", configured)}, which is all this server serves: the session named a folder that is not a working copy"
            : null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A session was turned away: {Reason}")]
    private static partial void TurnedAway(ILogger logger, string reason);
}
