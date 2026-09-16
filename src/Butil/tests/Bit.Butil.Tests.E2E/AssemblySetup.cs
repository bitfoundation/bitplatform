using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Fixtures run in parallel with each other; the tests inside one run in order. Every test opens a page of its
// own - a browser context against a shared web host, or a hybrid window rented from a pool - so what limits the
// run is how many of those fit at once rather than anything the tests share. Workers = 0 lets the runner pick,
// which is the processor count.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Session-wide setup: the WebSocket echo endpoint the harness talks to, a build of the harness host the run
/// drives (<c>BUTIL_E2E_HOST</c>), and that host started once for every fixture.
/// </summary>
[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        // The WebSocket harness needs a socket endpoint, and the standalone sample has no server side to add one
        // to. Hosted here for every host, so the tests assert on one endpoint whichever host serves the page.
        await WebSocketEchoFixture.Start();

        await HarnessBuild.EnsureBuiltAsync();
        await HarnessTarget.StartAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await HarnessTarget.StopAllAsync();
        await PlaywrightSession.StopAsync();
        await WebSocketEchoFixture.Stop();
    }
}
