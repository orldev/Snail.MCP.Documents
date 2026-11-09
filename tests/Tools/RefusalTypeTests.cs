using ModelContextProtocol.Protocol;
using Snail.MCP.Documents.Domain;
using Snail.MCP.Documents.Tools;
using Snail.Sdk.Mcp.Testing;

namespace Snail.MCP.Documents.Tests.Tools;

/// <summary>Every refusal of the server's own carries its kind as the top-level <c>type</c>, the word a program branches on.</summary>
public sealed class RefusalTypeTests : IDisposable
{
    private readonly Folder _copy = new();

    public void Dispose() => _copy.Dispose();

    [Theory]
    [InlineData(-1, null, Limits.DefaultCharacters)]
    [InlineData(0, 0, Limits.DefaultCharacters)]
    [InlineData(0, null, Limits.MostCharacters + 1)]
    public async Task Read_ANumberOutsideItsRange_IsTypedArguments(int from, int? part, int maxCharacters)
    {
        _copy.Write("a.txt", "текст");

        var answer = await WithToolsAsync(tools => tools.ReadAsync("a.txt", from, part, maxCharacters));

        Assert.Equal("arguments", answer.Type());
    }

    [Theory]
    [InlineData(0, 0, Limits.DefaultRows)]
    [InlineData(1, -1, Limits.DefaultRows)]
    [InlineData(1, 0, Limits.MostRows + 1)]
    public async Task Table_ANumberOutsideItsRange_IsTypedArguments(int table, int fromRow, int maxRows)
    {
        _copy.Write("data.csv", "a,b\n1,2");

        var answer = await WithToolsAsync(tools => tools.TableAsync("data.csv", table, fromRow, maxRows));

        Assert.Equal("arguments", answer.Type());
    }

    [Fact]
    public async Task Read_APartTheDocumentDoesNotHave_IsTypedNoSuchPart()
    {
        _copy.Write("book.xlsx", Samples.Excel());

        var answer = await WithToolsAsync(tools => tools.ReadAsync("book.xlsx", 0, 5));

        Assert.Equal("noSuchPart", answer.Type());
    }

    [Fact]
    public async Task Table_ATableTheDocumentDoesNotHave_IsTypedNoSuchTable()
    {
        _copy.Write("a.txt", "текст");

        var answer = await WithToolsAsync(tools => tools.TableAsync("a.txt"));

        Assert.Equal("noSuchTable", answer.Type());
    }

    [Fact]
    public async Task Read_FromPastTheEnd_IsTypedPastTheEnd()
    {
        _copy.Write("a.txt", "текст");

        var answer = await WithToolsAsync(tools => tools.ReadAsync("a.txt", 100));

        Assert.Equal("pastTheEnd", answer.Type());
    }

    [Fact]
    public async Task Table_FromARowPastTheEnd_IsTypedPastTheEnd()
    {
        _copy.Write("data.csv", "a,b\n1,2\n3,4");

        var answer = await WithToolsAsync(tools => tools.TableAsync("data.csv", 1, 50));

        Assert.Equal("pastTheEnd", answer.Type());
    }

    [Fact]
    public async Task Read_AFileOverTheSizeLimit_IsTypedTooLarge()
    {
        _copy.Write("big.txt", new string('x', 4_096));

        var answer = await WithToolsAsync(tools => tools.ReadAsync("big.txt"), maxFileBytes: 1_024);

        Assert.Equal("tooLarge", answer.Type());
    }

    [Fact]
    public async Task Read_AFormatNobodyReads_IsTypedUnknownFormat()
    {
        _copy.Write("old.doc", "x");

        var answer = await WithToolsAsync(tools => tools.ReadAsync("old.doc"));

        Assert.Equal("unknownFormat", answer.Type());
    }

    [Fact]
    public async Task Outline_AFileThatIsNotWhatItsNameSays_IsTypedDamaged()
    {
        _copy.Write("fake.docx", "plain text");

        var answer = await WithToolsAsync(tools => tools.OutlineAsync("fake.docx"));

        Assert.Equal("damaged", answer.Type());
    }

    [Fact]
    public async Task Read_PdfUnderAPassword_IsTypedLocked()
    {
        _copy.Write("locked.pdf", Samples.LockedPdf());

        var answer = await WithToolsAsync(tools => tools.ReadAsync("locked.pdf"));

        Assert.Equal("locked", answer.Type());
    }

    [Fact]
    public async Task Read_FileThisProcessMayNotOpen_IsTypedUnreadable()
    {
        if (OperatingSystem.IsWindows())
            return;

        string file = _copy.Write("closed.txt", "x");
        File.SetUnixFileMode(file, UnixFileMode.None);

        var answer = await WithToolsAsync(tools => tools.ReadAsync("closed.txt"));

        Assert.Equal("unreadable", answer.Type());
    }

    private async Task<CallToolResult> WithToolsAsync(Func<DocumentTools, Task<CallToolResult>> call, int maxFileBytes = 52_428_800)
    {
        using var reading = new Reading(_copy.Root, maxFileBytes);
        var tools = new DocumentTools(reading.Documents, reading.Outline, reading.Tables);
        var answer = await call(tools);

        Assert.True(answer.IsError);

        return answer;
    }
}
