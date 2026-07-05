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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
}
