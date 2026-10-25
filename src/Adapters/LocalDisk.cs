using System.Buffers;

namespace Snail.MCP.Documents.Adapters;

/// <summary>The file system of the machine this server runs on.</summary>
public sealed class LocalDisk : IDisk
{
    private const int Chunk = 81_920;

    /// <inheritdoc/>
    /// <remarks>
    /// Every link on the path, not just the last one: <c>ResolveLinkTarget</c> answers about the final component
    /// alone, so a link in the middle — <c>/copy/out/secret.txt</c> where <c>out</c> leads elsewhere — resolves
    /// to itself and walks straight out of the root. It is the cheapest escape there is.
    /// </remarks>
    public string Real(string path)
    {
        string existing = path;
        Stack<string> tail = [];

        while (!Directory.Exists(existing) && !File.Exists(existing))
        {
            string? parent = Path.GetDirectoryName(existing);
            if (string.IsNullOrEmpty(parent) || parent == existing)
                return path;

            tail.Push(Path.GetFileName(existing));
            existing = parent;
        }

        string resolved = Resolved(existing);

        return tail.Count == 0 ? resolved : Path.Combine([resolved, .. tail]);
    }

    /// <inheritdoc/>
    public bool IsFile(string path) => File.Exists(path);

    /// <inheritdoc/>
    public bool IsFolder(string path) => Directory.Exists(path);

    /// <inheritdoc/>
    public FileStamp Stamp(string path)
    {
        var file = new FileInfo(path);

        return new FileStamp(file.Length, file.LastWriteTimeUtc);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Opened as <see cref="RegularFile"/> opens it: a link that appeared on the path since it was resolved, or a pipe,
    /// is refused rather than followed or waited on. Bounded while it reads, not only before: the size checked a moment
    /// ago is not the size of a log still being written. The buffer starts at the size the file has, so a file is copied
    /// into memory once rather than through a doubling buffer.
    /// </remarks>
    public async Task<Stream> ReadAsync(string path, int most, CancellationToken cancellationToken)
    {
        await using var file = RegularFile.Opened(path);
        var content = new MemoryStream(file.CanSeek ? (int)Math.Min(file.Length, most) : 0);
        byte[] chunk = ArrayPool<byte>.Shared.Rent(Chunk);

        try
        {
            int read;

            while ((read = await file.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (content.Length + read > most)
                    throw new DocumentTooLargeException($"it grew past {most} bytes while it was read");

                await content.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }

        content.Position = 0;

        return content;
    }

    /// <remarks>A root is never a link, and is not asked: Windows answers the question about <c>C:\</c> with an exception.</remarks>
    private static string Resolved(string existing)
    {
        string? parent = Path.GetDirectoryName(existing);

        if (string.IsNullOrEmpty(parent) || parent == existing)
            return existing;

        if (Link(existing) is { } target)
            return Resolved(target);

        string real = Resolved(parent);

        return string.Equals(real, parent, StringComparison.Ordinal)
            ? existing
            : Path.Combine(real, Path.GetFileName(existing));
    }

    private static string? Link(string path) =>
        (Directory.Exists(path)
            ? new DirectoryInfo(path).ResolveLinkTarget(returnFinalTarget: true)
            : new FileInfo(path).ResolveLinkTarget(returnFinalTarget: true))?.FullName;
}
