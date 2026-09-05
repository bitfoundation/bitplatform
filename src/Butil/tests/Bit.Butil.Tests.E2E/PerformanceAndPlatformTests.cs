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

    /// <summary>
    /// The hardware buses (Bluetooth, USB, serial, HID, MIDI, sensors, pressure, posture, window
    /// management, local fonts, audio output) all reach a real device only through a chooser the
    /// user clicks, so the suite probes the part that needs neither: that every support check
    /// resolves and answers rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task Hardware_Support_Probes_All_Resolve()
    {
        await ClickAndExpectAsync("hw-support", "hw:support:ok");
    }

    /// <summary>
    /// The permissionless reads have one right answer on any browser: a machine that cannot fold
    /// is "continuous", and the listings come back empty rather than refusing.
    /// </summary>
    [TestMethod]
    public async Task Hardware_Permissionless_Reads_Answer()
    {
        await ClickAndExpectAsync("hw-reads", "hw:reads:Continuous:True/True/True");
    }
}
