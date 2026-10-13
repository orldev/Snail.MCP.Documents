using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using S = DocumentFormat.OpenXml.Spreadsheet;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Snail.MCP.Documents.Benchmarks;

/// <summary>Documents of the size a task brings, built in memory the way their own programs write them.</summary>
public static class Files
{
    /// <summary>A report of twenty sections, each a heading, eight paragraphs, a bulleted list and a table of twelve rows.</summary>
    public static byte[] Report()
    {
        using var stream = new MemoryStream();

        using (var word = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var main = word.AddMainDocumentPart();

            main.AddNewPart<StyleDefinitionsPart>().Styles = new W.Styles(
                new W.Style(new W.StyleName { Val = "heading 1" }) { Type = W.StyleValues.Paragraph, StyleId = "Heading1" });

            main.AddNewPart<NumberingDefinitionsPart>().Numbering = new W.Numbering(
                new W.AbstractNum(new W.Level(new W.NumberingFormat { Val = W.NumberFormatValues.Bullet }) { LevelIndex = 0 }) { AbstractNumberId = 0 },
                new W.NumberingInstance(new W.AbstractNumId { Val = 0 }) { NumberID = 1 });

            var body = new W.Body();

            for (var section = 1; section <= 20; section++)
            {
                body.Append(Paragraph($"Section {section}: what the week brought", new W.ParagraphProperties(new W.ParagraphStyleId { Val = "Heading1" })));

                for (var paragraph = 1; paragraph <= 8; paragraph++)
                    body.Append(Paragraph($"Paragraph {paragraph} of section {section}: the work went as planned, the numbers below say how far, and the risks are named at the end."));

                for (var point = 1; point <= 4; point++)
                    body.Append(Paragraph($"point {point}", new W.ParagraphProperties(new W.NumberingProperties(new W.NumberingLevelReference { Val = 0 }, new W.NumberingId { Val = 1 }))));

                var table = new W.Table(Row("Month", "Planned", "Done", "Note"));

                for (var row = 1; row <= 12; row++)
                    table.Append(Row($"Month {row}", (row * 100).ToString(CultureInfo.InvariantCulture), (row * 97).ToString(CultureInfo.InvariantCulture), "on track"));

                body.Append(table);
            }

            main.Document = new W.Document(body);
        }

        return stream.ToArray();
    }

    /// <summary>A workbook of three sheets of four hundred rows and six columns: text, numbers and dates.</summary>
    public static byte[] Ledger()
    {
        using var stream = new MemoryStream();

        using (var book = SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            var workbook = book.AddWorkbookPart();
            workbook.Workbook = new S.Workbook();

            workbook.AddNewPart<SharedStringTablePart>().SharedStringTable = new S.SharedStringTable(
                new S.SharedStringItem(new S.Text("Date")),
                new S.SharedStringItem(new S.Text("Account")),
                new S.SharedStringItem(new S.Text("Amount")));

            workbook.AddNewPart<WorkbookStylesPart>().Stylesheet = new S.Stylesheet(
                new S.CellFormats(new S.CellFormat { NumberFormatId = 0 }, new S.CellFormat { NumberFormatId = 14, ApplyNumberFormat = true }));

            var sheets = new S.Sheets();

            for (uint sheet = 1; sheet <= 3; sheet++)
            {
                var data = new S.SheetData(new S.Row(Shared("A1", 0), Shared("B1", 1), Shared("C1", 2)) { RowIndex = 1 });

                for (uint row = 2; row <= 401; row++)
                {
                    var line = new S.Row(
                        new S.Cell { CellReference = $"A{row}", StyleIndex = 1, CellValue = new S.CellValue((46000 + row).ToString(CultureInfo.InvariantCulture)) },
                        new S.Cell { CellReference = $"B{row}", DataType = S.CellValues.InlineString, InlineString = new S.InlineString(new S.Text($"account {row % 17}")) },
                        Number($"C{row}", (row * 1.1).ToString(CultureInfo.InvariantCulture)),
                        Number($"D{row}", (row * 3).ToString(CultureInfo.InvariantCulture)),
                        Number($"E{row}", "0.30000000000000004"),
                        Number($"F{row}", (row % 5).ToString(CultureInfo.InvariantCulture)))
                    {
                        RowIndex = row
                    };

                    data.Append(line);
                }

                var part = workbook.AddNewPart<WorksheetPart>();
                part.Worksheet = new S.Worksheet(data);
                sheets.Append(new S.Sheet { Id = workbook.GetIdOfPart(part), SheetId = sheet, Name = $"Sheet {sheet}" });
            }

            workbook.Workbook.AppendChild(sheets);
        }

        return stream.ToArray();
    }

    private static W.Paragraph Paragraph(string text, W.ParagraphProperties? properties = null) =>
        properties is null
            ? new W.Paragraph(new W.Run(new W.Text(text)))
            : new W.Paragraph(properties, new W.Run(new W.Text(text)));

    private static W.TableRow Row(params string[] cells) =>
        new([.. cells.Select(cell => new W.TableCell(Paragraph(cell)))]);

    private static S.Cell Shared(string reference, int index) =>
        new() { CellReference = reference, DataType = S.CellValues.SharedString, CellValue = new S.CellValue(index.ToString(CultureInfo.InvariantCulture)) };

    private static S.Cell Number(string reference, string value) =>
        new() { CellReference = reference, CellValue = new S.CellValue(value) };
}
