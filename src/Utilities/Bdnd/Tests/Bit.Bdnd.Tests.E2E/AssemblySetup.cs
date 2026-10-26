using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// The tests run one at a time. A drag is a sequence of native events whose handlers round-trip into .NET between
// them, and a page booting WebAssembly in the next context over is exactly the kind of load that turns "the hover
// has been rendered by now" into a flake on a two-core runner.
[assembly: DoNotParallelize]

namespace Bit.Bdnd.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot routinely takes more than Playwright's 5 second default on a CI runner, without
        // anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // The Blazor Hybrid harness is built before anything runs - an incremental build when it is up to date - so
        // its copy of the library is the current one, and no build rewrites the module under the running demo.
        // A failed build is reported by the hybrid tests.
        await HybridHarnessHost.PrepareAsync();

        // Started here rather than by the first test, so the boot is not charged to that test's time. Nothing
        // throws: a browser or a demo that is missing is reported by every test (see BdndTest).
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
            await HybridSession.StopAsync();
        }
    }
}
