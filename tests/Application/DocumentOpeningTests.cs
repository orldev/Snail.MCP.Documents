using Microsoft.Extensions.Time.Testing;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Configuration;

namespace Snail.MCP.Documents.Tests.Application;

/// <summary>How a file becomes a document: within a deadline, and answered with a reason when it cannot.</summary>
public sealed class DocumentOpeningTests : IDisposable
{
    private readonly Folder _copy = new();

    private readonly FakeTimeProvider _clock = new();

    public void Dispose() => _copy.Dispose();

    /// <summary>A file built to keep a reader busy costs the deadline and no more, and the model is told why there is nothing.</summary>
    [Fact]
    public async Task Open_AFileTheReaderIsStillOnAtTheDeadline_IsRefusedAsTooSlow()
    {
        _copy.Write("busy.slow", "x");
        var config = new ServerConfig { Roots = [_copy.Root] };
        var disk = new LocalDisk();
        var opening = new DocumentOpening(new SessionRoots(config, new StdioAddress(), disk), new DocumentReader([new EndlessReader()], _clock), new DocumentMemory(config, _clock), config, disk, _clock);

        var opened = opening.OpenAsync("busy.slow");

        while (!opened.IsCompleted)
        {
            _clock.Advance(TimeSpan.FromSeconds(ReadingLimits.MostReadingSeconds));
            await Task.WhenAny(opened, Task.Delay(TimeSpan.FromMilliseconds(20)));
        }

        Assert.Equal($"the file took more than {ReadingLimits.MostReadingSeconds} seconds to read and was not read to the end", (await opened).Error);
    }

    /// <summary>
    /// The memory is checked before a read and filled after it; without one read per file, sixteen calls arriving
    /// together parsed it sixteen times — sixteen copies of the file and its text on the heap at once.
    /// </summary>
    [Fact]
    public async Task Open_SixteenCallsForOneUnreadFile_ParseItOnce()
    {
        _copy.Write("report.slow", "x");
        var reader = new SlowReader();
        var config = new ServerConfig { Roots = [_copy.Root] };
        var disk = new LocalDisk();
        var opening = new DocumentOpening(new SessionRoots(config, new StdioAddress(), disk), new DocumentReader([reader]), new DocumentMemory(config, TimeProvider.System), config, disk, TimeProvider.System);

        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => opening.OpenAsync("report.slow"))));

        Assert.Equal(1, reader.Reads);
    }

    private sealed class SlowReader : IFormatReader
    {
        private int _reads;

        public int Reads => _reads;

        public DocumentKind Kind => DocumentKind.Text;

        public IReadOnlyList<string> Extensions { get; } = [".slow"];

        public async Task<IReadOnlyList<Block>> ReadAsync(Stream content, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _reads);
            await Task.Delay(200, cancellationToken);

            return [new Paragraph("x")];
        }
    }

    private sealed class EndlessReader : IFormatReader
    {
        public DocumentKind Kind => DocumentKind.Text;

        public IReadOnlyList<string> Extensions { get; } = [".slow"];

        public async Task<IReadOnlyList<Block>> ReadAsync(Stream content, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);

            return [];
        }
    }
}
