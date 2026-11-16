using Bit.Bink.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;

// The tests run one at a time. Most of them measure pixels a moment after a gesture, or race a pointer against
// an animation frame, and a page booting WebAssembly in the next context over is exactly the kind of load that
// turns those waits into flakes on a two-core runner.
[assembly: DoNotParallelize]

namespace Bit.Bink.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot and the first render of a canvas routinely take more than Playwright's 5 second
        // default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the browser's launch is not charged to that test's time. The
        // demo is not: the first test that drives it publishes and starts it, so a run that drives none (the lazy
        // loading tests alone) never pays for it. Nothing throws: a browser or a demo that is missing is reported by
        // every test that needs it (see BinkTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        try
        {
            await PlaywrightSession.StopAsync();
        }
        finally
        {
            await LazyLoadingSample.StopAsync();
        }
    }
}
