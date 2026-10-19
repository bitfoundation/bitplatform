using Bit.Bmotion.Tests.E2E.Infrastructure;

// Fixtures run in parallel with each other; the tests inside one run in order. Every test opens a browser context
// of its own against a shared host, so what limits the run is how many of those fit at once rather than anything
// the tests share. Animations are timing-sensitive, so the worker count is capped rather than left at the
// processor count: a starved browser drops frames, and a test measuring an animation would measure the starving.
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.ClassLevel)]

namespace Bit.Bmotion.Tests.E2E;

/// <summary>Session-wide setup: a build of the harness host, and the default host started once for every fixture.</summary>
[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        await HarnessBuild.EnsureBuiltAsync();
        await HarnessTarget.StartAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await HarnessTarget.StopAllAsync();
        await PlaywrightSession.StopAsync();
    }
}
