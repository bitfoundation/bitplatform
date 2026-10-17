using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// The tests run one at a time. A page booting WebAssembly in the next context over is exactly the kind of
// load that turns a wait on a background flush into a flake on a two-core runner.
[assembly: DoNotParallelize]

namespace Bit.BlazeDb.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot and the first open of a database routinely take more than Playwright's 5 second
        // default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the boot is not charged to that test's time. Nothing
        // throws: a browser or a demo that is missing is reported by every test (see BlazeDbTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
    }
}
