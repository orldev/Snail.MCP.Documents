using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using S = DocumentFormat.OpenXml.Spreadsheet;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Snail.MCP.Documents.Tests.Support;

/// <summary>Real files of every format, built the way their own programs write them, small enough to read at a glance.</summary>
internal static class Samples
{
    /// <summary>A Word report whose heading style has the id a Russian Word gives «Heading 1»: <c>1</c>.</summary>
    public static byte[] Word()
    {
        using var stream = new MemoryStream();

        using (var word = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = word.AddMainDocumentPart();

            main.AddNewPart<StyleDefinitionsPart>().Styles = new W.Styles(
                new W.Style(new W.StyleName { Val = "heading 1" }) { Type = W.StyleValues.Paragraph, StyleId = "1" });

            main.AddNewPart<NumberingDefinitionsPart>().Numbering = new W.Numbering(
                new W.AbstractNum(new W.Level(new W.NumberingFormat { Val = W.NumberFormatValues.Bullet }) { LevelIndex = 0 }) { AbstractNumberId = 0 },
                new W.NumberingInstance(new W.AbstractNumId { Val = 0 }) { NumberID = 1 });

            main.Document = new W.Document(new W.Body(
                Paragraph("Отчёт за неделю", new W.ParagraphProperties(new W.ParagraphStyleId { Val = "1" })),
                Paragraph("Первый абзац отчёта."),
                Paragraph("пункт списка", new W.ParagraphProperties(new W.NumberingProperties(new W.NumberingLevelReference { Val = 0 }, new W.NumberingId { Val = 1 }))),
                new W.Table(
                    Row("Месяц", "Сумма"),
                    Row("Январь", "100"))));
        }

        return stream.ToArray();
    }

    /// <summary>A workbook of two sheets: one with a header, a date, a sum and a number stored with its binary noise; one empty.</summary>
    public static byte[] Excel()
    {
        using var stream = new MemoryStream();

        using (var book = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbook = book.AddWorkbookPart();
            workbook.Workbook = new S.Workbook();

            workbook.AddNewPart<SharedStringTablePart>().SharedStringTable = new S.SharedStringTable(
                new S.SharedStringItem(new S.Text("Дата")),
                new S.SharedStringItem(new S.Text("Сумма")));

            workbook.AddNewPart<WorkbookStylesPart>().Stylesheet = new S.Stylesheet(
                new S.CellFormats(new S.CellFormat { NumberFormatId = 0 }, new S.CellFormat { NumberFormatId = 14, ApplyNumberFormat = true }));

            var revenue = workbook.AddNewPart<WorksheetPart>();
            revenue.Worksheet = new S.Worksheet(new S.SheetData(
                new S.Row(Shared("A1", 0), Shared("B1", 1)) { RowIndex = 1 },
                new S.Row(new S.Cell { CellReference = "A2", StyleIndex = 1, CellValue = new S.CellValue("46305") }, Number("B2", "0.30000000000000004")) { RowIndex = 2 },
                new S.Row(Number("B4", "12")) { RowIndex = 4 }));

            var empty = workbook.AddNewPart<WorksheetPart>();
            empty.Worksheet = new S.Worksheet(new S.SheetData());

            workbook.Workbook.AppendChild(new S.Sheets(
                new S.Sheet { Id = workbook.GetIdOfPart(revenue), SheetId = 1, Name = "Выручка" },
                new S.Sheet { Id = workbook.GetIdOfPart(empty), SheetId = 2, Name = "Пусто" }));
        }

        return stream.ToArray();
    }

    /// <summary>A presentation of one slide: a title, two points one level apart, a table and the speaker's notes.</summary>
    public static byte[] Slides()
    {
        using var stream = new MemoryStream();

        using (var deck = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentation = deck.AddPresentationPart();
            var slide = presentation.AddNewPart<SlidePart>();

            slide.Slide = new P.Slide(new P.CommonSlideData(new P.ShapeTree(
                Placeholder(P.PlaceholderValues.Title, Text("План работ")),
                Placeholder(P.PlaceholderValues.Body, Text("Первое", 0), Text("Второе", 1)),
                new P.GraphicFrame(new A.Graphic(new A.GraphicData(new A.Table(
                    CellRow("Этап", "Срок"),
                    CellRow("Сбор", "май"))))))));

            slide.AddNewPart<NotesSlidePart>().NotesSlide = new P.NotesSlide(new P.CommonSlideData(new P.ShapeTree(
                Placeholder(P.PlaceholderValues.Body, Text("Сказать про сроки")))));

            presentation.Presentation = new P.Presentation(new P.SlideIdList(new P.SlideId { Id = 256, RelationshipId = presentation.GetIdOfPart(slide) }));
        }

        return stream.ToArray();
    }

    /// <summary>A PDF of two pages: text on the first, a picture alone on the second — a scan.</summary>
    public static byte[] Pdf()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4).AddText("Quarterly report", 12, new UglyToad.PdfPig.Core.PdfPoint(72, 720), font);
        builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4).AddPng(Png, new UglyToad.PdfPig.Core.PdfRectangle(72, 72, 300, 300));

        return builder.Build();
    }

    /// <summary>A CSV the way a Russian Excel saves one: Windows-1251, semicolons, a quoted cell holding the separator.</summary>
    public static byte[] Csv()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return Encoding.GetEncoding(1251).GetBytes("Город;Сумма\nМосква;\"1;5\"\nКазань;7\n");
    }

    /// <summary>The smallest PNG there is: one pixel.</summary>
    private static byte[] Png { get; } = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static W.Paragraph Paragraph(string text, W.ParagraphProperties? properties = null) =>
        properties is null
            ? new W.Paragraph(new W.Run(new W.Text(text)))
            : new W.Paragraph(properties, new W.Run(new W.Text(text)));

    private static W.TableRow Row(params string[] cells) =>
        new([.. cells.Select(cell => new W.TableCell(Paragraph(cell)))]);

    private static S.Cell Shared(string reference, int index) =>
        new() { CellReference = reference, DataType = S.CellValues.SharedString, CellValue = new S.CellValue(index.ToString(System.Globalization.CultureInfo.InvariantCulture)) };

    private static S.Cell Number(string reference, string value) =>
        new() { CellReference = reference, CellValue = new S.CellValue(value) };

    private static P.Shape Placeholder(P.PlaceholderValues type, params A.Paragraph[] paragraphs) =>
        new(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = 2, Name = type.ToString() },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = type })),
            new P.ShapeProperties(),
            new P.TextBody([new A.BodyProperties(), .. paragraphs]));

    private static A.Paragraph Text(string text, int level = 0) =>
        new(new A.ParagraphProperties { Level = level }, new A.Run(new A.Text(text)));

    private static A.TableRow CellRow(params string[] cells) =>
        new([.. cells.Select(cell => (OpenXmlElement)new A.TableCell(new A.TextBody(new A.BodyProperties(), Text(cell))))]);
}
