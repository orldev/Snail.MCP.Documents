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

    private readonly ConcurrentDictionary<string, Lazy<Task<Opened>>> _reading = new(StringComparer.Ordinal);

    private TimeSpan Lifetime => TimeSpan.FromMinutes(config.MemoryMinutes);

    /// <summary>The document as it was read, or null when it was not, was read too long ago, or the file changed since.</summary>
    public Document? Of(string path, FileStamp stamp) =>
        _documents.TryGetValue(path, out var remembered) && IsFresh(remembered) && remembered.Stamp == stamp
            ? remembered.Document
            : null;

    /// <summary>The document as remembered, or read once however many calls ask for the same file at the same moment.</summary>
    /// <remarks>
    /// The memory is checked before a read and filled after it, so without this sixteen calls that arrive together for a
    /// file nobody has read yet parsed it sixteen times — sixteen copies of the file and its text on the heap at once.
    /// The read is shared and not cancelled by any one caller, since its failure would be every caller's; a caller that
    /// gives up only stops waiting for it.
    /// </remarks>
    public async Task<Opened> OnceAsync(string path, FileStamp stamp, Func<Task<Opened>> read, CancellationToken cancellationToken = default)
    {
        if (Of(path, stamp) is { } remembered)
            return Opened.Of(remembered);

        string key = $"{path}|{stamp.Length}|{stamp.WrittenUtc.Ticks}";
        var reading = _reading.GetOrAdd(key, _ => new Lazy<Task<Opened>>(read));

        try
        {
            var opened = await reading.Value.WaitAsync(cancellationToken);

            if (opened.Document is { } document)
                Keep(path, stamp, document);

            return opened;
        }
        finally
        {
            _reading.TryRemove(KeyValuePair.Create(key, reading));
        }
    }

    /// <summary>Remembers a document under the path and version of its file, making room when the memory is full.</summary>
    public void Keep(string path, FileStamp stamp, Document document)
    {
        if (Lifetime <= TimeSpan.Zero)
            return;

        _documents[path] = new Remembered(document, stamp, clock.GetUtcNow());

        Forget();
    }

    private bool IsFresh(Remembered remembered) => clock.GetUtcNow() - remembered.ReadAt < Lifetime;

    private void Forget()
    {
        var fresh = _documents.Where(entry => IsFresh(entry.Value));
        var newest = fresh.OrderByDescending(entry => entry.Value.ReadAt).Take(config.MemoryDocuments);
        var kept = newest.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

        foreach (string path in _documents.Keys.Where(path => !kept.Contains(path)))
            _documents.TryRemove(path, out _);
    }

    private sealed record Remembered(Document Document, FileStamp Stamp, DateTimeOffset ReadAt);
}
