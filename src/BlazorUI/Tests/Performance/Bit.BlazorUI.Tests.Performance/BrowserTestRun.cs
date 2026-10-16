using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Stops what the browser tests of a run share - the test host and the browser - once the whole run is over.
/// </summary>
[TestClass]
public sealed class BrowserTestRun
{
    [AssemblyCleanup]
    public static Task Cleanup() => PerformanceTestBase.ShutdownAsync();
}
