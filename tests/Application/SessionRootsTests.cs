using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Tests.Application;

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
    public void Reach_AFileOfTheCopy_IsReadAtItsFullPath()
    {
        _copy.Write(".swarm/attachments/report.docx", "the report");

        var reached = Roots().Reach(".swarm/attachments/report.docx");

        Assert.True(reached.IsAllowed);
        Assert.Equal("the report", File.ReadAllText(reached.Path!));
    }

    [Fact]
    public void Reach_APathClimbingOutOfTheCopy_IsRefused()
    {
        _elsewhere.Write("secret.txt", "x");

        var reached = Roots().Reach($"../{Path.GetFileName(_elsewhere.Root)}/secret.txt");

        Assert.False(reached.IsAllowed);
        Assert.Contains("outside this session", reached.Error, StringComparison.Ordinal);
    }

    /// <summary>A link in the middle of a path resolves to itself under ResolveLinkTarget, and walks straight out of the root.</summary>
    [Fact]
    public void Reach_AFileThroughALinkLeadingOutOfTheCopy_IsRefused()
    {
        _elsewhere.Write("secret.txt", "x");
        Directory.CreateSymbolicLink(Path.Combine(_copy.Root, "out"), _elsewhere.Root);

        var reached = Roots().Reach("out/secret.txt");

        Assert.False(reached.IsAllowed);
    }

    [Fact]
    public void Reach_AFileThatIsNotThere_SaysSoRatherThanThatItIsOutside()
    {
        var reached = Roots().Reach("missing.pdf");

        Assert.False(reached.IsAllowed);
        Assert.Contains("there is no file", reached.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Reach_NoPathAtAll_AsksForAFileAndSaysWhereFrom(string? path)
    {
        var reached = Roots().Reach(path);

        Assert.False(reached.IsAllowed);
        Assert.Equal("name a file", reached.Error);
        Assert.Contains(_copy.Root, reached.Hint, StringComparison.Ordinal);
    }

    /// <summary>A server with no roots at all reads nothing, rather than the whole disk.</summary>
    [Fact]
    public void Reach_WithNoRootAnywhere_ReadsNothing()
    {
        var reached = new SessionRoots(new ServerConfig(), new StdioAddress(), new LocalDisk()).Reach("/etc/hosts");

        Assert.False(reached.IsAllowed);
    }

    private SessionRoots Roots() => new(new ServerConfig { Roots = [_copy.Root] }, new StdioAddress(), new LocalDisk());
}
