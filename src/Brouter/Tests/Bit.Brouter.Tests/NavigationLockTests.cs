using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// The component-level navigation lock (<see cref="IBrouterRoute.OnDeactivatingAsync"/> /
/// <see cref="IBrouterRoute.OnRenavigatingAsync"/>): preventive cancellation and redirects from
/// the content itself, the renavigating veto a route-declared <see cref="Broute.LeaveGuard"/>
/// can't express (parameter changes), the Hidden/Disposing reason, lock-before-guard ordering,
/// first-cancel-wins across multiple handlers, and the await-a-custom-dialog flow (including its
/// supersession).
/// </summary>
[TestClass]
public class NavigationLockTests : BunitTestContext
{
    [TestMethod]
    public void Locked_content_cancels_the_navigation_preventively()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/edit");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        cut.Instance.EditState.Locked = true;
        cut.InvokeAsync(() => brouter.Navigate("/other"));

        cut.WaitForAssertion(() =>
        {
            // The lock saw the pending target and the transient route's Disposing reason...
            CollectionAssert.Contains(cut.Instance.EditState.Log, "edit:deactivating:Disposing:to=/other");
            // ...and cancelled preventively: content and URL both still /edit, and the
            // route-declared LeaveGuard never ran (the lock's decision settled the phase).
            Assert.IsNotNull(cut.Find("[data-testid=lockprobe]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=other]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/edit", StringComparison.Ordinal));
            CollectionAssert.DoesNotContain(cut.Instance.EditState.Log, "edit:leaveguard");
        });
    }

    [TestMethod]
    public void Unlocked_content_observes_the_callback_before_the_route_leave_guard_and_navigation_proceeds()
    {
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/edit");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        cut.InvokeAsync(() => brouter.Navigate("/other"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            // Innermost first: the content's own lock ran before the route-declared LeaveGuard.
            CollectionAssert.AreEqual(
                new[] { "edit:deactivating:Disposing:to=/other", "edit:leaveguard" },
                cut.Instance.EditState.Log);
        });
    }

    [TestMethod]
    public void Renavigating_lock_vetoes_a_parameter_change_on_the_same_route()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/doc/1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=doc]"));

        cut.Instance.DocState.Locked = true;
        cut.InvokeAsync(() => brouter.Navigate("/doc/2"));

        cut.WaitForAssertion(() =>
        {
            // A parameter change is not a "leave" - a route-declared LeaveGuard can't veto it -
            // but the content's renavigating lock can: still doc 1, URL untouched.
            CollectionAssert.Contains(cut.Instance.DocState.Log, "doc:renavigating:to=/doc/2");
            StringAssert.Contains(cut.Find("[data-testid=doc]").TextContent, "doc 1");
            Assert.IsTrue(nav.Uri.EndsWith("/doc/1", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public async Task Outlet_hosted_keepalive_content_sees_hidden_for_sibling_switches_but_disposing_when_the_host_leaves()
    {
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/shell/kalo");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        // Sibling switch under the same host: the kept child survives hidden - Hidden reason.
        await cut.InvokeAsync(() => brouter.Navigate("/shell/sibling"));
        cut.WaitForAssertion(() =>
            CollectionAssert.Contains(cut.Instance.KaloState.Log, "kalo:deactivating:Hidden:to=/shell/sibling"));

        await cut.InvokeAsync(() => brouter.Navigate("/shell/kalo"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        // Leaving the (transient) host destroys its outlet and the kept child with it: the lock
        // must report Disposing - KeepAlive alone doesn't mean the state survives this navigation.
        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            CollectionAssert.Contains(cut.Instance.KaloState.Log, "kalo:deactivating:Disposing:to=/other");
        });
    }

    [TestMethod]
    public async Task Named_outlet_view_content_registers_a_lock_and_can_veto_the_navigation()
    {
        // Regression: named-outlet views carry the route lifecycle cascade (see
        // BrouterOutlet.WrapRouteContext), so a BrouterRouteBase inside a <BrouterView> fragment
        // registers a lock against the CHILD route - without the cascade it would never be
        // consulted and the navigation would sail through.
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/nshell/view");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        cut.Instance.NamedViewState.Locked = true;
        await cut.InvokeAsync(() => brouter.Navigate("/nshell/plain"));

        cut.WaitForAssertion(() =>
        {
            // Named views die with the child route - transient regardless of KeepAlive - so the
            // lock sees the Disposing reason, and its cancel keeps content and URL in place.
            CollectionAssert.Contains(cut.Instance.NamedViewState.Log, "nview:deactivating:Disposing:to=/nshell/plain");
            Assert.IsNotNull(cut.Find("[data-testid=lockprobe]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=nplain]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/nshell/view", StringComparison.Ordinal));
        });

        // Unlocked, the same navigation proceeds and unmounts the named view.
        cut.Instance.NamedViewState.Locked = false;
        await cut.InvokeAsync(() => brouter.Navigate("/nshell/plain"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=nplain]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=lockprobe]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/nshell/plain", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public async Task Keepalive_named_view_lock_sees_disposing_while_primary_content_sees_hidden()
    {
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/nshell/kview");
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-testid=lockprobe]").Count));

        await cut.InvokeAsync(() => brouter.Navigate("/nshell/plain"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=nplain]"));
            // The reason is per-context: retention preserves the primary content across the
            // sibling switch (Hidden), but a named view is never kept - its lock honestly sees
            // Disposing in the very same navigation.
            CollectionAssert.Contains(cut.Instance.KviewState.Log, "kview:deactivating:Hidden:to=/nshell/plain");
            CollectionAssert.Contains(cut.Instance.KviewSideState.Log, "kviewside:deactivating:Disposing:to=/nshell/plain");
        });
    }

    [TestMethod]
    public void KeepAlive_content_sees_the_hidden_reason_and_can_let_the_navigation_proceed()
    {
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/kal");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        cut.InvokeAsync(() => brouter.Navigate("/other"));

        cut.WaitForAssertion(() =>
        {
            // Keep-alive retention means the state survives - the lock can see Hidden and decide
            // "no prompt needed", which no route-declared guard can express.
            CollectionAssert.Contains(cut.Instance.KalState.Log, "kal:deactivating:Hidden:to=/other");
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
        });
    }

    [TestMethod]
    public void Lock_can_redirect_the_navigation()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/edit");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        cut.Instance.EditState.RedirectTo = "/target";
        cut.InvokeAsync(() => brouter.Navigate("/other"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=target]"));
            Assert.IsTrue(nav.Uri.EndsWith("/target", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public void First_cancelling_lock_wins_and_later_handlers_are_skipped()
    {
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/two");
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-testid=lockprobe]").Count));

        cut.Instance.TwoFirstState.Locked = true;
        cut.InvokeAsync(() => brouter.Navigate("/other"));

        cut.WaitForAssertion(() =>
        {
            CollectionAssert.Contains(cut.Instance.TwoFirstState.Log, "two1:deactivating:Disposing:to=/other");
            // The first handler's cancel settled the navigation - the second never saw it
            // (no stacked confirmation prompts).
            Assert.AreEqual(0, cut.Instance.TwoSecondState.Log.Count);
            Assert.AreEqual(2, cut.FindAll("[data-testid=lockprobe]").Count);
        });
    }

    [TestMethod]
    public async Task Async_lock_holds_the_navigation_open_for_a_custom_prompt()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/edit");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        // The user says "stay": the parked navigation is cancelled.
        cut.Instance.EditState.Prompt = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await cut.InvokeAsync(() => brouter.Navigate("/other"));

        // The navigation is parked inside the awaited lock: nothing committed yet.
        cut.WaitForAssertion(() =>
            CollectionAssert.Contains(cut.Instance.EditState.Log, "edit:deactivating:Disposing:to=/other"));
        Assert.AreEqual(0, cut.FindAll("[data-testid=other]").Count);
        Assert.IsTrue(nav.Uri.EndsWith("/edit", StringComparison.Ordinal));

        cut.Instance.EditState.Prompt.TrySetResult(true);
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=lockprobe]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=other]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/edit", StringComparison.Ordinal));
        });

        // The user says "leave": the same flow releases the (new) navigation.
        cut.Instance.EditState.Prompt = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.Instance.EditState.Prompt.TrySetResult(false);

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            Assert.IsTrue(nav.Uri.EndsWith("/other", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public async Task A_superseding_navigation_completes_while_the_first_is_parked_and_never_resolves_its_prompt()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<NavigationLockHost>("http://localhost/edit");
        cut.WaitForAssertion(() => cut.Find("[data-testid=lockprobe]"));

        // Park the first navigation inside the awaited lock and deliberately never answer it.
        var firstPrompt = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        cut.Instance.EditState.Prompt = firstPrompt;
        await cut.InvokeAsync(() => brouter.Navigate("/other"));

        cut.WaitForAssertion(() =>
            CollectionAssert.Contains(cut.Instance.EditState.Log, "edit:deactivating:Disposing:to=/other"));
        Assert.AreEqual(0, cut.FindAll("[data-testid=other]").Count);
        Assert.IsTrue(nav.Uri.EndsWith("/edit", StringComparison.Ordinal));

        // A second navigation while the first is still parked supersedes it: the probe observes
        // the first context's CancellationToken (its parked await is released without touching
        // the prompt) and the second navigation runs its own lock phase and commits.
        cut.Instance.EditState.Prompt = null;
        await cut.InvokeAsync(() => brouter.Navigate("/target"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=target]"));
            Assert.IsTrue(nav.Uri.EndsWith("/target", StringComparison.Ordinal));
            CollectionAssert.Contains(cut.Instance.EditState.Log, "edit:deactivating:Disposing:to=/target");
        });

        // Nothing ever resolved the abandoned prompt - the supersession alone released the lock.
        Assert.IsFalse(firstPrompt.Task.IsCompleted);
    }
}
