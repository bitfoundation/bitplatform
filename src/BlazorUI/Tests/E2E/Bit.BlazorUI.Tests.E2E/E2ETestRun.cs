// Test classes run beside each other, each test on a browser context of its own against the one shared host and
// browser; the tests of one class run in order. A machine that cannot drive this many pages at once lowers the count
// through a runsettings file (MSTest/Parallelize/Workers), which overrides this attribute. Four keeps the Blazor Server
// host responsive: at eight, a loaded host times circuits out and drops fast keystrokes, failing a different handful
// of tests on every run.
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.ClassLevel)]

namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// Stops what the tests of a run share - the test host and the browser - once the whole run is over.
/// </summary>
[TestClass]
public sealed class E2ETestRun
{
    [AssemblyCleanup]
    public static Task Cleanup() => E2ETestBase.ShutdownAsync();
}
