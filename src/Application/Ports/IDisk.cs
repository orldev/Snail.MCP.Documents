namespace Snail.MCP.Documents.Application.Ports;

/// <summary>The files of the machine as the use cases need them: what is there, and its bytes.</summary>
/// <remarks>
/// The path arrives with every link on it followed already (<c>SessionRoots.Reach</c> of Snail.Sdk.Mcp.Hosting), and
/// checked there and opened here it is two facts about the disk taken at two moments: <see cref="ReadAsync"/> refuses a
/// link anywhere on it, so one swapped in between is not read through. Every member may throw <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> for a file that
/// changed under it; the use cases answer those with their reason.
/// </remarks>
public interface IDisk
{
    /// <summary>The size and last write of the file at the path.</summary>
    FileStamp Stamp(string path);

    /// <summary>The bytes of the file at the path, in memory and positioned at the start.</summary>
    /// <exception cref="DocumentTooLargeException">The file grew past <paramref name="most"/> bytes while it was read.</exception>
    Task<Stream> ReadAsync(string path, int most, CancellationToken cancellationToken);
}
