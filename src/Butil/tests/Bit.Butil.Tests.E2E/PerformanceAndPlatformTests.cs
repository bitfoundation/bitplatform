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
    public async Task Typed_PerformanceObserver_Deserializes_Its_Entries()
    {
        // The measure is produced on demand, so this proves the typed relay rather than depending
        // on the browser happening to be slow.
        await ClickAndExpectAsync("perf-typed-observer", "perf:typed-observer:True");
    }

    [TestMethod]
    public async Task NavigationTiming_Comes_Back_Typed()
    {
        await ClickAndExpectAsync("perf-navigation", "perf:navigation:True");
    }

    [TestMethod]
    public async Task ResourceTiming_Comes_Back_Typed()
    {
        await ClickAndExpectAsync("perf-resources", "perf:resources:True");
    }

    [TestMethod]
    public async Task WebVitals_Reports_Ttfb_Without_Any_Interaction()
    {
        // LCP, CLS and INP all depend on what the page did and what the user did; TTFB comes from
        // the navigation entry, so it is the one metric a headless run can assert on.
        await ClickAndExpectAsync("perf-vitals", "perf:vitals:True");
    }

    [TestMethod]
    public async Task IsInputPending_Answers_On_Every_Engine()
    {
        // The value itself is whatever the input queue happens to hold; what matters is that a
        // browser without navigator.scheduling reports false instead of throwing.
        await ClickAndExpectAsync("nav-input-pending", "nav:input-pending:ok:True/True");
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
