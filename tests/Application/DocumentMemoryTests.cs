using Microsoft.Extensions.Time.Testing;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Configuration;
using Snail.MCP.Documents.Domain;

namespace Snail.MCP.Documents.Tests.Application;

/// <summary>What the memory of documents read a moment ago keeps, for how long, and what gives way.</summary>
public sealed class DocumentMemoryTests
{
    private static readonly FileStamp Written = new(1, new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc));

    private readonly FakeTimeProvider _clock = new();

    [Fact]
    public void Of_ADocumentKeptAMomentAgo_IsServedFromMemory()
    {
        var memory = new DocumentMemory(new ServerConfig(), _clock);
        memory.Keep("/copy/a.txt", Written, Text("a"));

        var remembered = memory.Of("/copy/a.txt", Written);

        Assert.Equal("a", remembered?.Text);
    }

    /// <summary>A file written since it was read is another version of it; serving the remembered one would answer about a file that no longer is.</summary>
    [Fact]
    public void Of_AFileWrittenSinceItWasKept_IsNotServed()
    {
        var memory = new DocumentMemory(new ServerConfig(), _clock);
        memory.Keep("/copy/a.txt", Written, Text("a"));

        var remembered = memory.Of("/copy/a.txt", Written with { WrittenUtc = Written.WrittenUtc.AddSeconds(1) });

        Assert.Null(remembered);
    }

    /// <summary>Zero minutes is how a deployment says «read every file afresh»; a document kept anyway would be served stale.</summary>
    [Fact]
    public void Keep_WithAMemoryOfZeroMinutes_KeepsNothing()
    {
        var memory = new DocumentMemory(new ServerConfig { MemoryMinutes = 0 }, _clock);

        memory.Keep("/copy/a.txt", Written, Text("a"));

        Assert.Null(memory.Of("/copy/a.txt", Written));
    }

    [Fact]
    public void Of_ADocumentKeptLongerAgoThanTheMemoryLasts_IsNotServed()
    {
        var memory = new DocumentMemory(new ServerConfig { MemoryMinutes = 10 }, _clock);
        memory.Keep("/copy/a.txt", Written, Text("a"));

        _clock.Advance(TimeSpan.FromMinutes(10));

        Assert.Null(memory.Of("/copy/a.txt", Written));
    }

    /// <summary>A full memory gives up the document read longest ago, so a long session does not hold every file it ever opened.</summary>
    [Fact]
    public void Keep_PastTheNumberOfDocuments_ForgetsTheOneReadLongestAgo()
    {
        var memory = new DocumentMemory(new ServerConfig { MemoryDocuments = 1 }, _clock);
        memory.Keep("/copy/older.txt", Written, Text("older"));
        _clock.Advance(TimeSpan.FromSeconds(1));

        memory.Keep("/copy/newer.txt", Written, Text("newer"));

        Assert.Null(memory.Of("/copy/older.txt", Written));
        Assert.Equal("newer", memory.Of("/copy/newer.txt", Written)?.Text);
    }

    private static Document Text(string text) => new(DocumentKind.Text, text, [], [], []);
}
