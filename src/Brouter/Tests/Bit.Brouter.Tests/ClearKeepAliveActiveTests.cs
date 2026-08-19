using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// <c>IBrouter.ClearKeepAlive(includeActive: true)</c>: the eviction that also throws away the page
/// on screen and rebuilds it in place - no navigation, no pipeline, so guards and loaders stay put
/// (the pipeline-running variant is <see cref="ReloadTests"/>).
/// </summary>
[TestClass]
public class ClearKeepAliveActiveTests : BunitTestContext
{
    [TestMethod]
    public async Task IncludeActive_rebuilds_the_visible_keep_alive_page()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();

        // The plain overload leaves the visible instance alone...
        await cut.InvokeAsync(() => brouter.ClearKeepAlive());
        cut.WaitForAssertion(() => Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent));

        // ...includeActive is what replaces it.
        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
            // Rebuilt visible, not stranded in the hidden keep-alive wrapper.
            Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count);
        });
    }

    [TestMethod]
    public async Task IncludeActive_rebuilds_a_transient_page_too()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();

        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public async Task IncludeActive_rebuilds_outlet_hosted_content_inside_its_layout()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/parent/k1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();

        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        cut.WaitForAssertion(() =>
        {
            // The hosting layout is back with the child mounted inside it, rebuilt.
            Assert.IsNotNull(cut.Find("[data-testid=playout] [data-testid=stateful]"));
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
        });
    }

    [TestMethod]
    public async Task IncludeActive_keeps_the_loaded_data_and_does_not_rerun_guards_or_loaders()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/data/view");
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-1")));
        cut.Find("[data-testid=inc]").Click();

        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        cut.WaitForAssertion(() =>
        {
            // Instances rebuilt (layout counter reset)...
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
            // ...but this is not a navigation: the chain keeps the data it already loaded.
            Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-1"));
        });

        Assert.AreEqual(1, cut.Instance.ParentLoaderRuns);
        Assert.AreEqual(1, cut.Instance.ChildLoaderRuns);
        Assert.AreEqual(1, cut.Instance.GuardRuns);
    }

    [TestMethod]
    public async Task IncludeActive_fires_a_disposing_deactivation_then_a_fresh_activation()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/lifecycle");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "activated:first=True"));

        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        cut.WaitForAssertion(() =>
        {
            CollectionAssert.Contains(log, "deactivated:Disposing");
            Assert.AreEqual(2, log.FindAll(e => e == "activated:first=True").Count);
            Assert.AreEqual(0, log.FindAll(e => e.StartsWith("renavigated:")).Count);
        });
    }

    [TestMethod]
    public async Task IncludeActive_does_not_touch_the_url_or_fire_navigation_hooks()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        var navigating = 0;
        var navigated = 0;
        var locationChanges = 0;
        brouter.OnNavigating += _ => { navigating++; return ValueTask.CompletedTask; };
        brouter.OnNavigated += _ => { navigated++; return ValueTask.CompletedTask; };
        nav.LocationChanged += (_, _) => locationChanges++;
        var uri = nav.Uri;

        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));

        Assert.AreEqual(uri, nav.Uri);
        Assert.AreEqual(0, locationChanges);
        Assert.AreEqual(0, navigating);
        Assert.AreEqual(0, navigated);
    }

    [TestMethod]
    public async Task IncludeActive_also_drops_the_retained_pages_of_other_routes()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();

        await cut.InvokeAsync(() => brouter.Navigate("/plain"));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("div[hidden] [data-testid=stateful]")));

        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count));

        // The retained /ka instance really was disposed: returning recreates it.
        await cut.InvokeAsync(() => brouter.Navigate("/ka"));
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public async Task IncludeActive_with_nothing_matched_is_a_noop()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/nowhere");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count));

        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count);
    }
}
