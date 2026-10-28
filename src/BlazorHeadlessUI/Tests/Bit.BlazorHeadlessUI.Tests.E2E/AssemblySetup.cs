using Bit.BlazorHeadlessUI.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

// Every test opens a browser context of its own against the shared demo and browser, so the tests run side by side.
// Capped rather than left at the processor count: each test boots a WebAssembly runtime (or opens a circuit), and a
// starved page turns the waits for a render into flakes. Four is what a GitHub-hosted runner has cores for.
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]

namespace Bit.BlazorHeadlessUI.Tests.E2E;

[TestClass]
public static class AssemblySetup
{
    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        Assertions.SetDefaultExpectTimeout(DemoTest.ExpectTimeout);

        // Started here rather than by the first test, so the publish and the boot are not charged to that test's time.
        // Nothing throws: a browser or a demo that is missing is reported by every test (see DemoTest).
        await PlaywrightSession.GetAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await PlaywrightSession.StopAsync();
    }
}
