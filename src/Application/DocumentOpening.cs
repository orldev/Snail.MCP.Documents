using Snail.MCP.Documents.Application.Prompts;
using Snail.MCP.Documents.Configuration;
using Snail.Sdk.Mcp.Hosting.Sessions;

namespace Snail.MCP.Documents.Application;

/// <summary>Brings a file of the session into memory as a document: inside the root, under the size limit, in a format read here.</summary>
public sealed class DocumentOpening(SessionRoots roots, DocumentReader reader, DocumentMemory memory, ServerConfig config, IDisk disk, TimeProvider clock)
{
    /// <summary>The document at <paramref name="path"/>, or why it cannot be read.</summary>
    /// <remarks>
    /// The session reaches the path with every link on it followed and a file there (<see cref="SessionRoots.Reach"/> with
    /// <see cref="PathKind.File"/>): a missing file inside the copy is told as missing and a folder as not a file, not as
    /// outside, and the refusal keeps its kind.
    /// </remarks>
    public async Task<Opened> OpenAsync(string? path, CancellationToken cancellationToken = default)
    {
        var reached = roots.Reach(path, PathKind.File);
        if (!reached.IsAllowed)
            return Opened.Refused(reached.Refusal!, reached.Hint, reached.Type);

        try
        {
            return await OpenedAsync(reached.Path!, cancellationToken);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            return Opened.Refused(Messages.Unreadable(failure.Message), Messages.UnreadableHint, Refusals.Unreadable);
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
            return Opened.Refused(Messages.TooLarge(stamp.Length, config.MaxFileBytes), Messages.TooLargeHint, Refusals.TooLarge);

        if (reader.ReaderFor(path) is not { } format)
            return Opened.Refused(Messages.Unknown(Path.GetExtension(path).ToLowerInvariant()), Messages.UnknownHint, Refusals.UnknownFormat);

        return await memory.OnceAsync(path, stamp, () => ReadAsync(path, format.Kind), cancellationToken);
    }

    /// <summary>The file brought into memory and read by its format, within <see cref="ReadingLimits.MostReadingSeconds"/>.</summary>
    /// <remarks>
    /// Not cancelled by the call that started it, since every call for the same file shares this read; stopped by its own
    /// deadline instead, so that a file built to keep a parser busy costs half a minute of one thread and no more. The disk
    /// is held to the same deadline as the reading, which the library holds to it once the bytes are in memory.
    /// </remarks>
    private async Task<Opened> ReadAsync(string path, DocumentKind format)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(ReadingLimits.MostReadingSeconds), clock);

        try
        {
            await using var content = await disk.ReadAsync(path, config.MaxFileBytes, deadline.Token);
            var reading = await reader.ReadAsync(content, path);

            return reading.TryGetDocument(out var document) ? Opened.Of(document) : Refused(reading.Failure.Value, reading.Reason, format);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return Opened.Refused(Messages.TooSlow(ReadingLimits.MostReadingSeconds), Messages.TooLargeHint, Refusals.Timeout);
        }
    }

    private static Opened Refused(ReadingFailure failure, string reason, DocumentKind format) => failure switch
    {
        ReadingFailure.TooLarge => Opened.Refused(Messages.TooLargeOnceRead(reason), Messages.TooLargeHint, Refusals.TooLarge),
        ReadingFailure.TooSlow => Opened.Refused(Messages.TooSlow(ReadingLimits.MostReadingSeconds), Messages.TooLargeHint, Refusals.Timeout),
        ReadingFailure.Locked => Opened.Refused(Messages.Locked(reason), Messages.LockedHint, Refusals.Locked),
        _ => Opened.Refused(Messages.Damaged(format.ToString(), reason), Messages.DamagedHint, Refusals.Damaged)
    };
}
