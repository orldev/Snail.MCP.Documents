using System.Collections.Concurrent;
using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Application;

/// <summary>The documents read a moment ago, kept so the next piece of one is served without reading the file again.</summary>
/// <remarks>
/// A long PDF is read in many pieces, and each piece is another call: without this every one of them would parse
/// the whole file again. A document is remembered under its path, its size and the moment it was last written, so a
/// file that changed is read afresh rather than served as it was.
/// </remarks>
public sealed class DocumentMemory(ServerConfig config, TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, Remembered> _documents = new(StringComparer.Ordinal);

    private TimeSpan Lifetime => TimeSpan.FromMinutes(config.MemoryMinutes);

    /// <summary>The document as it was read, or null when it was not, was read too long ago, or the file changed since.</summary>
    public Document? Of(FileInfo file) =>
        _documents.TryGetValue(file.FullName, out var remembered) && IsFresh(remembered) && remembered.Stamp == Stamp(file)
            ? remembered.Document
            : null;

    /// <summary>Remembers a document under its file, making room when the memory is full.</summary>
    public void Keep(FileInfo file, Document document)
    {
        if (Lifetime <= TimeSpan.Zero)
            return;

        _documents[file.FullName] = new Remembered(document, Stamp(file), clock.GetUtcNow());

        Forget();
    }

    private static string Stamp(FileInfo file) =>
        $"{file.Length}:{file.LastWriteTimeUtc.Ticks}";

    private bool IsFresh(Remembered remembered) => clock.GetUtcNow() - remembered.ReadAt < Lifetime;

    private void Forget()
    {
        var kept = _documents
            .Where(entry => IsFresh(entry.Value))
            .OrderByDescending(entry => entry.Value.ReadAt)
            .Take(config.MemoryDocuments)
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string path in _documents.Keys.Where(path => !kept.Contains(path)))
            _documents.TryRemove(path, out _);
    }

    private sealed record Remembered(Document Document, string Stamp, DateTimeOffset ReadAt);
}
