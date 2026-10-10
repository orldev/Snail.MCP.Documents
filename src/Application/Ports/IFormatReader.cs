namespace Snail.MCP.Documents.Application.Ports;

/// <summary>Reads one format of file into the blocks every format shares.</summary>
/// <remarks>
/// Synchronous on purpose: the libraries behind every format read a stream that is already in memory, and
/// the file is brought into memory asynchronously before any of them is called.
/// </remarks>
public interface IFormatReader
{
    /// <summary>The format this reader reads.</summary>
    DocumentKind Format { get; }

    /// <summary>The file extensions it takes, with their dots, lower case.</summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>The blocks of the document in <paramref name="content"/>.</summary>
    /// <exception cref="InvalidDataException">The file is not in this format, or is damaged past reading.</exception>
    IReadOnlyList<Block> Read(Stream content);
}
