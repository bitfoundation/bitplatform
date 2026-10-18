using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// The tests run one at a time. Many of them share an origin's storage on purpose (two tabs on one OPFS
// file, a worker killed mid-transaction and reopened), every one of them boots a worker and a WebAssembly
// engine, and a second test doing the same next to it is exactly the load that turns a wait into a flake
// on a two-core runner.
[assembly: DoNotParallelize]

namespace Bit.Bsqlite.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // Instantiating a WebAssembly engine in a fresh worker routinely takes longer than Playwright's
        // 5 second default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the boot is not charged to that test's time. Nothing
        // throws: a browser or assets that are missing are reported by every test (see BsqliteTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
    }
}
