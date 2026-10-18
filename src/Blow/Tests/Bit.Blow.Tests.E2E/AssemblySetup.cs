using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// The tests run one at a time. Most of them measure where something landed on screen a moment after a gesture,
// or race a pointer against the render a press triggers, and a page booting WebAssembly in the next context over
// is exactly the kind of load that turns those waits into flakes on a two-core runner.
[assembly: DoNotParallelize]

namespace Bit.Blow.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot and the first measure-and-fit of a flow routinely take more than Playwright's 5 second
        // default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the boot is not charged to that test's time. Nothing
        // throws: a browser or a demo that is missing is reported by every test (see BlowTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
    }
}
