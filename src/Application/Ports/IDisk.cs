namespace Snail.MCP.Documents.Application.Ports;

/// <summary>The files of the machine as the use cases need them: where a path really leads, what is there, and its bytes.</summary>
/// <remarks>
/// One port for the whole file system, so that resolving a path and opening it live in one adapter: checked in one place
/// and opened in another, a path is two facts about the disk taken at two moments, and a link swapped in between is read
/// through. Every member may throw <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> for a file that
/// changed under it; the use cases answer those with their reason.
/// </remarks>
public interface IDisk
{
    /// <summary>The full path with every link on it resolved; for a path that does not exist, its nearest existing parent resolved and the rest kept.</summary>
    string Real(string path);

    /// <summary>Whether a file is at the path.</summary>
    bool IsFile(string path);

    /// <summary>Whether a folder is at the path.</summary>
    bool IsFolder(string path);

    /// <summary>The size and last write of the file at the path.</summary>
    FileStamp Stamp(string path);

    /// <summary>The bytes of the file at the path, in memory and positioned at the start.</summary>
    /// <exception cref="DocumentTooLargeException">The file grew past <paramref name="most"/> bytes while it was read.</exception>
    Task<Stream> ReadAsync(string path, int most, CancellationToken cancellationToken);
}
