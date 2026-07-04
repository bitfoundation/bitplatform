using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class GuardAndLoaderTests : BunitTestContext
{
    [TestMethod]
    public void Guard_can_redirect()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/secret");

        var cut = RenderComponent<GuardHost>();

        cut.WaitForAssertion(() => StringAssert.EndsWith(nav.Uri, "/login"));
    }

    [TestMethod]
    public void Loader_value_is_exposed_via_RouteData()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/data");

        var cut = RenderComponent<LoaderHost>();
        cut.WaitForAssertion(() => Assert.AreEqual("loaded!", cut.Find("[data-testid=val]").TextContent));
    }

    [TestMethod]
    public void Chain_loaders_run_sequentially_root_to_leaf_by_default()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/parent/child");

        var cut = RenderComponent<SequentialLoadersHost>();
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=done]")), TimeSpan.FromSeconds(3));

        CollectionAssert.AreEqual(
            new[] { "parent:start", "parent:end", "child:start" },
            cut.Instance.Events);
    }

    [TestMethod]
    public void ParallelLoaders_runs_chain_loaders_concurrently()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/parent/child");

        var cut = RenderComponent<ParallelLoadersHost>();

        // The parent's loader completes only after the child's has started, so rendering
        // finishing at all proves the two loaders overlapped (sequential execution would
        // time out inside the parent's loader and never match the route).
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=done]")), TimeSpan.FromSeconds(3));
    }

    [TestMethod]
    public void Guard_cancel_prevents_navigation_before_the_url_commits()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<PreventiveGuardHost>();
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=home]")));

        // Navigate to a guarded route whose guard cancels. Because the guard now runs inside the
        // RegisterLocationChangingHandler decision, the URL must never change to /cancel: the
        // navigation is PREVENTED, not committed-then-reverted.
        nav.NavigateTo("http://localhost/cancel");

        cut.WaitForAssertion(() =>
        {
            StringAssert.EndsWith(nav.Uri, "/home");
            var latest = nav.History.First();
            StringAssert.EndsWith(latest.Uri, "cancel");
            Assert.AreEqual(NavigationState.Prevented, latest.State);
        });

        // The guarded route never rendered, and we stayed on /home.
        Assert.AreEqual(0, cut.FindAll("[data-testid=cancel]").Count);
        Assert.IsNotNull(cut.Find("[data-testid=home]"));
    }

    [TestMethod]
    public void Guard_redirect_prevents_the_original_navigation_and_lands_on_the_target()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<PreventiveGuardHost>();
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=home]")));

        // Navigate to a guarded route whose guard redirects to /login. The intermediate /redirect
        // URL must be prevented (never committed), and we end up on /login.
        nav.NavigateTo("http://localhost/redirect");

        cut.WaitForAssertion(() =>
        {
            StringAssert.EndsWith(nav.Uri, "/login");
            Assert.IsNotNull(cut.Find("[data-testid=login]"));
        });

        // The blocked /redirect entry was prevented, never succeeded.
        var redirectEntry = nav.History.FirstOrDefault(h => h.Uri.EndsWith("redirect"));
        Assert.IsNotNull(redirectEntry);
        Assert.AreEqual(NavigationState.Prevented, redirectEntry!.State);
    }
}
