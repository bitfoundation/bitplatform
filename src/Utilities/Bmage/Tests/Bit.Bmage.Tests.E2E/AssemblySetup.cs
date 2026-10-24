using Bit.Bmage.Tests.E2E.Infrastructure;

// One test at a time. The suite drives a Debug WebAssembly build - Bmage's pixel loops on the IL
// interpreter, which is CPU-bound - and several tests measure what happens after a fixed settling
// delay or drag to pixel positions; running them side by side turns CPU contention into flakiness
// rather than into speed. Point BMAGE_E2E_HOST_URL at an AOT publish for a fast run.
[assembly: DoNotParallelize]

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// Session-wide setup. The hosts - the demo, the hosting samples, the MAUI app - are each built and started the first
/// time a test needs them and stopped here at the end, so a run filtered to one of them never builds the others.
/// </summary>
[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        // A WebAssembly boot of a Debug build, prerender hydration, a capability probe: routinely more than
        // Playwright's 5 second default on a CI runner, without anything being wrong.
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(30_000);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await MauiSampleApp.StopAsync();
        await PlaywrightSession.StopAsync();
        await DemoHost.StopAsync();
        await SampleHosts.StopAsync();
    }
}
