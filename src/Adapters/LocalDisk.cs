using System.Buffers;

namespace Snail.MCP.Documents.Adapters;

/// <summary>The file system of the machine this server runs on.</summary>
public sealed class LocalDisk : IDisk
{
    private const int Chunk = 81_920;

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
}
