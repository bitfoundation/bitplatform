using Bit.Brouter.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// Test classes run in parallel with each other; the tests inside one class run in order. Each web mode
// class talks to its own host process and each test to its own browser context, so what limits the run
// is how many contexts fit at once. The hybrid class drives the single page of its app window, which is
// why ordering within a class matters.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace Bit.Brouter.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // Hydration, a WebAssembly boot in Debug, a circuit round-trip: routinely more than Playwright's
        // 5 second default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        await HarnessBuild.EnsureBuiltAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await HybridSession.StopAsync();
        await HarnessHosts.StopAllAsync();
        await PlaywrightSession.StopAsync();
    }
}
