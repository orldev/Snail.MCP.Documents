namespace Snail.MCP.Documents.Adapters;

/// <summary>A server started as a program has no address, and says so by answering nothing.</summary>
public sealed class StdioAddress : ISessionAddress
{
    /// <inheritdoc/>
    public IReadOnlyList<string> Values(string parameter) => [];
}
