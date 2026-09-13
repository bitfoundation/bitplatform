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

    [TestMethod]
    public async Task Reload_yields_to_a_navigation_started_from_the_torn_down_routes_teardown()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/teardown-nav");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "activated"));
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        // The probe's Disposing deactivation navigates to /teardown-nav-target, whose async loader
        // keeps that navigation in flight (started, not yet committed). It owns the screen
        // from then on: the reload must not re-match /teardown-nav behind it (which would rebuild
        // the page the user just left and supersede their navigation).
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(nav.Uri.EndsWith("/teardown-nav-target"), nav.Uri);
            Assert.AreEqual(1, cut.FindAll("[data-testid=teardown-nav-target]").Count);
            Assert.AreEqual(0, cut.FindAll("[data-testid=teardown-nav]").Count);
            Assert.AreEqual(1, log.FindAll(e => e == "activated").Count);
        });
    }

    [TestMethod]
    public async Task Reload_tears_a_nested_chain_down_child_first()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/nested/child");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "child:activated:first=True"));
        log.Clear();

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        // The reference order the ClearKeepAlive(includeActive) sibling has to match.
        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "parent:deactivated:Disposing"));
        var child = log.IndexOf("child:deactivated:Disposing");
        var parent = log.IndexOf("parent:deactivated:Disposing");
        Assert.IsTrue(child >= 0, $"no child deactivation; log: {string.Join(" | ", log)}");
        Assert.IsTrue(child < parent, $"child must deactivate before parent; log: {string.Join(" | ", log)}");
    }

    [TestMethod]
    public async Task Reload_cancelled_by_a_guard_restores_the_page_instead_of_blanking_it()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/data/view");
        cut.WaitForAssertion(() => cut.Find("[data-testid=layout]"));
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        // The reload tears the chain down BEFORE the pipeline runs, so a guard that then cancels
        // would otherwise leave nothing on screen: for a navigation "cancel" means the current page
        // stays, but here that page has already been destroyed, and HandleSideEffects does not even
        // restore the url (from == to). No error UI and no NotFound fallback would render either -
        // a guard is not an error and a winner WAS matched.
        cut.Instance.CancelGuard = true;
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.FindAll("[data-testid=layout]").Count);
            Assert.AreEqual(1, cut.FindAll("[data-testid=child-data]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/data/view"), nav.Uri);
        });
    }

    [TestMethod]
    public async Task Reload_with_nothing_committed_does_not_re_run_the_not_found_path()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/missing");
        var cut = RenderComponent<NotFoundInteropHost>();
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=nf-inline]"));
        Assert.AreEqual(1, cut.Instance.NotFoundHookCount);

        // Nothing matched, so nothing is committed and there is no chain to rebuild. Running the
        // pipeline anyway would re-match a url the router never rendered a page for, firing the
        // not-found path a second time for a reload the caller expects to be a no-op.
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.Instance.NotFoundHookCount);
            Assert.AreEqual(1, cut.FindAll("[data-testid=nf-inline]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/missing", StringComparison.Ordinal), nav.Uri);
        });
    }

    [TestMethod]
    public async Task Reload_with_nothing_committed_leaves_a_configured_NotFoundUrl_alone()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/missing");
        // NotFoundUrl deliberately points at a path with no route of its own: the redirect lands
        // there, nothing matches, and the router sits on the inline fallback with an empty chain -
        // the state a reload must not disturb.
        var cut = RenderComponent<NotFoundInteropHost>(p => p.Add(x => x.NotFoundUrl, "/nowhere"));
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid=nf-inline]");
            Assert.IsTrue(nav.Uri.EndsWith("/nowhere", StringComparison.Ordinal), nav.Uri);
        });
        var hooks = cut.Instance.NotFoundHookCount;

        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(hooks, cut.Instance.NotFoundHookCount);
            Assert.AreEqual(1, cut.FindAll("[data-testid=nf-inline]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/nowhere", StringComparison.Ordinal), nav.Uri);
        });
    }

    [TestMethod]
    public void Reload_from_a_ui_event_handler_rebuilds_outlet_hosted_content()
    {
        var (cut, _) = RenderAt<ReloadHost>("http://localhost/outlet/t");
        var log = cut.Instance.ProbeLog;
        cut.WaitForAssertion(() => cut.Find("[data-testid=olayout] [data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent));
        log.Clear();

        // Content hosted by a BrouterOutlet is rendered by a different renderer than inline content
        // (per-entry, in the outlet component) and torn down by a different call (DropChild rather
        // than the inline DropAllContent), so the UI-event-handler path - where render requests
        // batch - is worth pinning down here too. A survivor that merely LOOKS rebuilt is the real
        // hazard: it would still hold the dropped entry's lifecycle context and never receive
        // another route callback, hence the probe assertions alongside the state one.
        cut.Find("[data-testid=reload-btn]").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.FindAll("[data-testid=olayout] [data-testid=stateful]").Count);
            Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent);
            CollectionAssert.Contains(log, "deactivated:Disposing");
            CollectionAssert.Contains(log, "activated:first=True");
        });
    }

    [TestMethod]
    public async Task Reload_that_fails_into_the_router_level_boundary_leaves_only_the_error_ui()
    {
        var (cut, brouter) = RenderAt<ErrorContentHost>("http://localhost/rooterr");
        cut.WaitForAssertion(() => cut.Find("[data-testid=rooterr-page]"));

        // The route has no boundary of its own, so the failure lands on the ROUTER-level one, whose
        // branch empties the committed chain (everything routed is evicted). That is exactly the
        // shape the cancel restore watches for, so without a discriminator the restore would fire
        // and re-mount the page this reload already tore down - on screen beside the ErrorContent
        // that replaced it. A realistic pairing: an app-wide boundary plus a loader that starts
        // failing on the very state change ReloadAsync exists for.
        cut.Instance.RootErrShouldFail = true;
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.FindAll("[data-testid=root-boundary]").Count);
            Assert.AreEqual(0, cut.FindAll("[data-testid=rooterr-page]").Count);
        });
    }

    [TestMethod]
    public async Task Reload_cancelled_by_a_loader_restores_the_page_with_its_data()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/data/view");
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-1")));

        // A loader cancels LATER than a guard does: by then the pipeline has nulled the whole
        // matched chain's LoadedData and this reload has evicted the cached results, so the routes
        // hold nothing to render with. The restore has to put the pre-reload results back or the
        // page comes back blank-of-data - visibly intact, silently empty.
        cut.Instance.CancelInLoader = true;
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.FindAll("[data-testid=layout]").Count);
            Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-1"),
                cut.Find("[data-testid=child-data]").TextContent);
        });
    }

    [TestMethod]
    public async Task Reload_stands_down_while_a_navigation_waits_in_its_guard()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var navigated = 0;
        brouter.OnNavigated += _ => { navigated++; return ValueTask.CompletedTask; };

        // The navigation is parked in its DECISION phase (an async enter guard), so its pipeline has
        // not started: _processingNavigation is still 0 and the generation is unbumped. It is on its
        // way all the same - reloading here tears down a page it is about to replace and supersedes
        // it with the reload's own pipeline, resolving the caller's awaited NavigateAsync as
        // Superseded even though the navigation arrives moments later.
        Task<BrouterNavigationOutcome> pending = null!;
        await cut.InvokeAsync(() => { pending = brouter.NavigateAsync("/slow-guard").AsTask(); });
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        // Stood down: the page is untouched, state and all, rather than rebuilt behind the guard.
        Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => cut.Instance.GuardGate.SetResult());
        var outcome = await pending;

        Assert.AreEqual(BrouterNavigationStatus.Succeeded, outcome.Status);
        Assert.AreEqual(1, navigated);
        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(nav.Uri.EndsWith("/slow-guard"), nav.Uri);
            Assert.AreEqual(1, cut.FindAll("[data-testid=slow-guard]").Count);
        });
    }

    [TestMethod]
    public async Task Reload_stands_down_while_a_navigation_is_already_in_flight()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var navigated = 0;
        brouter.OnNavigated += _ => { navigated++; return ValueTask.CompletedTask; };

        // Mid-pipeline the router's state is half-committed: CurrentLocation is already the
        // destination while the committed chain is still the old page. A reload on that pair would
        // supersede the user's real navigation with a reload pipeline for the same url. Asserting
        // the destination alone would NOT catch that - the user still lands on the right page. What
        // breaks is the navigation's semantics: the reload commits with isReload set, so OnNavigated
        // never fires (its OnNavigating already did, in the changing phase) and the awaited outcome
        // resolves Superseded even though the navigation visibly arrived.
        Task<BrouterNavigationOutcome> pending = null!;
        await cut.InvokeAsync(() => { pending = brouter.NavigateAsync("/teardown-nav-target").AsTask(); });
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        var outcome = await pending;

        Assert.AreEqual(BrouterNavigationStatus.Succeeded, outcome.Status);
        Assert.AreEqual(1, navigated);
        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(nav.Uri.EndsWith("/teardown-nav-target"), nav.Uri);
            Assert.AreEqual(1, cut.FindAll("[data-testid=teardown-nav-target]").Count);
            Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count);
        });
    }

    [TestMethod]
    public async Task Reload_is_not_vetoed_by_a_leave_guard()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/leave-guarded");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        cut.Find("[data-testid=inc]").Click();

        // The route's leave guard cancels everything it is asked about. A reload never leaves the
        // page, so it is not asked: the teardown empties the committed chain before the pipeline
        // runs, leaving its leave phase nothing to consult. Were it consulted, the veto would abort
        // the re-match and strand the user on a blank routed region.
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
        Assert.AreEqual(0, cut.Instance.LeaveGuardRuns);

        // Still armed for real departures - the reload neither disarmed nor consumed it.
        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.Instance.LeaveGuardRuns));
        Assert.AreEqual(0, cut.FindAll("[data-testid=other]").Count);
    }

    [TestMethod]
    public async Task Reload_honours_a_redirect_from_the_re_matching_guard()
    {
        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/data/view");
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=child-data]")));
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        // A guard that means to send the user elsewhere redirects rather than cancels: unlike the
        // cancel path there is nothing to restore, because the redirect commits a real navigation.
        cut.Instance.RedirectGuardTo = "/other";
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, cut.FindAll("[data-testid=other]").Count);
            // The torn-down page must not be put back beside the page it redirected to.
            Assert.AreEqual(0, cut.FindAll("[data-testid=layout]").Count);
            // A redirect IS a navigation, so this one moves the url - the reload itself never does.
            Assert.IsTrue(nav.Uri.EndsWith("/other"), nav.Uri);
        });
    }

    [TestMethod]
    public async Task Reload_does_not_run_a_view_transition()
    {
        Services.Configure<BrouterOptions>(o => o.ViewTransitions = true);
        var module = Context!.JSInterop.SetupModule("./_content/Bit.Brouter/bit-brouter.js");
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("beginViewTransition", _ => true).SetResult(true);
        module.SetupVoid("completeViewTransition").SetVoidResult();

        var (cut, brouter) = RenderAt<ReloadHost>("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        // A real navigation animates, so the handshake is demonstrably wired up in this fixture...
        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));
        var afterNavigation = module.Invocations.Count(i => i.Identifier == "beginViewTransition");
        Assert.AreEqual(1, afterNavigation);

        await cut.InvokeAsync(() => brouter.Navigate("/plain"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));
        var beforeReload = module.Invocations.Count(i => i.Identifier == "beginViewTransition");

        // ...but a reload rebuilds the same page at the same url, where an animation reads as the
        // page cross-fading into itself.
        await cut.InvokeAsync(() => brouter.ReloadAsync().AsTask());
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));

        Assert.AreEqual(beforeReload, module.Invocations.Count(i => i.Identifier == "beginViewTransition"));
    }
}
