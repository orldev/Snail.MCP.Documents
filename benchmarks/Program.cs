using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Snail.MCP.Documents.Benchmarks.ReadingBenchmarks).Assembly).Run(args);
