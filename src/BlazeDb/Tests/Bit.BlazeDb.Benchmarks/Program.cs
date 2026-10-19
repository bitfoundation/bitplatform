using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bit.BlazeDb.Benchmarks.ReadBenchmarks).Assembly).Run(args);
