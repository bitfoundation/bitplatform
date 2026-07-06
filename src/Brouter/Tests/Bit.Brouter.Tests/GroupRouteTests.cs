using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class GroupRouteTests : BunitTestContext
{
    [TestMethod]
    public void Group_children_match_at_their_own_paths_and_render_inside_the_group_layout()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/admin-a");

        // Two sibling pathless groups coexist without an ambiguity error, and the group adds no
        // URL segment: /admin-a matches directly.
        var cut = RenderComponent<GroupRouteHost>();

        cut.WaitForAssertion(() =>
        {
            var shell = cut.Find("[data-testid=admin-shell]");
            Assert.IsNotNull(shell.QuerySelector("[data-testid=admin-a]"));
        });
    }

    [TestMethod]
    public void Group_guard_and_loader_run_for_child_navigations()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/admin-a");

        var cut = RenderComponent<GroupRouteHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=admin-a]"));

        Assert.AreEqual(1, cut.Instance.GuardRuns);
        Assert.AreEqual(1, cut.Instance.LoaderRuns);
    }

    [TestMethod]
    public void Group_guard_redirect_protects_every_child()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/public");

        var cut = RenderComponent<GroupRouteHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=public]"));

        cut.Instance.BlockAdmin = true;
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/admin-b"));

        cut.WaitForAssertion(() =>
        {
            // Redirected by the group's guard before the URL ever committed.
            Assert.IsNotNull(cut.Find("[data-testid=public]"));
            Assert.IsTrue(nav.Uri.EndsWith("/public", StringComparison.Ordinal));
            Assert.AreEqual(0, cut.FindAll("[data-testid=admin-b]").Count);
        });
    }

    [TestMethod]
    public void Group_is_invisible_to_outlet_resolution()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/shell/inside");

        var cut = RenderComponent<GroupRouteHost>();

        cut.WaitForAssertion(() =>
        {
            // The child sits inside a pathless group with no outlets of its own; its content must
            // pass through the group into the grandparent's outlet, not render at the declaration
            // site.
            var outlet = cut.Find("[data-testid=shell-outlet]");
            Assert.IsNotNull(outlet.QuerySelector("[data-testid=inside-shell]"));
        });
    }

    [TestMethod]
    public void Group_does_not_shadow_sibling_routes()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/public");

        var cut = RenderComponent<GroupRouteHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=public]"));
            // The admin group's guard/loader must not run for a route outside it.
            Assert.AreEqual(0, cut.Instance.GuardRuns);
            Assert.AreEqual(0, cut.Instance.LoaderRuns);
        });
    }
}
