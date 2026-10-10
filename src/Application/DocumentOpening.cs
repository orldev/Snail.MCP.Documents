using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Tools.Prompts;

namespace Snail.MCP.Documents.Application;

/// <summary>Brings a file of the session into memory as a document: inside the root, under the size limit, in a format read here.</summary>
public sealed class DocumentOpening(SessionRoots roots, IEnumerable<IFormatReader> readers, DocumentMemory memory, ServerConfig config)
{
    /// <summary>The document at <paramref name="path"/>, or why it cannot be read.</summary>
    public async Task<Opened> OpenAsync(string? path, CancellationToken cancellationToken = default)
    {
        var reached = roots.Reach(path);
        if (!reached.IsAllowed)
            return Opened.Refused(reached.Error!, reached.Hint);

        var file = new FileInfo(reached.Path!);
        if (file.Length > config.MaxFileBytes)
            return Opened.Refused(Messages.TooLarge(file.Length, config.MaxFileBytes), Messages.TooLargeHint);

        string extension = file.Extension.ToLowerInvariant();
        if (readers.FirstOrDefault(reader => reader.Extensions.Contains(extension, StringComparer.Ordinal)) is not { } reader)
            return Opened.Refused(Messages.Unknown(extension), Messages.UnknownHint);

        if (memory.Of(file) is { } remembered)
            return Opened.Of(remembered);

        using var content = new MemoryStream();
        await using (var stream = file.OpenRead())
            await stream.CopyToAsync(content, cancellationToken);

        content.Position = 0;

        try
        {
            var document = Markdown.Of(reader.Format, reader.Read(content));
            memory.Keep(file, document);

            return Opened.Of(document);
        }
        catch (InvalidDataException failure)
        {
            return Opened.Refused(Messages.Damaged(reader.Format.ToString(), failure.Message), Messages.DamagedHint);
        }
    }
}
