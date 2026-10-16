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

    /// <summary>Pages, sheets or slides an outline lists; a document with more names the count of the rest instead.</summary>
    /// <remarks>Uncapped, a workbook of two thousand empty sheets made an outline of 187 443 characters — longer than the largest piece of text one answer may carry.</remarks>
    public const int MostParts = 300;

    /// <summary>Tables an outline lists; a document with more names the count of the rest instead.</summary>
    public const int MostTables = 300;

    /// <summary>Characters of one cell a table answer carries; a cell longer than this is prose, read with documents_read.</summary>
    public const int CellCharacters = 2_000;

    /// <summary>Characters the whole text of one document may come to; a file that unpacks to more is refused, not read.</summary>
    /// <remarks>
    /// The size of a file says nothing about the size of its text: a workbook of 11 648 bytes with one cell at XFD on
    /// each row came to 98 408 325 characters and 4.69 GB allocated. Twenty million characters are forty megabytes of
    /// text, which the 512 MB a container is given holds beside the memory of documents.
    /// </remarks>
    public const int MostDocumentCharacters = 20_000_000;

    /// <summary>Cells one table may come to, counting the empty ones a short row is padded with.</summary>
    /// <remarks>
    /// Padding is where a small file grows: rows that each reach the last column of a sheet, or records that each name
    /// a key of their own, become grids of millions of empty cells. A million cells is a sheet of fifty thousand rows
    /// of twenty columns.
    /// </remarks>
    public const int MostCells = 1_000_000;

    /// <summary>Seconds one file may take to read before the reading is stopped and refused.</summary>
    /// <remarks>
    /// The budget of characters bounds what a file becomes, not how long it takes to become it: a sparse workbook took 27 s
    /// before the budget refused it, and a page of fifty thousand nested elements keeps the HTML parser busy for some
    /// ninety. Half a minute is several times the slowest real document of the benchmarks, a PDF of twenty pages in 17 ms.
    /// </remarks>
    public const int MostReadingSeconds = 30;

    /// <summary>Levels a list item is indented at most; Word and PowerPoint number nine, from 0 to 8.</summary>
    /// <remarks>A level is an integer in the file, and indenting by it would turn one item into a line of millions of spaces.</remarks>
    public const int MostListDepth = 8;
}
