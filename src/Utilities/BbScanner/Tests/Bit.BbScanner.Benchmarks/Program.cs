using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Bit.BbScanner.Benchmarks;

// --budget: the quick performance gate CI runs; --update-budgets rewrites its baseline. See BudgetCheck.
if (args.Contains("--budget") || args.Contains("--update-budgets"))
{
    return BudgetCheck.Run(update: args.Contains("--update-budgets"));
}

// A quick correctness pass before measuring: a benchmark that decodes nothing measures nothing.
Verification.Run();

BenchmarkSwitcher.FromAssembly(typeof(DecoderBenchmarks).Assembly)
    .Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator));

return 0;
