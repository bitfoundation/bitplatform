using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// Runs TestPages/benchmarks.html per backend and prints what it measured: cold open after engine init, a
/// 100-row insert batch in one round trip, 1 000 repeated parameterised inserts inside one transaction (the
/// statement cache), a 1 000-row read on the binary path and on the tagged JavaScript path, and an indexed
/// point read.
/// </summary>
/// <remarks>
/// The numbers are for a human comparing backends and encodings on one machine, not for a red build, so
/// nothing is asserted about them and the tests only run with <c>BSQLITE_E2E_BENCHMARKS=1</c>. The page also
/// works by hand: its console exposes <c>await run('opfs')</c> and the rest.
/// </remarks>
[TestClass]
public sealed class BenchmarkTests : BsqliteTest
{
    [TestMethod]
    [TestCategory("Benchmark")]
    [DataRow("memory")]
    [DataRow("opfs")]
    [DataRow("indexeddb")]
    [DataRow("cache")]
    public async Task Benchmark(string backend)
    {
        SkipWhen(E2EEnvironment.Benchmarks is false, "the benchmarks are a manual read, not a gate; set BSQLITE_E2E_BENCHMARKS=1 to run them");
        if (backend == "opfs") await SkipUnlessOpfsAsync(Page, "/benchmarks.html");

        await Page.GotoAsync("/benchmarks.html");
        await Page.WaitForFunctionAsync("() => typeof globalThis.run === 'function'");

        var row = await Page.EvaluateAsync<JsonElement>("backend => globalThis.run(backend)", backend);

        foreach (var measurement in row.EnumerateObject())
        {
            TestContext.WriteLine($"{measurement.Name}: {measurement.Value}");
        }
    }
}
