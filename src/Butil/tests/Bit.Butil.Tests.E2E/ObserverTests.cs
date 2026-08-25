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

    [TestMethod]
    public async Task MutationObserver_Fires_On_Attribute_Change()
    {
        await ClickAndExpectAsync("mutation-observe", "mutation:True");
    }
}
