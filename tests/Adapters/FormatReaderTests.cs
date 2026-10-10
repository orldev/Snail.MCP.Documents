using System.Text;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Application.Ports;
using Snail.MCP.Documents.Domain;

namespace Snail.MCP.Documents.Tests.Adapters;

/// <summary>Every format, read from a real file into the same Markdown, outline and tables.</summary>
public sealed class FormatReaderTests
{
    /// <summary>A Russian Word saves «Heading 1» under the id «1»; read by id, the report would have no headings at all.</summary>
    [Fact]
    public void Word_HeadingStyledByARussianWord_IsAHeadingWithItsListAndTable()
    {
        var document = Read(new WordReader(), Samples.Word());

        Assert.Equal([new Section(1, "Отчёт за неделю", 0)], document.Headings);
        Assert.Contains("# Отчёт за неделю\n\nПервый абзац отчёта.\n\n- пункт списка", document.Text, StringComparison.Ordinal);
        Assert.Equal([["Месяц", "Сумма"], ["Январь", "100"]], Rows(Assert.Single(document.Tables)));
    }

    /// <summary>A date is a number in the file; only the cell's format says it is one.</summary>
    [Fact]
    public void Excel_SheetWithADateAndANoisyNumber_ReadsThemAsExcelShowsThem()
    {
        var document = Read(new ExcelReader(), Samples.Excel());

        Assert.Equal(["Выручка", "Пусто"], document.Parts.Select(part => part.Title));
        Assert.Equal([["Дата", "Сумма"], ["2026-10-10", "0.3"], [string.Empty, "12"]], Rows(Assert.Single(document.Tables)));
        Assert.Equal(1, document.Tables[0].Part);
        Assert.StartsWith("## Sheet 1: Выручка\n\n| Дата | Сумма |", document.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void PowerPoint_Slide_IsTitledAndCarriesItsPointsTableAndNotes()
    {
        var document = Read(new PowerPointReader(), Samples.Slides());

        Assert.Equal("План работ", Assert.Single(document.Parts).Title);
        Assert.Contains("- Первое\n  - Второе", document.Text, StringComparison.Ordinal);
        Assert.Equal([["Этап", "Срок"], ["Сбор", "май"]], Rows(Assert.Single(document.Tables)));
        Assert.Contains("Notes: Сказать про сроки", document.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("- План работ", document.Text, StringComparison.Ordinal);
    }

    /// <summary>A page with a picture and no text is a scan, said so — not an empty page passed over in silence.</summary>
    [Fact]
    public void Pdf_PageOfTextAndPageOfPicture_ReadsTheTextAndMarksTheScan()
    {
        var document = Read(new PdfReader(), Samples.Pdf());

        Assert.Equal([false, true], document.Parts.Select(part => part.IsScanned));
        Assert.Contains("Quarterly report", document.Text, StringComparison.Ordinal);
        Assert.Contains("## Page 2\n\n*This page is a scan", document.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pdf_ThatIsNotAPdf_IsDamagedData() =>
        Assert.Throws<InvalidDataException>(() => new PdfReader().Read(new MemoryStream("not a pdf"u8.ToArray())));

    [Fact]
    public void Word_ThatIsNotAPackage_IsDamagedData() =>
        Assert.Throws<InvalidDataException>(() => new WordReader().Read(new MemoryStream("not a zip"u8.ToArray())));

    /// <summary>Read as UTF-8, a Windows-1251 CSV turns every Cyrillic letter into noise; a quoted cell keeps its separator.</summary>
    [Fact]
    public void Csv_SavedByARussianExcel_ReadsItsLettersSeparatorAndQuotedCell()
    {
        var document = Read(new DelimitedReader(), Samples.Csv());

        Assert.Equal([["Город", "Сумма"], ["Москва", "1;5"], ["Казань", "7"]], Rows(Assert.Single(document.Tables)));
    }

    [Fact]
    public void Json_ListOfRecords_IsATable()
    {
        var document = Read(new JsonReader(), """[{"name":"a","n":1},{"name":"b","extra":true}]"""u8.ToArray());

        Assert.Equal([["name", "n", "extra"], ["a", "1", string.Empty], ["b", string.Empty, "true"]], Rows(Assert.Single(document.Tables)));
    }

    [Fact]
    public void Json_Tree_IsKeptAsIndentedJson()
    {
        var document = Read(new JsonReader(), """{"a":{"b":[1,2]}}"""u8.ToArray());

        Assert.StartsWith("```json\n{\n  \"a\"", document.Text, StringComparison.Ordinal);
        Assert.Empty(document.Tables);
    }

    [Fact]
    public void Json_ThatIsNotJson_IsDamagedData() =>
        Assert.Throws<InvalidDataException>(() => new JsonReader().Read(new MemoryStream("{oops"u8.ToArray())));

    /// <summary>Most pages put their words loose in a div; a reader that waited for p would find them empty.</summary>
    [Fact]
    public void Html_PageWithMenuLooseTextNestedListAndTable_ReadsTheContentOnly()
    {
        var document = Read(new HtmlReader(), Encoding.UTF8.GetBytes(
            """
            <html><head><title>Цены</title><style>p{}</style></head><body>
            <nav><a href="/">Главная</a></nav>
            <div>Текст без абзаца, <b>с выделением</b>.</div>
            <ul><li>Один<ul><li>Вложенный</li></ul></li><li>Два</li></ul>
            <table><tr><th>Товар</th><th>Цена</th></tr><tr><td>Хлеб</td><td>50</td></tr></table>
            <script>alert(1)</script>
            </body></html>
            """));

        Assert.Equal("Цены", Assert.Single(document.Headings).Title);
        Assert.Contains("Текст без абзаца, с выделением.", document.Text, StringComparison.Ordinal);
        Assert.Contains("- Один\n  - Вложенный\n- Два", document.Text, StringComparison.Ordinal);
        Assert.Equal([["Товар", "Цена"], ["Хлеб", "50"]], Rows(Assert.Single(document.Tables)));
        Assert.DoesNotContain("Главная", document.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("alert", document.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_File_KeepsItsHeadingsListTableAndCode()
    {
        var document = Read(new MarkdownReader(), Encoding.UTF8.GetBytes(
            """
            # Заголовок

            Абзац.

            - пункт

            | a | b \| c |
            |---|---|
            | 1 | 2 |

            ```cs
            var x = 1;
            ```
            """));

        Assert.Equal(["Заголовок"], document.Headings.Select(heading => heading.Title));
        Assert.Equal([["a", "b | c"], ["1", "2"]], Rows(Assert.Single(document.Tables)));
        Assert.Contains("```cs\nvar x = 1;\n```", document.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_File_KeepsItsParagraphs()
    {
        var document = Read(new PlainTextReader(), Encoding.UTF8.GetBytes("Первый\nабзац.\n\nВторой."));

        Assert.Equal("Первый\nабзац.\n\nВторой.", document.Text);
    }

    private static Document Read(IFormatReader reader, byte[] bytes) =>
        Markdown.Of(reader.Format, reader.Read(new MemoryStream(bytes)));

    private static string[][] Rows(Table table) => [.. table.Rows.Select(row => row.ToArray())];
}
