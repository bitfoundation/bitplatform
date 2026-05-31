using Bit.Butil.E2ETests.Infrastructure;
using NUnit.Framework;

namespace Bit.Butil.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class ObserverTests : ButilObserversPageTest
{
    [Test]
    public async Task IntersectionObserver_Fires_For_OnScreen_Target()
    {
        await ClickAndExpectAsync("intersection-observe", "intersection:True");
    }

    [Test]
    public async Task ResizeObserver_Fires_On_Initial_Observe()
    {
        // ResizeObserver delivers an initial entry on observe, so we don't even need to resize.
        await ClickAndExpectAsync("resize-observe", "resize:observed:True");
    }

    [Test]
    public async Task ResizeObserver_Trigger_Changes_Target_Width()
    {
        await Page.Locator("#resize-observe").ClickAsync();
        await ClickAndExpectAsync("resize-trigger", "resize:triggered:");
    }

    [Test]
    public async Task MutationObserver_Fires_On_Attribute_Change()
    {
        await ClickAndExpectAsync("mutation-observe", "mutation:True");
    }
}
