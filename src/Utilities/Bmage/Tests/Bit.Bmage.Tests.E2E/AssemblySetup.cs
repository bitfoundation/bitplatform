using Bit.Bmage.Tests.E2E.Infrastructure;

// One test at a time. The suite drives a Debug WebAssembly build - Bmage's pixel loops on the IL
// interpreter, which is CPU-bound - and several tests measure what happens after a fixed settling
// delay or drag to pixel positions; running them side by side turns CPU contention into flakiness
// rather than into speed. Point BMAGE_E2E_HOST_URL at an AOT publish for a fast run.
[assembly: DoNotParallelize]

namespace Bit.Bmage.Tests.E2E;

/// <summary>Session-wide setup: a current build of the demo host, started once for every test.</summary>
[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot of a Debug build, prerender hydration, a capability probe: routinely more than
        // Playwright's 5 second default on a CI runner, without anything being wrong.
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(30_000);

        await DemoBuild.EnsureBuiltAsync();
        await DemoHost.BaseUrlAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
        await DemoHost.StopAsync();
    }
}
