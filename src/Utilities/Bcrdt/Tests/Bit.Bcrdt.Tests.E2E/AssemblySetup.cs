using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// The tests run one at a time. Most of them race an edit in one replica against its arrival in another, or a
// reconnect against the handshake that follows it, and a page booting WebAssembly in the next context over is
// exactly the kind of load that turns those waits into flakes on a two-core runner. The live tests also share the
// demo server's relay, which is easier to reason about with one room in use at a time.
[assembly: DoNotParallelize]

namespace Bit.Bcrdt.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // A WebAssembly boot, or a reconnect and the handshake it starts, routinely takes more than Playwright's
        // 5 second default on a CI runner, without anything being wrong.
        Assertions.SetDefaultExpectTimeout(20_000);

        // Started here rather than by the first test, so the boot is not charged to that test's time. Nothing
        // throws: a browser or a demo that is missing is reported by every test (see BcrdtTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
    }
}
