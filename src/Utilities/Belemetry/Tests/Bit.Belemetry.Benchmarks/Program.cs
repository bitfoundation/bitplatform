using BenchmarkDotNet.Running;

namespace Bit.Belemetry.Benchmarks;

/// <summary>The benchmark host.</summary>
/// <remarks>
/// A switcher rather than a single runner, so the suite can be sliced from the command line without recompiling:
/// <c>--filter "*Privacy*"</c> runs one class, <c>--job Dry</c> proves the harness works without paying for
/// statistics, and <c>--list flat</c> enumerates everything available.
/// </remarks>
public static class Program
{
    /// <summary>Runs the benchmarks selected by <paramref name="args"/>.</summary>
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
