namespace Snail.MCP.Documents.Adapters;

/// <summary>Plain text: paragraphs where the file has blank lines between them.</summary>
public sealed class PlainTextReader : IFormatReader
{
    /// <inheritdoc/>
    public DocumentKind Format => DocumentKind.Text;

    /// <inheritdoc/>
    public IReadOnlyList<string> Extensions { get; } = [".txt", ".text", ".log"];

    /// <inheritdoc/>
    public IReadOnlyList<Block> Read(Stream content) =>
    [
        .. Decoding.Text(content)
            .ReplaceLineEndings("\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
            .Select(paragraph => new Paragraph(paragraph.Trim('\n')))
    ];
}
