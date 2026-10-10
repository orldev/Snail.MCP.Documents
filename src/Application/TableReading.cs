using Snail.MCP.Documents.Tools.Prompts;

namespace Snail.MCP.Documents.Application;

/// <summary>Answers «what does this table hold»: its rows as cells, a page of rows at a time.</summary>
/// <remarks>
/// Cells and not prose, because a model that counts, compares or sums reads a number out of a cell far more
/// reliably than out of a line of a Markdown table. Every page carries the header, so the hundredth page of rows
/// is as readable as the first.
/// </remarks>
public sealed class TableReading(DocumentOpening opening)
{
    /// <summary>Rows of table <paramref name="table"/> from <paramref name="fromRow"/>, or why there are none.</summary>
    public async Task<Answer> ReadAsync(string path, int table, int fromRow, int maxRows, CancellationToken cancellationToken = default)
    {
        if (Refusal(table, fromRow, maxRows) is { } refusal)
            return Answer.Failed(refusal);

        var opened = await opening.OpenAsync(path, cancellationToken);
        if (opened.Document is not { } document)
            return opened.Failure;

        if (table > document.Tables.Count)
            return Answer.Failed(Messages.NoSuchTable(table, document.Tables.Count), Messages.OutlineHint);

        var found = document.Tables[table - 1];

        return fromRow > 0 && fromRow >= found.Rows.Count
            ? Answer.Failed(Messages.RowBeyondTheEnd(fromRow, found.Rows.Count))
            : Answer.Ok(Described(path, found, fromRow, maxRows));
    }

    internal static JsonObject Described(string path, Table table, int fromRow, int maxRows)
    {
        var rows = table.Rows.Skip(fromRow).Take(maxRows).ToList();
        int next = fromRow + rows.Count;

        return new JsonObject
        {
            ["path"] = path,
            ["table"] = table.Number,
            ["title"] = table.Title,
            ["part"] = table.Part,
            ["columns"] = table.Columns,
            ["rows"] = new JsonObject { ["from"] = fromRow, ["returned"] = rows.Count, ["total"] = table.Rows.Count },
            ["next"] = next < table.Rows.Count ? next : null,
            ["header"] = fromRow > 0 && table.Rows.Count > 0 ? Cells(table.Rows[0], table.Columns) : null,
            ["cells"] = new JsonArray([.. rows.Select(row => (JsonNode)Cells(row, table.Columns))])
        };
    }

    private static JsonArray Cells(IReadOnlyList<string> row, int columns) =>
    [
        .. Enumerable.Range(0, columns).Select(column => (JsonNode?)Cut(column < row.Count ? row[column] : string.Empty))
    ];

    private static string Cut(string cell) =>
        cell.Length <= Limits.CellCharacters ? cell : $"{cell[..Limits.CellCharacters]}…";

    private static string? Refusal(int table, int fromRow, int maxRows)
    {
        if (table < 1)
            return Messages.OutOfRange("table", table, 1, int.MaxValue);

        if (fromRow < 0)
            return Messages.OutOfRange("fromRow", fromRow, 0, int.MaxValue);

        return maxRows is < 1 or > Limits.MostRows
            ? Messages.OutOfRange("maxRows", maxRows, 1, Limits.MostRows)
            : null;
    }
}
