using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class ObserverTests : ButilObserversPageTest
{
    [TestMethod]
    public async Task IntersectionObserver_Fires_For_OnScreen_Target()
    {
        await ClickAndExpectAsync("intersection-observe", "intersection:True");
    }

    [TestMethod]
    public async Task ResizeObserver_Fires_On_Initial_Observe()
    {
        // ResizeObserver delivers an initial entry on observe, so we don't even need to resize.
        await ClickAndExpectAsync("resize-observe", "resize:observed:True");
    }

    [TestMethod]
    public async Task ResizeObserver_Trigger_Changes_Target_Width()
    {
        await Page.Locator("#resize-observe").ClickAsync();
        await ClickAndExpectAsync("resize-trigger", "resize:triggered:");
    }

    /// <summary>
    /// A rate-limited observer still reports the size the element settled at.
    /// </summary>
    /// <remarks>
    /// The harness makes five resizes inside one interval, so every one of them is suppressed by the
    /// gate. What has to arrive anyway is the last: the status carries both whether it did and the
    /// width the handler ended up holding, so a regression that silently pinned the handler to a
    /// stale size fails here with the stale number in the message rather than as a bare False.
    /// <br/>
    /// How much traffic the gate removes is measured in Bit.Butil.Tests.Benchmarks; this is the half
    /// of the claim that has to hold on every run.
    /// </remarks>
    [TestMethod]
    public async Task ResizeObserver_RateLimited_Still_Delivers_The_Settled_Size()
    {
        // 228 rather than 230: the reported size is the content box, and the target has a 1px border.
        await ClickAndExpectAsync("resize-observe-gated", "resize:gated:True:228");
    }

    [TestMethod]
    public async Task MutationObserver_Fires_On_Attribute_Change()
    {
        await ClickAndExpectAsync("mutation-observe", "mutation:True");
    }
}
