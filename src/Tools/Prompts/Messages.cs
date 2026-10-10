namespace Snail.MCP.Documents.Tools.Prompts;

/// <summary>What a caller is told when something did not work: the reason, and the next move.</summary>
/// <remarks>
/// Kept apart from <see cref="ToolDescriptions"/> because the audience differs. A description is read before
/// a call and steers what is asked; a message is read after one and has to end the guessing.
/// </remarks>
public static class Messages
{
    /// <summary>A file too large to open.</summary>
    public static string TooLarge(long bytes, int most) =>
        $"the file is {bytes} bytes, and this server opens files up to {most}";

    /// <summary>What to do about a file too large to open.</summary>
    public const string TooLargeHint = "Split the document, or read a smaller export of it.";

    /// <summary>A file of a format nobody here reads.</summary>
    public static string Unknown(string extension) =>
        $"'{extension}' is not a format this server reads";

    /// <summary>What this server reads instead, for a caller that named something else.</summary>
    public const string UnknownHint = "It reads .docx, .xlsx, .pptx, .pdf, .html, .htm, .csv, .tsv, .json, .md and .txt; an older .doc, .xls or .ppt has to be saved in the newer format first.";

    /// <summary>A file that claims a format and is not in it.</summary>
    public static string Damaged(string format, string why) =>
        $"the file could not be read as {format}: {why}";

    /// <summary>What to do about a damaged file.</summary>
    public const string DamagedHint = "The file is damaged or is not what its name says. Ask for it again rather than retrying the read.";

    /// <summary>A position past the end of the text.</summary>
    public static string FromBeyondTheEnd(int from, int total) =>
        $"from {from} is past the end of the text, which has {total} characters";

    /// <summary>A part the document does not have.</summary>
    public static string NoSuchPart(int part, int parts) =>
        parts == 0 ? $"there is no part {part}: this document is not divided into pages, sheets or slides" : $"there is no part {part}: this document has parts 1 to {parts}";

    /// <summary>A table the document does not have.</summary>
    public static string NoSuchTable(int table, int tables) =>
        tables == 0 ? "this document holds no table" : $"there is no table {table}: this document has tables 1 to {tables}";

    /// <summary>A row past the end of the table.</summary>
    public static string RowBeyondTheEnd(int fromRow, int rows) =>
        $"row {fromRow} is past the end of the table, which has {rows} rows";

    /// <summary>A number outside the range a description promises.</summary>
    public static string OutOfRange(string name, int value, int least, int most) =>
        $"{name} must be between {least} and {most}, not {value}";

    /// <summary>What to do when the outline is the better first step.</summary>
    public const string OutlineHint = "Call documents_outline to see the parts and tables there are.";
}
