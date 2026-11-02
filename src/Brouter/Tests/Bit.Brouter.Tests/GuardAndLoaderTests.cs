using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class GuardAndLoaderTests : BunitTestContext
{
    [TestMethod]
    public void Guard_can_redirect()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/secret");

        var cut = RenderComponent<GuardHost>();

        cut.WaitForAssertion(() => StringAssert.EndsWith(nav.Uri, "/login"));
    }

    [TestMethod]
    public void Guard_redirect_to_a_route_differing_only_by_casing_is_kept_under_case_sensitive_matching()
    {
        Services.Configure<BrouterOptions>(o => o.CaseSensitive = true);
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/Admin");

        var cut = RenderComponent<CaseSensitiveGuardHost>();

        cut.WaitForAssertion(() =>
        {
            StringAssert.EndsWith(nav.Uri, "/admin");
            Assert.IsNotNull(cut.Find("[data-testid=login]"));
        });
        Assert.AreEqual(0, cut.FindAll("[data-testid=secret]").Count);
    }

    [TestMethod]
    [DataRow("/login", "/login/", 2)]
    [DataRow("/login", "/login?returnUrl=%2Fmy%20page", 2)]
    [DataRow("/login?returnUrl=%2Fmy%20page", "/login?returnUrl=%2Fmy%20page", 1)]
    [DataRow("/caf%C3%A9", "/caf%C3%A9", 1)]
    [DataRow("/caf%C3%A9", "/café", 1)]
    public void Guard_redirect_to_its_own_target_does_not_loop_on_the_initial_load(string url, string redirectUrl, int guardCalls)
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo(url);

        var cut = RenderComponent<SelfRedirectGuardHost>(p => p.Add(h => h.RedirectUrl, redirectUrl));

        AssertLandedOnTheRedirectTarget(cut, nav, redirectUrl, guardCalls);
    }

    [TestMethod]
    [DataRow("/login", "/login/", 2)]
    [DataRow("/login", "/login?returnUrl=%2Fmy%20page", 2)]
    [DataRow("/login?returnUrl=%2Fmy%20page", "/login?returnUrl=%2Fmy%20page", 1)]
    [DataRow("/caf%C3%A9", "/caf%C3%A9", 1)]
    [DataRow("/caf%C3%A9", "/café", 1)]
    public void Guard_redirect_to_its_own_target_does_not_loop_on_an_in_app_navigation(string url, string redirectUrl, int guardCalls)
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("/home");
        var cut = RenderComponent<SelfRedirectGuardHost>(p => p.Add(h => h.RedirectUrl, redirectUrl));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=home]")));

        // Decided in the LocationChanging phase, whose target location is built from the
        // unescaped Uri.ToString() form of the URL.
        nav.NavigateTo(url);

        AssertLandedOnTheRedirectTarget(cut, nav, redirectUrl, guardCalls);
    }

    private static void AssertLandedOnTheRedirectTarget(IRenderedComponent<SelfRedirectGuardHost> cut, NavigationManager nav, string redirectUrl, int guardCalls)
    {
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=target]"));
            Assert.AreEqual(nav.ToAbsoluteUri(redirectUrl).AbsoluteUri, new Uri(nav.Uri).AbsoluteUri);
        });
        Assert.AreEqual(guardCalls, cut.Instance.GuardCalls);
    }

    [TestMethod]
    [DataRow("http://localhost/admin?tab=Users#Top", "/admin?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "admin?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", ".?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "http://localhost/admin?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "HTTP://LOCALHOST:80/admin?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "//localhost/admin?tab=Users#Top")]
    [DataRow("http://localhost/admin", "/admin?")]
    [DataRow("http://localhost/admin", "/admin#")]
    [DataRow("http://localhost/login/", "/login/")]
    [DataRow("http://localhost/caf%C3%A9?returnUrl=%2Fmy%20page", "/café?returnUrl=%2Fmy page")]
    [DataRow("http://localhost/caf%C3%A9?returnUrl=%2Fmy%20page", "/caf%c3%a9?returnUrl=%2Fmy%20page")]
    // The unescaped form the LocationChanging phase builds its target location from.
    [DataRow("http://localhost/café?returnUrl=%2Fmy page", "/caf%C3%A9?returnUrl=%2Fmy%20page")]
    [DataRow("http://localhost/app/admin", "admin", "http://localhost/app/")]
    [DataRow("http://localhost/app/admin", "http://localhost/app/admin", "http://localhost/app/")]
    public void Redirect_to_the_current_target_is_a_continue(string toUri, string url, string baseUri = "http://localhost/")
    {
        var ctx = CreateContext(toUri, baseUri);

        ctx.Redirect(url);

        Assert.IsNull(ctx.RedirectUrl);
    }

    [TestMethod]
    [DataRow("http://localhost/admin?tab=Users#Top", "/Admin?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "http://localhost/ADMIN?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "/admin/?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "/admin?tab=users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "/admin?tab=Users#top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "/admin")]
    [DataRow("http://localhost/admin?tab=Users#Top", "https://localhost/admin?tab=Users#Top")]
    [DataRow("http://localhost/admin?tab=Users#Top", "http://localhost:5000/admin?tab=Users#Top")]
    [DataRow("http://localhost/products/Blue-Shirt", "/products/blue-shirt")]
    [DataRow("http://localhost/login/", "/login")]
    [DataRow("http://localhost/login/", "http://localhost/login")]
    [DataRow("http://localhost/login", "/login/")]
    [DataRow("http://localhost/admin", "http://[localhost/admin")]
    // NavigateTo resolves "/admin" against the host root, off the base path.
    [DataRow("http://localhost/app/admin", "/admin", "http://localhost/app/")]
    [DataRow("http://localhost/app/admin", "http://localhost/APP/admin", "http://localhost/app/")]
    public void Redirect_that_changes_the_url_is_honored(string toUri, string url, string baseUri = "http://localhost/")
    {
        var ctx = CreateContext(toUri, baseUri);

        ctx.Redirect(url);

        Assert.AreEqual(url, ctx.RedirectUrl);
    }

    [TestMethod]
    public void Redirect_to_the_current_target_clears_an_earlier_redirect()
    {
        var ctx = CreateContext("http://localhost/admin");

        ctx.Redirect("/other");
        ctx.Redirect("/admin");

        Assert.IsNull(ctx.RedirectUrl);
    }

    private static BrouterNavigationContext CreateContext(string toUri, string baseUri = "http://localhost/")
    {
        var navManager = new TestNavigationManager(baseUri, toUri);
        // Redirect reads only the full URI and the base-relative path (for route-relative URLs).
        var path = "/" + navManager.ToBaseRelativePath(toUri).Split('?', '#')[0];
        var to = new BrouterLocation(toUri, path, path.Split('/', StringSplitOptions.RemoveEmptyEntries), "", "");
        return new BrouterNavigationContext(to, to, CancellationToken.None, navManager);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }
    }

    [TestMethod]
    public void Loader_value_is_exposed_via_RouteData()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/data");

        var cut = RenderComponent<LoaderHost>();
        cut.WaitForAssertion(() => Assert.AreEqual("loaded!", cut.Find("[data-testid=val]").TextContent));
    }

    [TestMethod]
    public void Chain_loaders_run_sequentially_root_to_leaf_by_default()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
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
        var nav = Services.GetRequiredService<BunitNavigationManager>();
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
        var nav = Services.GetRequiredService<BunitNavigationManager>();
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
        var nav = Services.GetRequiredService<BunitNavigationManager>();
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
