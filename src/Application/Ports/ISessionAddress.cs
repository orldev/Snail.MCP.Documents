namespace Snail.MCP.Documents.Application.Ports;

/// <summary>What the address of the running session says.</summary>
/// <remarks>
/// A server reached over HTTP is opened at a URL, and the URL is the one place a caller can state
/// something about its whole session rather than about one call. The platform writes it and the model
/// cannot reach it, which is why the grant of a session travels there: <c>…/mcp?tool=documents_read</c>.
/// A server speaking stdio has no address and answers nothing here.
/// </remarks>
public interface ISessionAddress
{
    /// <summary>Every value the address gives that parameter, empty when there is no address at all.</summary>
    IReadOnlyList<string> Values(string parameter);
}
