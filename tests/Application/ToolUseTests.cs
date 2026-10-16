using Snail.MCP.Documents.Domain;

namespace Snail.MCP.Documents.Tests.Application;

/// <summary>The three questions the tools answer, asked of real files in a working copy.</summary>
public sealed class ToolUseTests : IDisposable
{
    private readonly Folder _copy = new();

    public void Dispose() => _copy.Dispose();

    /// <summary>A document cut without a word reads as one that ended; the piece says where the next one starts.</summary>
    [Fact]
    public async Task Read_LongText_ComesInPiecesThatJoinIntoTheWhole()
    {
        string whole = string.Join("\n\n", Enumerable.Range(1, 400).Select(line => $"Строка {line} отчёта."));
        _copy.Write("notes.txt", whole);
        using var reading = new Reading(_copy.Root);

        var first = (await reading.Documents.ReadAsync("notes.txt", 0, null, 1_000)).Data!;
        int next = first["next"]!.GetValue<int>();
        var second = (await reading.Documents.ReadAsync("notes.txt", next, null, Limits.MostCharacters)).Data!;

        Assert.Equal(whole, $"{first["text"]}{second["text"]}");
        Assert.Null(second["next"]);
    }

    [Fact]
    public async Task Read_FromAPart_StartsAtThatSheetAndNamesIt()
    {
        _copy.Write("book.xlsx", Samples.Excel());
        using var reading = new Reading(_copy.Root);

        var piece = (await reading.Documents.ReadAsync("book.xlsx", 0, 2, Limits.DefaultCharacters)).Data!;

        Assert.StartsWith("## Sheet 2: Пусто", piece["text"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.Equal("Пусто", piece["parts"]![0]!["title"]!.GetValue<string>());
    }

    [Fact]
    public async Task Read_APieceCoveringAScan_ListsTheScannedPage()
    {
        _copy.Write("scan.pdf", Samples.Pdf());
        using var reading = new Reading(_copy.Root);

        var piece = (await reading.Documents.ReadAsync("scan.pdf", 0, null, Limits.DefaultCharacters)).Data!;

        Assert.Equal([2], piece["scans"]!.AsArray().Select(page => page!.GetValue<int>()));
    }

    [Fact]
    public async Task Read_APartTheDocumentDoesNotHave_SaysHowManyThereAre()
    {
        _copy.Write("book.xlsx", Samples.Excel());
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync("book.xlsx", 0, 5, Limits.DefaultCharacters);

        Assert.Equal("there is no part 5: this document has parts 1 to 2", refused.Error);
    }

    [Theory]
    [InlineData(-1, Limits.DefaultCharacters)]
    [InlineData(0, Limits.FewestCharacters - 1)]
    [InlineData(0, Limits.MostCharacters + 1)]
    public async Task Read_ANumberOutsideItsRange_IsRefusedNotClamped(int from, int maxCharacters)
    {
        _copy.Write("a.txt", "текст");
        using var reading = new Reading(_copy.Root);

        Assert.False((await reading.Documents.ReadAsync("a.txt", from, null, maxCharacters)).IsSuccess);
    }

    [Fact]
    public async Task Read_PartZero_IsRefusedWithTheRange()
    {
        _copy.Write("book.xlsx", Samples.Excel());
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync("book.xlsx", 0, 0, Limits.DefaultCharacters);

        Assert.Equal("part must be between 1 and 2147483647, not 0", refused.Error);
    }

    /// <summary>A position past the end says how long the text is, so the next call can be a right one.</summary>
    [Fact]
    public async Task Read_FromPastTheEnd_SaysHowLongTheTextIs()
    {
        _copy.Write("a.txt", "текст");
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync("a.txt", 100, null, Limits.DefaultCharacters);

        Assert.Equal("from 100 is past the end of the text, which has 5 characters", refused.Error);
    }

    /// <summary>A piece that ended between the halves of an emoji would hand the model a broken character at each end.</summary>
    [Fact]
    public async Task Read_PieceEndingInsideAnEmoji_EndsBeforeItAndTheNextStartsWithIt()
    {
        string whole = $"{new string('а', Limits.FewestCharacters - 1)}😀конец";
        _copy.Write("emoji.txt", whole);
        using var reading = new Reading(_copy.Root);

        var first = (await reading.Documents.ReadAsync("emoji.txt", 0, null, Limits.FewestCharacters)).Data!;
        int next = first["next"]!.GetValue<int>();
        var second = (await reading.Documents.ReadAsync("emoji.txt", next, null, Limits.FewestCharacters)).Data!;

        Assert.Equal(Limits.FewestCharacters - 1, next);
        Assert.StartsWith("😀", second["text"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.Equal(whole, $"{first["text"]}{second["text"]}");
    }

    [Fact]
    public async Task Read_AFileOverTheSizeLimit_IsRefusedBeforeItIsOpened()
    {
        _copy.Write("big.txt", new string('x', 4_096));
        using var reading = new Reading(_copy.Root, maxFileBytes: 1_024);

        var refused = await reading.Documents.ReadAsync("big.txt", 0, null, Limits.DefaultCharacters);

        Assert.Equal("the file is 4096 bytes, and this server opens files up to 1024", refused.Error);
    }

    [Fact]
    public async Task Read_AFormatNobodyReads_NamesTheFormatsThatAreRead()
    {
        _copy.Write("old.doc", "x");
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync("old.doc", 0, null, Limits.DefaultCharacters);

        Assert.Equal("'.doc' is not a format this server reads", refused.Error);
        Assert.Contains(".docx", refused.Hint, StringComparison.Ordinal);
    }

    /// <summary>A file named .docx that is something else is a fact about the file, said as one; not an exception for the model.</summary>
    [Fact]
    public async Task Read_AFileThatIsNotWhatItsNameSays_IsCalledDamaged()
    {
        _copy.Write("fake.docx", "plain text");
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync("fake.docx", 0, null, Limits.DefaultCharacters);

        Assert.StartsWith("the file could not be read as Word", refused.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Outline_Workbook_ListsItsSheetsAndTheirTables()
    {
        _copy.Write("book.xlsx", Samples.Excel());
        using var reading = new Reading(_copy.Root);

        var outline = (await reading.Outline.OutlineAsync("book.xlsx")).Data!;

        Assert.Equal("Excel", outline["kind"]!.GetValue<string>());
        Assert.Equal(["Выручка", "Пусто"], outline["parts"]!.AsArray().Select(part => part!["title"]!.GetValue<string>()));
        Assert.Equal(3, outline["tables"]![0]!["rows"]!.GetValue<int>());
        Assert.Equal(1, outline["tables"]![0]!["part"]!.GetValue<int>());
    }

    /// <summary>The hundredth page of rows is read without the first; it carries the header to be read by.</summary>
    [Fact]
    public async Task Table_ALaterPageOfRows_CarriesTheHeader()
    {
        _copy.Write("data.csv", "a,b\n" + string.Join('\n', Enumerable.Range(1, 30).Select(row => $"{row},{row * 2}")));
        using var reading = new Reading(_copy.Root);

        var page = (await reading.Tables.ReadAsync("data.csv", 1, 11, 10)).Data!;

        Assert.Equal(["a", "b"], page["header"]!.AsArray().Select(cell => cell!.GetValue<string>()));
        Assert.Equal(["11", "22"], page["cells"]![0]!.AsArray().Select(cell => cell!.GetValue<string>()));
        Assert.Equal(21, page["next"]!.GetValue<int>());
        Assert.Equal(31, page["rows"]!["total"]!.GetValue<int>());
    }

    [Fact]
    public async Task Table_OfADocumentWithNone_SaysItHoldsNone()
    {
        _copy.Write("a.txt", "текст");
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Tables.ReadAsync("a.txt", 1, 0, Limits.DefaultRows);

        Assert.Equal("this document holds no table", refused.Error);
    }

    [Theory]
    [InlineData(0, 0, Limits.DefaultRows, "table must be between 1 and 2147483647, not 0")]
    [InlineData(1, -1, Limits.DefaultRows, "fromRow must be between 0 and 2147483647, not -1")]
    [InlineData(1, 0, Limits.MostRows + 1, "maxRows must be between 1 and 1000, not 1001")]
    public async Task Table_ANumberOutsideItsRange_IsRefusedWithTheRange(int table, int fromRow, int maxRows, string error)
    {
        _copy.Write("data.csv", "a,b\n1,2");
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Tables.ReadAsync("data.csv", table, fromRow, maxRows);

        Assert.Equal(error, refused.Error);
    }

    [Fact]
    public async Task Table_FromARowPastTheEnd_SaysHowManyRowsThereAre()
    {
        _copy.Write("data.csv", "a,b\n1,2\n3,4");
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Tables.ReadAsync("data.csv", 1, 50, Limits.DefaultRows);

        Assert.Equal("row 50 is past the end of the table, which has 3 rows", refused.Error);
    }

    [Fact]
    public async Task Table_OfAFileThatIsNotThere_SaysThereIsNoSuchFile()
    {
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Tables.ReadAsync("missing.csv", 1, 0, Limits.DefaultRows);

        Assert.Equal("there is no file 'missing.csv'", refused.Error);
    }

    /// <summary>A file read a moment ago and changed since is read afresh, not served as it was.</summary>
    [Fact]
    public async Task Read_AFileChangedSinceItWasRead_IsReadAfresh()
    {
        string file = _copy.Write("a.txt", "первая версия");
        using var reading = new Reading(_copy.Root);
        await reading.Documents.ReadAsync("a.txt", 0, null, Limits.DefaultCharacters);

        File.WriteAllText(file, "вторая версия, длиннее");
        var piece = (await reading.Documents.ReadAsync("a.txt", 0, null, Limits.DefaultCharacters)).Data!;

        Assert.Equal("вторая версия, длиннее", piece["text"]!.GetValue<string>());
    }
}
