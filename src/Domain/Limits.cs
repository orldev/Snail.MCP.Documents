namespace Snail.MCP.Documents.Domain;

/// <summary>The numbers the tool descriptions promise, kept where a guard test can hold prose and code to each other.</summary>
/// <remarks>
/// A value outside its range is refused, never clamped in silence: a caller that asked for two hundred thousand
/// characters and received a hundred thousand without a word has been told something false about the document.
/// </remarks>
public static class Limits
{
    /// <summary>Characters of text one answer carries when the caller names no number.</summary>
    public const int DefaultCharacters = 20_000;

    /// <summary>The smallest piece of a document worth a call.</summary>
    public const int FewestCharacters = 500;

    /// <summary>The largest piece of a document one answer may carry.</summary>
    public const int MostCharacters = 100_000;

    /// <summary>Rows of a table one answer carries when the caller names no number.</summary>
    public const int DefaultRows = 100;

    /// <summary>Rows of a table one answer may carry at most.</summary>
    public const int MostRows = 1_000;

    /// <summary>Headings an outline lists; a document with more names the count of the rest instead.</summary>
    public const int MostHeadings = 300;

    /// <summary>Characters of one cell a table answer carries; a cell longer than this is prose, read with documents_read.</summary>
    public const int CellCharacters = 2_000;
}
