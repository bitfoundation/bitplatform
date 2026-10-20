using BenchmarkDotNet.Running;

namespace Bit.Balette.Benchmarks;

/// <summary>
/// Entry point for the benchmark suite. Run everything with <c>dotnet run -c Release</c>, or a single set with
/// <c>dotnet run -c Release -- --filter *Keystroke*</c>.
/// </summary>
public static class Program
{
    /// <summary>Runs the benchmarks selected on the command line.</summary>
    public static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
