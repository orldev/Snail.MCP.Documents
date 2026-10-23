using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Snail.MCP.Documents.Benchmarks.OpeningBenchmarks).Assembly).Run(args);
