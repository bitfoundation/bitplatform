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

    [TestMethod]
    public async Task IncludeActive_yields_to_a_navigation_started_from_the_torn_down_routes_teardown()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/teardown-nav");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "activated"));
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        // The probe's Disposing deactivation navigates to /teardown-nav-target, whose async loader
        // keeps that navigation in flight (started, not yet committed). It owns the screen
        // from then on: the reset must not re-mount /teardown-nav behind it (a fresh instance the
        // navigation would immediately depart, and a stale staged activation for it).
        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(nav.Uri.EndsWith("/teardown-nav-target"), nav.Uri);
            Assert.AreEqual(1, cut.FindAll("[data-testid=teardown-nav-target]").Count);
            Assert.AreEqual(0, cut.FindAll("[data-testid=teardown-nav]").Count);
            Assert.AreEqual(1, log.FindAll(e => e == "activated").Count);
        });
    }

    [TestMethod]
    public void IncludeActive_rebuilds_the_page_when_called_from_a_ui_event_handler()
    {
        var (cut, _) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent));

        // The sibling tests all drive the API through InvokeAsync, where each render request
        // flushes on its own. Inside a UI event handler Blazor is mid-batch, so the unmatch and the
        // re-mount collapse into a SINGLE render - and a render that sees the route matched both
        // before and after has nothing to tell the diff unless the content is keyed by its session.
        // Without that key the subtree diffs as unchanged and the instance the caller asked to
        // throw away silently survives. This is the path the documented use cases actually take.
        cut.Find("[data-testid=clear-active]").Click();

        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public void IncludeActive_from_a_ui_event_handler_rebuilds_keep_alive_content_too()
    {
        var (cut, _) = RenderAt<ReloadHost>("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent));

        cut.Find("[data-testid=clear-active]").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
            // Rebuilt visible, not stranded in the hidden keep-alive wrapper.
            Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count);
        });
    }

    [TestMethod]
    public void IncludeActive_from_a_ui_event_handler_gives_the_rebuilt_instance_a_live_lifecycle()
    {
        var (cut, _) = RenderAt<ReloadHost>("http://localhost/lifecycle");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "activated:first=True"));
        log.Clear();

        cut.Find("[data-testid=clear-active]").Click();

        // The old instance is disposed and its replacement gets a fresh activation. A survivor that
        // merely LOOKS rebuilt is the real hazard: it would keep a registration bound to the dead
        // session and never receive another lifecycle callback - silently disabling its
        // OnDeactivating navigation lock / unsaved-changes guard for the rest of its life.
        cut.WaitForAssertion(() =>
        {
            CollectionAssert.Contains(log, "deactivated:Disposing");
            CollectionAssert.Contains(log, "activated:first=True");
        });
    }

    [TestMethod]
    public void IncludeActive_tears_a_nested_chain_down_child_first()
    {
        var (cut, _) = RenderAt<ReloadHost>("http://localhost/nested/child");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "child:activated:first=True"));
        log.Clear();

        cut.Find("[data-testid=clear-active]").Click();

        // Child before parent, matching ReloadAsync, a real navigation's departures and disposal
        // itself. Sweeping registered routes instead would invert it - registration is
        // parent-before-child - and a page whose Disposing handler flushes through context its
        // layout provides would run after that layout was already torn down.
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "parent:deactivated:Disposing"));
        var child = log.IndexOf("child:deactivated:Disposing");
        var parent = log.IndexOf("parent:deactivated:Disposing");
        Assert.IsTrue(child >= 0, $"no child deactivation; log: {string.Join(" | ", log)}");
        Assert.IsTrue(child < parent, $"child must deactivate before parent; log: {string.Join(" | ", log)}");
    }

    [TestMethod]
    public void IncludeActive_from_a_ui_event_handler_rebuilds_outlet_hosted_content()
    {
        var (cut, _) = RenderAt<ReloadHost>("http://localhost/outlet/t");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => cut.Find("[data-testid=olayout] [data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent));
        log.Clear();

        // The outlet's TRANSIENT path, from a UI event handler: the sibling outlet test above goes
        // through /parent/k1 (keep-alive, so the retained-entries region renders it) and drives the
        // API through InvokeAsync, where each render request flushes on its own. Here the drop and
        // the re-mount are requested inside one event handler, and the content is merely the
        // current match rather than a retained entry - the combination the documented use cases
        // actually take for a page that lives in a layout's outlet.
        cut.Find("[data-testid=clear-active]").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.FindAll("[data-testid=olayout] [data-testid=stateful]").Count);
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
            CollectionAssert.Contains(log, "deactivated:Disposing");
            CollectionAssert.Contains(log, "activated:first=True");
        });
    }

    [TestMethod]
    public async Task IncludeActive_leaves_a_navigation_waiting_in_its_guard_alone()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        // A navigation parked in its async enter guard has not started its pipeline yet, so the
        // in-flight counter the sibling test relies on is still 0 - but the pipeline (and the
        // Matched reset that makes re-mounting here unsafe) is one continuation away. The
        // includeActive half stands down for the decision phase too; the hidden-content half still
        // runs, which is what makes the downgrade a downgrade rather than a bail-out.
        Task<BrouterNavigationOutcome> pending = null!;
        await cut.InvokeAsync(() => { pending = brouter.NavigateAsync("/slow-guard").AsTask(); });
        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => cut.Instance.GuardGate.SetResult());
        var outcome = await pending;

        Assert.AreEqual(BrouterNavigationStatus.Succeeded, outcome.Status);
        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(nav.Uri.EndsWith("/slow-guard"), nav.Uri);
            Assert.AreEqual(1, cut.FindAll("[data-testid=slow-guard]").Count);
            // The page the navigation left is gone, not rebuilt by the reset and stranded beside it.
            Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count);
        });
    }

    [TestMethod]
    public async Task IncludeActive_leaves_an_already_in_flight_navigation_alone()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        // Start a navigation whose async loader keeps it in flight, then reset while it runs. The
        // pipeline has already cleared every route's Matched flag and owns the screen until it
        // commits; re-mounting the committed chain here would mark the departing routes matched
        // again, so at commit they would be re-rendered instead of unmounted and the old page would
        // stay on screen beside the new one, subscriptions and all.
        await cut.InvokeAsync(() => brouter.Navigate("/teardown-nav-target"));
        await cut.InvokeAsync(() => brouter.ClearKeepAlive(includeActive: true));

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(nav.Uri.EndsWith("/teardown-nav-target"), nav.Uri);
            Assert.AreEqual(1, cut.FindAll("[data-testid=teardown-nav-target]").Count);
            // The page the navigation is leaving must be gone, not resurrected next to its successor.
            Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count);
        });
    }
}
