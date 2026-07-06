using Bit.Butil.E2ETests.Infrastructure;
using NUnit.Framework;

namespace Bit.Butil.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class PerformanceAndPlatformTests : ButilObserversPageTest
{
    [Test]
    public async Task Performance_Mark_And_Measure_Produces_An_Entry()
    {
        await ClickAndExpectAsync("perf-mark-measure", "perf:measure:True");
    }

    [Test]
    public async Task PerformanceObserver_Reports_A_Mark()
    {
        await ClickAndExpectAsync("perf-observer", "perf:observer:True");
    }

    [Test]
    public async Task StorageManager_Estimate_Reports_A_Quota()
    {
        await ClickAndExpectAsync("storage-estimate", "storage:estimate:True");
    }

    [Test]
    public async Task NetworkInformation_Reports_Online()
    {
        await ClickAndExpectAsync("network-status", "network:online:True");
    }
}
