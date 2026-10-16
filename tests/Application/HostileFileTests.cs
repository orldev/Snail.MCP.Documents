using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Domain;
using S = DocumentFormat.OpenXml.Spreadsheet;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Snail.MCP.Documents.Tests.Application;

/// <summary>Files built to hurt the reader — small on disk, enormous or malformed once read — answered as data.</summary>
/// <remarks>
/// The server is one process for every session of a machine, so a file one task attaches is a file every other task's
/// session shares a heap and a thread pool with. <c>MaxFileBytes</c> measures the file on disk; these tests hold the
/// bounds on what the file becomes, each found by the audit of 2026-10-16 as a way to take the process down.
/// </remarks>
public sealed class HostileFileTests : IDisposable
{
    private const int Megabyte = 1_048_576;

    private readonly Folder _copy = new();

    public void Dispose() => _copy.Dispose();

    /// <summary>
    /// A cell at column XFD on every row makes every row 16 384 cells wide: a workbook of 11 kB unpacked to 98 million
    /// characters and 4.69 GB before the budget of cells refused it.
    /// </summary>
    [Fact]
    public async Task Read_WorkbookOfSparseRowsAtTheLastColumn_IsRefusedAsTooLarge()
    {
        var rows = Enumerable.Range(1, 200).Select(row => SheetRow((uint)row, Cell(row == 1 ? $"A{row}" : $"XFD{row}", "x")));
        _copy.Write("wide.xlsx", Workbook([.. rows]));
        using var reading = new Reading(_copy.Root, maxFileBytes: Megabyte);

        var answer = await reading.Outline.OutlineAsync("wide.xlsx");

        Assert.StartsWith("the file is too large once read", answer.Error, StringComparison.Ordinal);
    }

