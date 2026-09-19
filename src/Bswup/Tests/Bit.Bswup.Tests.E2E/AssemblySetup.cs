using Bit.Bswup.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// Test classes run in parallel with each other; the tests inside one class run in order. Every test runs
// on an origin of its own (its own service worker registrations and caches) in a browser context of its
// own, against the host process shared by its render mode, so what limits the run is how many contexts
// can download a WebAssembly runtime at once.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace Bit.Bswup.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A first install downloads and caches a whole Debug WebAssembly app before Blazor starts: routinely
        // more than Playwright's 5 second default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(30_000);

        await HarnessBuild.EnsureBuiltAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await HarnessHosts.StopAllAsync();
        await PlaywrightSession.StopAsync();
    }
}
