using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Once the whole run is over, writes what its performance tests measured (while the host they measured is still up to
/// say what it is), then stops what the browser tests of the run share - the test host and the browser.
/// </summary>
[TestClass]
public sealed class BrowserTestRun
{
    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        try
        {
            await PerformanceResults.WriteAsync(PerformanceTestBase.HostUrl);
        }
        finally
        {
            await PerformanceTestBase.ShutdownAsync();
        }
    }
}
