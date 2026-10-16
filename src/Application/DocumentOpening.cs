using System.Xml;
using Snail.MCP.Documents.Application.Prompts;
using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Application;

/// <summary>Brings a file of the session into memory as a document: inside the root, under the size limit, in a format read here.</summary>
public sealed class DocumentOpening(SessionRoots roots, IEnumerable<IFormatReader> readers, DocumentMemory memory, ServerConfig config, IDisk disk, TimeProvider clock)
{
    /// <summary>The document at <paramref name="path"/>, or why it cannot be read.</summary>
    public async Task<Opened> OpenAsync(string? path, CancellationToken cancellationToken = default)
    {
        var reached = roots.Reach(path);
        if (!reached.IsAllowed)
            return Opened.Refused(reached.Error!, reached.Hint);

        try
        {
            return await OpenedAsync(reached.Path!, cancellationToken);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            return Opened.Refused(Messages.Unreadable(failure.Message), Messages.UnreadableHint);
        }
    }

    /// <summary>The file read and written as Markdown, or why it cannot be.</summary>
    /// <remarks>
    /// A file that was there when the path was checked can be gone, locked or swapped by the time it is opened; that
    /// surfaces as an I/O failure the caller answers with its reason, rather than one that leaves the use case as a crash.
    /// </remarks>
    private async Task<Opened> OpenedAsync(string path, CancellationToken cancellationToken)
    {
        var stamp = disk.Stamp(path);
        if (stamp.Length > config.MaxFileBytes)
            return Opened.Refused(Messages.TooLarge(stamp.Length, config.MaxFileBytes), Messages.TooLargeHint);

        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (readers.FirstOrDefault(reader => reader.Extensions.Contains(extension, StringComparer.Ordinal)) is not { } reader)
            return Opened.Refused(Messages.Unknown(extension), Messages.UnknownHint);

        return await memory.OnceAsync(path, stamp, () => ReadAsync(path, reader), cancellationToken);
    }

    /// <summary>The file brought into memory and read by its format, within <see cref="Limits.MostReadingSeconds"/>.</summary>
    /// <remarks>
    /// Not cancelled by the call that started it, since every call for the same file shares this read; stopped by its own
    /// deadline instead, so that a file built to keep a parser busy costs half a minute of one thread and no more.
    /// </remarks>
    private async Task<Opened> ReadAsync(string path, IFormatReader reader)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(Limits.MostReadingSeconds), clock);

        try
        {
            await using var content = await disk.ReadAsync(path, config.MaxFileBytes, deadline.Token);
            var blocks = await reader.ReadAsync(content, deadline.Token);
            var document = Markdown.Of(reader.Format, blocks);

            return Opened.Of(document);
        }
        catch (DocumentTooLargeException failure)
        {
            return Opened.Refused(Messages.TooLargeOnceRead(failure.Message), Messages.TooLargeHint);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return Opened.Refused(Messages.TooSlow(Limits.MostReadingSeconds), Messages.TooLargeHint);
        }
        catch (Exception failure) when (failure is InvalidDataException or XmlException)
        {
            return Opened.Refused(Messages.Damaged(reader.Format.ToString(), failure.Message), Messages.DamagedHint);
        }
    }
}
