using Bit.BevTools.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;

// The tests run one at a time: each boots WebAssembly in a browser context of its own, and two boots side by side on a
// two-core runner turn "the panel has opened by now" into a flake.
[assembly: DoNotParallelize]

namespace Bit.BevTools.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot - and a download of Bit.BevTools.wasm on top - routinely takes more than Playwright's 5
        // second default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the launch is not charged to that test's time. Nothing
        // throws: a browser that is missing is reported by every test (see BevToolsTest).
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
