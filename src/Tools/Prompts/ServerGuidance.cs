namespace Snail.MCP.Documents.Tools.Prompts;

/// <summary>What the client is told about this server when it connects.</summary>
/// <remarks>
/// The instructions are read once, before any tool is called, so they carry what a model cannot discover by
/// trying: which question each tool answers, where the files are, and what the server cannot do.
/// </remarks>
public static class ServerGuidance
{
    /// <summary>The text that rides along with initialize.</summary>
    public const string Instructions =
        """
        This server reads the documents of the working copy as Markdown, and only reads. Every tool is named
        documents_*. Paths are relative to the copy; files a person attached to the task are under
        .swarm/attachments.

        Three questions, three tools. documents_outline answers what a document holds — its pages, sheets,
        slides and headings, and its tables — and costs almost nothing; ask it first of a long document.
        documents_read answers what the document says: its text in pieces, each page, sheet and slide marked,
        starting anywhere the outline points to. documents_table answers what a table holds, row by row, as
        cells rather than prose — use it for numbers you will count, compare or sum.

        A PDF page with no text layer is a scan: it is marked as one and its words are not here. Read such
        pages with a model that sees pictures instead. Everything else this server reads is text.

        Every answer is JSON with an ok flag. When ok is false, read error and hint and change the call
        instead of repeating it.
        """;
}
