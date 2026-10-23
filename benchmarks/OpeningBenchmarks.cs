using BenchmarkDotNet.Attributes;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Configuration;
using Snail.Toolkit.Documents;

namespace Snail.MCP.Documents.Benchmarks;

/// <summary>What documents_read pays before a format is read: the path reached, the file brought from disk, the document made.</summary>
/// <remarks>The memory of documents is off, so every call reads the file as the first call for it does.</remarks>
[MemoryDiagnoser]
public class OpeningBenchmarks
{
    private string _copy = string.Empty;

    private DocumentOpening _opening = null!;

    [GlobalSetup]
    public void Setup()
    {
        _copy = Directory.CreateTempSubdirectory("s3l-bench-").FullName;
        File.WriteAllBytes(Path.Combine(_copy, "export.csv"), Files.Export());
        File.WriteAllBytes(Path.Combine(_copy, "report.docx"), Files.Report());

        var config = new ServerConfig { Roots = [_copy], MemoryMinutes = 0 };
        var disk = new LocalDisk();
        _opening = new DocumentOpening(new SessionRoots(config, new StdioAddress(), disk), DocumentReader.Every, new DocumentMemory(config, TimeProvider.System), config, disk, TimeProvider.System);
    }

    [GlobalCleanup]
    public void Cleanup() => Directory.Delete(_copy, recursive: true);

    /// <summary>A CSV export of two thousand rows opened from disk.</summary>
    [Benchmark]
    public async Task<int> Csv() => (await _opening.OpenAsync("export.csv")).Document!.Text.Length;

    /// <summary>A Word report of twenty sections opened from disk.</summary>
    [Benchmark]
    public async Task<int> Word() => (await _opening.OpenAsync("report.docx")).Document!.Text.Length;
}
