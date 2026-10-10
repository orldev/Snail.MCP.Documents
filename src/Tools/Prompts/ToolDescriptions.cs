namespace Snail.MCP.Documents.Tools.Prompts;

/// <summary>What the model reads before calling: the tool contract, guarded by the tests in <c>ToolCatalogTests</c>.</summary>
/// <remarks>
/// Separate from anything a human reads, because these strings steer behaviour: a limit stated here is a
/// promise about the code, and a guard test fails when the two drift apart. Every word is also paid for in
/// every session, so a description says what a model cannot find out by trying, and nothing else.
/// </remarks>
public static class ToolDescriptions
{
    /// <summary><c>documents_read</c></summary>
    public const string Read =
        """
        Read a document as Markdown: Word, Excel, PowerPoint, PDF, HTML, CSV, JSON, Markdown or plain text. Headings,
        lists and tables keep their shape, and each page, sheet and slide begins with a marked heading. A long
        text comes in pieces: next is the position to pass as from for the following piece, and null when the
        document has ended. To start at a page, sheet or slide, pass its number as part instead of from. A PDF
        page with no text layer is listed under scans: its words are not in the text.
        """;

    /// <summary>The <c>path</c> of <c>documents_read</c>.</summary>
    public const string ReadPath = "The file, relative to the working copy; attachments are under .swarm/attachments.";

    /// <summary>The <c>from</c> of <c>documents_read</c>.</summary>
    public const string ReadFrom = "Position in the text to start at: 0 by default, or the next of the previous piece.";

    /// <summary>The <c>part</c> of <c>documents_read</c>.</summary>
    public const string ReadPart = "Number of the page, sheet or slide to start at, as documents_outline lists them; when given, from is ignored.";

    /// <summary>The <c>maxCharacters</c> of <c>documents_read</c>.</summary>
    public const string ReadCharacters = "Characters of text to return, 500 to 100000; 20000 by default.";

    /// <summary><c>documents_outline</c></summary>
    public const string Outline =
        """
        Say what a document holds without reading it: its format and length, its pages, sheets or slides with
        their titles, its headings, its tables with their size, and which PDF pages are scans. Each part and
        heading carries the position to pass to documents_read as from, and each table the number to pass to
        documents_table.
        """;

    /// <summary>The <c>path</c> of <c>documents_outline</c>.</summary>
    public const string OutlinePath = "The file, relative to the working copy.";

    /// <summary><c>documents_table</c></summary>
    public const string Table =
        """
        Read one table of a document as rows of cells: a sheet of a workbook, a table of a Word or HTML page,
        of a slide or of a Markdown file, or the whole of a CSV. The first row is the header as the document
        has it. A long table comes in pages of rows: next is the row to pass as fromRow, and null when the
        table has ended.
        """;

    /// <summary>The <c>path</c> of <c>documents_table</c>.</summary>
    public const string TablePath = "The file, relative to the working copy.";

    /// <summary>The <c>table</c> of <c>documents_table</c>.</summary>
    public const string TableNumber = "Number of the table as documents_outline lists it; 1 by default.";

    /// <summary>The <c>fromRow</c> of <c>documents_table</c>.</summary>
    public const string TableFromRow = "Row to start at, counting the header as 0: 0 by default, or the next of the previous page.";

    /// <summary>The <c>maxRows</c> of <c>documents_table</c>.</summary>
    public const string TableRows = "Rows to return, 1 to 1000; 100 by default.";
}