    /// <summary>Records that each name a key of their own become one table under the union of the keys: n records, an n by n grid.</summary>
    [Fact]
    public async Task Read_JsonRecordsWithAKeyEach_IsRefusedAsTooLarge()
    {
        string records = $"[{string.Join(',', Enumerable.Range(0, 2_000).Select(index => $$"""{"k{{index}}":1}"""))}]";
        _copy.Write("sparse.json", records);
        using var reading = new Reading(_copy.Root, maxFileBytes: Megabyte);

        var answer = await reading.Outline.OutlineAsync("sparse.json");

        Assert.StartsWith("the file is too large once read", answer.Error, StringComparison.Ordinal);
    }

    /// <summary>A reference past XFD overflowed the column arithmetic into an array no process can allocate; it is a damaged file.</summary>
    [Fact]
    public async Task Read_CellReferencePastEveryColumn_IsCalledDamaged()
    {
        _copy.Write("overflow.xlsx", Workbook([SheetRow(1, Cell("ZZZZZZZ1", "x"))]));
        using var reading = new Reading(_copy.Root);

        var answer = await reading.Documents.ReadAsync("overflow.xlsx", 0, null, Limits.DefaultCharacters);

        Assert.StartsWith("the file could not be read as Excel", answer.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// A list level is an integer in the file, and Markdown indents by twice it: a level near int.MaxValue overflowed into
    /// a negative width. Indented no deeper than the eight levels Word numbers, as a heading is no deeper than six.
    /// </summary>
    [Fact]
    public async Task Read_ListItemAtALevelNoWordWrites_IsIndentedAtMostEightLevels()
    {
        _copy.Write("deep.docx", Document(new W.Paragraph(
            new W.ParagraphProperties(new W.NumberingProperties(new W.NumberingLevelReference { Val = 1_200_000_000 }, new W.NumberingId { Val = 1 })),
            new W.Run(new W.Text("item")))));
        using var reading = new Reading(_copy.Root);

        var answer = await reading.Documents.ReadAsync("deep.docx", 0, null, Limits.DefaultCharacters);

        Assert.Equal($"{new string(' ', Limits.MostListDepth * 2)}1. item", answer.Data!["text"]!.GetValue<string>());
    }

    /// <summary>A valid package around a broken part fails only when the part is first read, as an <c>XmlException</c>; it is still a damaged file.</summary>
    [Fact]
    public async Task Read_PackageWithBrokenDocumentXml_IsCalledDamaged()
    {
        _copy.Write("broken.docx", WithEntry(Document(new W.Paragraph(new W.Run(new W.Text("x")))), "word/document.xml", "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p>"));
        using var reading = new Reading(_copy.Root);

        var answer = await reading.Documents.ReadAsync("broken.docx", 0, null, Limits.DefaultCharacters);

        Assert.StartsWith("the file could not be read as Word", answer.Error, StringComparison.Ordinal);
    }

    /// <summary>The outline promises to cost almost nothing; uncapped, two thousand empty sheets made one of 187 443 characters.</summary>
    [Fact]
    public async Task Outline_WorkbookOfTwoThousandSheets_IsNoLongerThanTheLargestPiece()
    {
        _copy.Write("sheets.xlsx", Workbook(2_000));
        using var reading = new Reading(_copy.Root);

        var outline = (await reading.Outline.OutlineAsync("sheets.xlsx")).Data!;

        Assert.InRange(outline.ToJsonString().Length, 0, Limits.MostCharacters);
    }

    /// <summary>
    /// The separator of a Markdown table opens with two runs of spaces around an optional bar; backtracking tried every
    /// split of them, and a row followed by 40 000 spaces took 22 s. Five seconds, not one: the bound tells 22 s from
    /// milliseconds, and a tighter one fails under a parallel run for reasons that are not the regex.
    /// </summary>
    [Fact]
    public async Task Markdown_RowFollowedByALongLineOfSpaces_IsReadInSeconds()
    {
        byte[] page = Encoding.UTF8.GetBytes($"| a |\n{new string(' ', 40_000)}x");
        var clock = Stopwatch.StartNew();

        await new MarkdownReader().ReadAsync(new MemoryStream(page));

        Assert.InRange(clock.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    /// <summary>A file that passes every check and then cannot be opened is answered with the reason, not with the SDK's «an error occurred».</summary>
    [Fact]
    public async Task Read_FileThisProcessMayNotOpen_IsRefusedWithAReason()
    {
        if (OperatingSystem.IsWindows())
            return;

        string file = _copy.Write("locked.txt", "x");
        File.SetUnixFileMode(file, UnixFileMode.None);
        using var reading = new Reading(_copy.Root);

        var answer = await reading.Documents.ReadAsync("locked.txt", 0, null, Limits.DefaultCharacters);

        Assert.False(answer.IsSuccess);
    }

    private static byte[] Workbook(S.Row[] rows)
    {
        using var stream = new MemoryStream();

        using (var book = SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            var workbook = book.AddWorkbookPart();
            workbook.Workbook = new S.Workbook();

            var sheet = workbook.AddNewPart<WorksheetPart>();
            sheet.Worksheet = new S.Worksheet(new S.SheetData(rows));

            workbook.Workbook.AppendChild(new S.Sheets(new S.Sheet { Id = workbook.GetIdOfPart(sheet), SheetId = 1, Name = "Sheet1" }));
        }

        return stream.ToArray();
    }

    private static byte[] Workbook(int sheets)
    {
        using var stream = new MemoryStream();

        using (var book = SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            var workbook = book.AddWorkbookPart();
            workbook.Workbook = new S.Workbook();
            var listed = new S.Sheets();

            for (uint number = 1; number <= sheets; number++)
            {
                var sheet = workbook.AddNewPart<WorksheetPart>();
                sheet.Worksheet = new S.Worksheet(new S.SheetData());
                listed.Append(new S.Sheet { Id = workbook.GetIdOfPart(sheet), SheetId = number, Name = $"Sheet {number}" });
            }

            workbook.Workbook.AppendChild(listed);
        }

        return stream.ToArray();
    }

    private static byte[] Document(params W.Paragraph[] paragraphs)
    {
        using var stream = new MemoryStream();

        using (var word = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            word.AddMainDocumentPart().Document = new W.Document(new W.Body(paragraphs));

        return stream.ToArray();
    }

    private static byte[] WithEntry(byte[] package, string entry, string text)
    {
        using var stream = new MemoryStream();
        stream.Write(package);

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            zip.GetEntry(entry)!.Delete();

            using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
            writer.Write(text);
        }

        return stream.ToArray();
    }

    private static S.Row SheetRow(uint index, params S.Cell[] cells) =>
        new(cells) { RowIndex = index };

    private static S.Cell Cell(string reference, string text) =>
        new() { CellReference = reference, DataType = S.CellValues.InlineString, InlineString = new S.InlineString(new S.Text(text)) };
}
