namespace Snail.MCP.Documents.Application.Prompts;

/// <summary>What a caller is told when something did not work: the reason, and the next move.</summary>
/// <remarks>
/// Kept apart from <see cref="Tools.Prompts.ToolDescriptions"/> because the audience differs. A description is read before
/// a call and steers what is asked; a message is read after one and has to end the guessing.
/// </remarks>
public static class Messages
{
    /// <summary>A file too large to open.</summary>
    public static string TooLarge(long bytes, int most) =>
        $"the file is {bytes} bytes, and this server opens files up to {most}";

    /// <summary>A file small enough on disk that comes to too much once read.</summary>
    public static string TooLargeOnceRead(string why) =>
        $"the file is too large once read: {why}";

    /// <summary>A file whose reading took longer than a file may.</summary>
    public static string TooSlow(int seconds) =>
        $"the file took more than {seconds} seconds to read and was not read to the end";

    /// <summary>What to do about a file too large to open.</summary>
    public const string TooLargeHint = "Split the document, or read a smaller export of it.";

    /// <summary>A file of a format nobody here reads.</summary>
    public static string Unknown(string extension) =>
        $"'{extension}' is not a format this server reads";

    /// <summary>What this server reads instead, for a caller that named something else.</summary>
    public const string UnknownHint = "It reads .docx, .xlsx, .xls, .pptx, .pdf, .html, .htm, .csv, .tsv, .json, .md and .txt; an older .doc or .ppt has to be saved in the newer format first.";

    /// <summary>A file that claims a format and is not in it.</summary>
    public static string Damaged(string format, string why) =>
        $"the file could not be read as {format}: {why}";

    /// <summary>What to do about a damaged file.</summary>
    public const string DamagedHint = "The file is damaged or is not what its name says. Ask for it again rather than retrying the read.";

    /// <summary>A file protected by a password.</summary>
    public static string Locked(string why) =>
        $"the file is protected by a password: {why}";

    /// <summary>What to do about a file under a password.</summary>
    public const string LockedHint = "Ask for the document without its password, or for an export of it; retrying the read will not open it.";

    /// <summary>A file that is there and could not be opened.</summary>
    public static string Unreadable(string why) =>
        $"the file could not be opened: {why}";

    /// <summary>What to do about a file that could not be opened.</summary>
    public const string UnreadableHint = "Check that it is a plain file this server may read; a file that is being written or replaced can be read once that is done.";

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

    /// <summary>A call that failed inside the server for a reason nobody foresaw.</summary>
    public static string Failed(string tool, string kind, string why) =>
        $"{tool} failed inside the server ({kind}): {why}";

    /// <summary>What to do about a failure inside the server.</summary>
    public const string FailedHint = "This is a fault of the server, not of the call: other documents still read. Do not repeat this call; report the file instead.";

    /// <summary>What to do when the outline is the better first step.</summary>
    public const string OutlineHint = "Call documents_outline to see the parts and tables there are.";
}
