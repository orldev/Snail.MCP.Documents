using Snail.MCP.Documents.Domain;
using Snail.Sdk.Mcp.Hosting.Sessions;

namespace Snail.MCP.Documents.Tests.Application;

/// <summary>
/// What this server asks of the roots of a session, which Snail.Sdk.Mcp.Hosting keeps: a path the session reaches is a
/// file to read (<see cref="PathKind.File"/>), and one that is not is told apart from one outside the copy, its kind kept.
/// </summary>
public sealed class SessionRootsTests : IDisposable
{
    private readonly Folder _copy = new();

    private readonly Folder _elsewhere = new();

    public void Dispose()
    {
        _copy.Dispose();
        _elsewhere.Dispose();
    }

    [Fact]
    public async Task Read_AFileThatIsNotThere_SaysSoRatherThanThatItIsOutside()
    {
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync("missing.pdf", 0, null, Limits.DefaultCharacters);

        Assert.Equal("there is no file 'missing.pdf'", refused.Error);
        Assert.Equal("check the name and the folder it is in", refused.Hint);
        Assert.Equal(Reached.Absent, refused.Type);
    }

    /// <summary>
    /// A file in a folder this server may not enter is there: telling the model there is no such file sends it looking
    /// for another name, where what it needs to hear is that the server may not read it.
    /// </summary>
    [Fact]
    public async Task Read_AFileInAFolderThisServerMayNotEnter_IsUnreadableRatherThanMissing()
    {
        if (OperatingSystem.IsWindows())
            return;

        _copy.Write("locked/report.txt", "x");
        string folder = Path.Combine(_copy.Root, "locked");
        File.SetUnixFileMode(folder, UnixFileMode.None);

        try
        {
            using var reading = new Reading(_copy.Root);

            var refused = await reading.Documents.ReadAsync("locked/report.txt", 0, null, Limits.DefaultCharacters);

            Assert.False(refused.IsSuccess);
            Assert.Equal(Reached.Unreadable, refused.Type);
            Assert.NotNull(refused.Hint);
        }
        finally
        {
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public async Task Read_AFolderOfTheCopy_AsksForAFileInsideIt()
    {
        _copy.Write(".swarm/attachments/report.txt", "x");
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync(".swarm/attachments", 0, null, Limits.DefaultCharacters);

        Assert.Equal("'.swarm/attachments' is not a file", refused.Error);
        Assert.Equal("that is a folder: name a file inside it", refused.Hint);
        Assert.Equal(Reached.NotAFile, refused.Type);
    }

    /// <summary>A link in the middle of a path walks out of the copy; the file it leads to is not read, whatever it holds.</summary>
    [Fact]
    public async Task Read_AFileThroughALinkLeadingOutOfTheCopy_IsRefusedAsOutside()
    {
        _elsewhere.Write("secret.txt", "x");
        Directory.CreateSymbolicLink(Path.Combine(_copy.Root, "out"), _elsewhere.Root);
        using var reading = new Reading(_copy.Root);

        var refused = await reading.Documents.ReadAsync("out/secret.txt", 0, null, Limits.DefaultCharacters);

        Assert.False(refused.IsSuccess);
        Assert.Contains("outside this session", refused.Error, StringComparison.Ordinal);
        Assert.Equal(Reached.Outside, refused.Type);
    }
}
