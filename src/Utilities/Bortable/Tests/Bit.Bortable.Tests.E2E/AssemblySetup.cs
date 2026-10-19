using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// The tests run one at a time. Every one of them drives a native drag whose dragover events are handled in .NET a
// moment after the browser fires them, and a page booting WebAssembly in the next context over is exactly the kind
// of load that makes a handler miss the last pointer position of a drag on a two-core runner.
[assembly: DoNotParallelize]

namespace Bit.Bortable.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot routinely takes more than Playwright's 5 second default on a CI runner, without
        // anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the boot is not charged to that test's time. Nothing
        // throws: a browser or a demo that is missing is reported by every test (see BortableTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
    }
}
