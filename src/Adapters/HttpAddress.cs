using Microsoft.AspNetCore.Http;

namespace Snail.MCP.Documents.Adapters;

/// <summary>The address the session was opened at, read from the request being served.</summary>
/// <remarks>
/// Every request of one session is posted to the same URL, so the query is on all of them and reading
/// the current one answers about the session — with a session or without one, which matters because a
/// client of the sessionless protocol revision is served request by request.
/// </remarks>
public sealed class HttpAddress(IHttpContextAccessor requests) : ISessionAddress
{
    /// <inheritdoc/>
    public IReadOnlyList<string> Values(string parameter) =>
        requests.HttpContext is { } request
            ? [.. request.Request.Query[parameter].Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!)]
            : [];
}
