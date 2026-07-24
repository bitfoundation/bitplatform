using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class ErrorContentTests : BunitTestContext
{
    [TestMethod]
    public void Loader_failure_renders_the_routes_own_ErrorContent_and_still_fires_OnError()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/fail-leaf");

        var cut = RenderComponent<ErrorContentHost>();

        cut.WaitForAssertion(() =>
        {
            var boundary = cut.Find("[data-testid=leaf-boundary]");
            Assert.IsTrue(boundary.TextContent.Contains("leaf loader failed"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=leaf-content]").Count);
            // Boundaries are UI; observability still goes through the OnError hook.
            Assert.AreEqual(1, cut.Instance.ErrorHookCount);
            Assert.IsInstanceOfType<InvalidOperationException>(cut.Instance.LastError);
            // The URL committed - the error happened in the commit phase, after the address bar moved.
            Assert.IsTrue(nav.Uri.EndsWith("/fail-leaf", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public void Child_failure_without_own_boundary_bubbles_to_the_parents_ErrorContent()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/parent/child");

        var cut = RenderComponent<ErrorContentHost>();

        cut.WaitForAssertion(() =>
        {
            var boundary = cut.Find("[data-testid=parent-boundary]");
            Assert.IsTrue(boundary.TextContent.Contains("child loader failed"));
            // The boundary replaces the parent's content (layout included), mirroring
            // React Router's ErrorBoundary semantics; the child never rendered.
            Assert.AreEqual(0, cut.FindAll("[data-testid=parent-layout]").Count);
            Assert.AreEqual(0, cut.FindAll("[data-testid=child-content]").Count);
        });
    }

    [TestMethod]
    public void Failure_with_no_route_boundary_falls_back_to_the_router_level_ErrorContent()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/fail-root");

        var cut = RenderComponent<ErrorContentHost>();

        cut.WaitForAssertion(() =>
        {
            var boundary = cut.Find("[data-testid=root-boundary]");
            Assert.IsTrue(boundary.TextContent.Contains("root loader failed"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=root-content]").Count);
        });
    }

    [TestMethod]
    public void Retry_re_runs_the_navigation_and_replaces_the_error_ui_on_success()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/fail-leaf");

        var cut = RenderComponent<ErrorContentHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=leaf-boundary]"));

        // Heal the loader, then retry from the boundary UI.
        cut.Instance.LeafShouldFail = false;
        cut.Find("[data-testid=retry]").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=leaf-content]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=leaf-boundary]").Count);
        });
    }

    [TestMethod]
    public void A_later_successful_navigation_clears_the_error_boundary()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/fail-root");

        var cut = RenderComponent<ErrorContentHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=root-boundary]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/ok"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=ok]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=root-boundary]").Count);
        });
    }

    [TestMethod]
    public void Route_boundary_clears_when_the_same_route_later_matches_successfully()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/fail-leaf");

        var cut = RenderComponent<ErrorContentHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=leaf-boundary]"));

        // Navigate away, heal the loader, come back: no stale error UI may survive.
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/ok"));
        cut.WaitForAssertion(() => cut.Find("[data-testid=ok]"));

        cut.Instance.LeafShouldFail = false;
        cut.InvokeAsync(() => brouter.Navigate("/fail-leaf"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=leaf-content]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=leaf-boundary]").Count);
        });
    }

    [TestMethod]
    public async Task Error_content_replacing_a_page_drops_its_stale_lock_so_later_navigation_proceeds()
    {
        // /elock is keep-alive, but the error render REPLACES its page: the departure is forced
        // to Disposing and ends the content session, so the page's auto-registered lock
        // (ErrorLockPage cancels every deactivation and never unregisters) dies with the page
        // instead of vetoing every later navigation from beyond the grave.
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/elock");

        var cut = RenderComponent<ErrorContentHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=elock-page]"));

        // A renavigation (query change - OnDeactivating doesn't fire) with a failing loader swaps
        // the page for the route's error boundary, disposing the page instance.
        cut.Instance.ELockShouldFail = true;
        var brouter = Services.GetRequiredService<IBrouter>();
        await cut.InvokeAsync(() => brouter.Navigate("/elock?attempt=2"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=elock-boundary]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=elock-page]").Count);
        });

        // The disposed page's always-cancel lock must not block leaving the error UI.
        await cut.InvokeAsync(() => brouter.Navigate("/ok"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=ok]"));
            Assert.IsTrue(nav.Uri.EndsWith("/ok", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public async Task Error_content_replacing_the_active_per_parameter_entry_reports_disposing()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/kperr/1");

        var cut = RenderComponent<ErrorContentHost>();
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find("[data-testid=kperr-page]").TextContent, "kperr 1"));

        // A second parameter set retains entry 1 hidden (its honest Hidden deactivation).
        var brouter = Services.GetRequiredService<IBrouter>();
        await cut.InvokeAsync(() => brouter.Navigate("/kperr/2"));
        cut.WaitForAssertion(() => CollectionAssert.Contains(cut.Instance.KPErrLog, "deactivated:Hidden"));

        // A renavigation with a failing loader swaps the ACTIVE entry's page for the boundary:
        // that entry ends with Disposing (its page is destroyed, not hidden) and is dropped.
        cut.Instance.KPErrShouldFail = true;
        await cut.InvokeAsync(() => brouter.Navigate("/kperr/2?attempt=2"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=kperr-boundary]"));
            CollectionAssert.Contains(cut.Instance.KPErrLog, "deactivated:Disposing");
        });
    }

    [TestMethod]
    public async Task Error_content_replacing_keepalive_content_reports_a_disposing_deactivation()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/kerr");

        var cut = RenderComponent<ErrorContentHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=probe]"));

        // A renavigation with a failing loader swaps the content for the route's error boundary.
        cut.Instance.KErrShouldFail = true;
        var brouter = Services.GetRequiredService<IBrouter>();
        await cut.InvokeAsync(() => brouter.Navigate("/kerr?attempt=2"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=kerr-boundary]"));
            // The error render destroys the page - keep-alive retention keeps a subtree that no
            // longer holds it - so the departure honestly reports Disposing, never Hidden.
            CollectionAssert.Contains(cut.Instance.KErrLog, "deactivated:Disposing");
            CollectionAssert.DoesNotContain(cut.Instance.KErrLog, "deactivated:Hidden");
        });
    }
}
