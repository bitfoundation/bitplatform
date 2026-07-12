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
    private (IRenderedComponent<NavigationLockHost> Cut, IBrouter Brouter) RenderAt(string url)
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo(url);
        var cut = RenderComponent<NavigationLockHost>();
        return (cut, Services.GetRequiredService<IBrouter>());
    }

    [TestMethod]
    public void Locked_content_cancels_the_navigation_preventively()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var (cut, brouter) = RenderAt("http://localhost/edit");
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
        var (cut, brouter) = RenderAt("http://localhost/edit");
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var (cut, brouter) = RenderAt("http://localhost/doc/1");
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
    public void KeepAlive_content_sees_the_hidden_reason_and_can_let_the_navigation_proceed()
    {
        var (cut, brouter) = RenderAt("http://localhost/kal");
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var (cut, brouter) = RenderAt("http://localhost/edit");
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
        var (cut, brouter) = RenderAt("http://localhost/two");
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var (cut, brouter) = RenderAt("http://localhost/edit");
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
}
