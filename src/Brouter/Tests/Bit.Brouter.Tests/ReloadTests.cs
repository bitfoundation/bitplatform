using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// <see cref="IBrouter.ReloadAsync"/>: rebuilding the current chain from scratch. The case neither
/// Brouter nor the built-in Router can express with a navigation - re-navigating to the URL the
/// user is already on reuses the component, so nothing that ran in <c>OnInitialized</c> runs again.
/// </summary>
[TestClass]
public class ReloadTests : BunitTestContext
{
    [TestMethod]
    public async Task Reload_recreates_the_current_pages_component_instance()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public async Task Re_navigating_to_the_same_url_keeps_the_instance_but_reload_replaces_it()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();

        // Framework parity baseline: a navigation to the URL already on screen matches the same
        // route, so the component (and its state) survives - this is what makes ReloadAsync
        // necessary in the first place.
        await cut.InvokeAsync(() => brouter.Navigate("/plain", replace: true));
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent));

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public async Task Reload_replaces_the_visible_instance_of_a_keep_alive_route()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();

        // ClearKeepAlive only evicts RETAINED (hidden) instances; the one on screen is exactly the
        // one it leaves alone, so it can't refresh the page the user is looking at.
        await cut.InvokeAsync(brouter.ClearKeepAlive);
        cut.WaitForAssertion(() => Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent));

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
            // Rebuilt visible, not left behind in the hidden keep-alive wrapper.
            Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count);
        });
    }

    [TestMethod]
    public async Task Reload_leaves_retained_instances_of_other_routes_alone()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();

        await cut.InvokeAsync(() => brouter.Navigate("/plain"));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("div[hidden] [data-testid=stateful]")));

        // The reload rebuilds the CURRENT chain (/plain); the hidden /ka instance is not part of it.
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        await cut.InvokeAsync(() => brouter.Navigate("/ka"));
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public async Task Reload_fires_a_disposing_deactivation_then_a_fresh_activation()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/lifecycle");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "activated:first=True"));

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            // The old instance was genuinely torn down (Disposing, not Hidden) and the replacement
            // reports a first activation - a rebuilt component, not a renavigated one.
            CollectionAssert.Contains(log, "deactivated:Disposing");
            Assert.AreEqual(2, log.FindAll(e => e == "activated:first=True").Count);
            Assert.AreEqual(0, log.FindAll(e => e.StartsWith("renavigated:")).Count);
        });
    }

    [TestMethod]
    public async Task Reload_rebuilds_the_whole_chain_including_outlet_hosted_content()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/data/view");
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-1")));

        // The layout (parent route content) holds state too; both it and the outlet-hosted child
        // must come back fresh.
        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
            Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-2"));
        });
    }

    [TestMethod]
    public async Task Reload_reruns_guards_and_loaders_bypassing_the_loader_cache()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/data/view");
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.Instance.ChildLoaderRuns));
        Assert.AreEqual(1, cut.Instance.GuardRuns);
        Assert.IsFalse(cut.Instance.LastLoadWasReload!.Value);

        // Both routes cache for 5 minutes, so a round-trip navigation re-serves the cached results.
        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        await cut.InvokeAsync(() => brouter.Navigate("/data/view"));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=child-data]")));
        Assert.AreEqual(1, cut.Instance.ChildLoaderRuns);
        Assert.AreEqual(1, cut.Instance.ParentLoaderRuns);

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            // Rebuilt instances must not be handed the data the discarded ones were showing.
            Assert.AreEqual(2, cut.Instance.ChildLoaderRuns);
            Assert.AreEqual(2, cut.Instance.ParentLoaderRuns);
            Assert.IsTrue(cut.Instance.LastLoadWasReload!.Value);
            // The chain is matched from nothing, so guards re-run (unlike RevalidateAsync).
            Assert.AreEqual(3, cut.Instance.GuardRuns);
        });
    }

    [TestMethod]
    public async Task Reload_does_not_touch_the_url_or_fire_navigation_hooks()
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

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));

        Assert.AreEqual(uri, nav.Uri);
        Assert.AreEqual(0, locationChanges);
        Assert.AreEqual(0, navigating);
        Assert.AreEqual(0, navigated);
    }

    [TestMethod]
    public async Task Reload_with_no_matched_chain_is_a_noop()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/nowhere");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count));

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count);
    }
}
