using Microsoft.VisualStudio.TestTools.UnitTesting;

// Fixtures run in parallel with each other; the tests inside one run in order. That is what the
// [Parallelizable(ParallelScope.Self)] on each fixture used to say, expressed once for the assembly:
// every test launches a browser of its own against a read-only demo app, so what limits the run is
// how many browsers fit at once rather than anything the tests share. Workers = 0 lets the runner
// pick, which is the processor count.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

// NOTE: deliberately in the assembly's root test namespace (not .Infrastructure), next to the
// fixtures whose base URL it sets.
namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Boots <c>Bit.Butil.Samples.Web</c> for the duration of the test session and exposes the URL test
/// fixtures should hit. Reuses an externally-running server when <c>BUTIL_E2E_BASE_URL</c> is set so
/// CI can hand-roll the boot if it wants. The boot itself is <see cref="Infrastructure.SampleAppHost"/>,
/// which the benchmark suite shares.
/// </summary>
[TestClass]
public class DemoServerFixture
{
    public static string BaseUrl { get; private set; } = string.Empty;

    // Static because MSTest's assembly-level hooks are: the process outlives every test instance.
    private static Infrastructure.SampleAppHost? _app;

    [AssemblyInitialize]
    public static async Task GlobalSetup(TestContext context)
    {
        // The WebSocket harness needs a socket endpoint, and the app under test is a standalone
        // WebAssembly host with no server side. MSTest allows one [AssemblyInitialize] per assembly,
        // so it is started from here rather than from a fixture of its own.
        await Infrastructure.WebSocketEchoFixture.Start();

        _app = await Infrastructure.SampleAppHost.Start(Environment.GetEnvironmentVariable("BUTIL_E2E_BASE_URL"));
        BaseUrl = _app.BaseUrl;
    }

    [AssemblyCleanup]
    public static async Task GlobalTeardown()
    {
        await Infrastructure.WebSocketEchoFixture.Stop();

        if (_app is null) return;
        await _app.DisposeAsync();
        _app = null;
    }
}
