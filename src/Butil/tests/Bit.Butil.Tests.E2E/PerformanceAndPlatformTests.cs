using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class PerformanceAndPlatformTests : ButilObserversPageTest
{
    [TestMethod]
    public async Task Performance_Mark_And_Measure_Produces_An_Entry()
    {
        await ClickAndExpectAsync("perf-mark-measure", "perf:measure:True");
    }

    [TestMethod]
    public async Task PerformanceObserver_Reports_A_Mark()
    {
        await ClickAndExpectAsync("perf-observer", "perf:observer:True");
    }

    [TestMethod]
    public async Task StorageManager_Estimate_Reports_A_Quota()
    {
        await ClickAndExpectAsync("storage-estimate", "storage:estimate:True");
    }

    [TestMethod]
    public async Task NetworkInformation_Reports_Online()
    {
        await ClickAndExpectAsync("network-status", "network:online:True");
    }
}
