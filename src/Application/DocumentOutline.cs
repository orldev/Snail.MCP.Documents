namespace Snail.MCP.Documents.Application;

/// <summary>Answers «what does this document hold»: its parts, headings and tables, with where each starts.</summary>
/// <remarks>
/// The outline costs a fraction of the text and tells the model where to read: a question about the third
/// quarter is answered from one sheet, not from the whole workbook read piece by piece.
/// </remarks>
public sealed class DocumentOutline(DocumentOpening opening)
{
    /// <summary>The outline of the document, or why it cannot be read.</summary>
    public async Task<Answer> OutlineAsync(string path, CancellationToken cancellationToken = default)
    {
        var opened = await opening.OpenAsync(path, cancellationToken);

        return opened.Document is { } document ? Answer.Ok(Described(path, document)) : opened.Failure;
    }

    internal static JsonObject Described(string path, Document document) => new()
    {
        ["path"] = path,
        ["kind"] = document.Format.ToString(),
        ["characters"] = document.Text.Length,
        ["parts"] = new JsonArray([.. document.Parts.Take(Limits.MostParts).Select(part => (JsonNode)new JsonObject
        {
            ["kind"] = part.Kind.ToString(),
            ["number"] = part.Number,
            ["title"] = part.Title,
            ["from"] = part.Start,
            ["characters"] = part.Length,
            ["isScan"] = part.IsScanned ? true : null
        })]),
        ["partsNotListed"] = NotListed(document.Parts.Count, Limits.MostParts),
        ["headings"] = new JsonArray([.. document.Headings.Take(Limits.MostHeadings).Select(heading => (JsonNode)new JsonObject
        {
            ["level"] = heading.Level,
            ["title"] = heading.Title,
            ["from"] = heading.Start
        })]),
        ["headingsNotListed"] = NotListed(document.Headings.Count, Limits.MostHeadings),
        ["tables"] = new JsonArray([.. document.Tables.Take(Limits.MostTables).Select(table => (JsonNode)new JsonObject
        {
            ["number"] = table.Number,
            ["title"] = table.Title,
            ["part"] = table.Part,
            ["rows"] = table.Rows.Count,
            ["columns"] = table.Columns,
            ["from"] = table.Start
        })]),
        ["tablesNotListed"] = NotListed(document.Tables.Count, Limits.MostTables)
    };

    private static int? NotListed(int count, int most) => count > most ? count - most : null;
}
