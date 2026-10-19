using Bit.BbScanner.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// The tests run one at a time. Every one of them launches a browser that plays a video into a fake camera and
// waits for the decoder to keep up with it in real time; a second browser booting WebAssembly next to it is
// exactly the kind of load that turns those waits into flakes on a two-core runner.
[assembly: DoNotParallelize]

namespace Bit.BbScanner.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot and the first camera frame routinely take more than Playwright's 5 second default
        // on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the boot is not charged to that test's time. Nothing
        // throws: a browser or a demo that is missing is reported by every test (see BbScannerTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
    }
}
