using BenchmarkDotNet.Attributes;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;
using Snail.MCP.Documents.Domain;

namespace Snail.MCP.Documents.Benchmarks;

/// <summary>What documents_read does with a file once it is in memory: the format read into blocks, the blocks into Markdown.</summary>
[MemoryDiagnoser]
public class ReadingBenchmarks
{
    private readonly WordReader _word = new();

    private readonly ExcelReader _excel = new();

    private byte[] _report = [];

    private byte[] _ledger = [];

    [GlobalSetup]
    public void Setup()
    {
        _report = Files.Report();
        _ledger = Files.Ledger();
    }

    /// <summary>A Word report of twenty sections read as Markdown.</summary>
    [Benchmark]
    public int Word()
    {
        using var content = new MemoryStream(_report);

        return Markdown.Of(DocumentKind.Word, _word.Read(content)).Text.Length;
    }

    /// <summary>A workbook of three sheets of four hundred rows read as Markdown.</summary>
    [Benchmark]
    public int Excel()
    {
        using var content = new MemoryStream(_ledger);

        return Markdown.Of(DocumentKind.Excel, _excel.Read(content)).Text.Length;
    }
}
