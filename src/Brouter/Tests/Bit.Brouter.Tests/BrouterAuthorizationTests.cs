using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class BrouterAuthorizationTests : BunitTestContext
{
    [TestMethod]
    public void Authorize_page_renders_when_authorized_with_no_Found_template()
    {
        var auth = Context!.AddAuthorization();
        auth.SetAuthorized("alice");

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/secure");

        var cut = RenderComponent<AutoAuthHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("secure", cut.Find("[data-testid=secure]").TextContent));
    }

    [TestMethod]
    public void Authorize_page_renders_NotAuthorized_when_denied()
    {
        var auth = Context!.AddAuthorization();
        auth.SetNotAuthorized();

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/secure");

        var cut = RenderComponent<AutoAuthHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("denied", cut.Find("[data-testid=auto-denied]").TextContent);
            Assert.AreEqual(0, cut.FindAll("[data-testid=secure]").Count);
        });
    }

    [TestMethod]
    public void Unprotected_page_renders_even_when_not_authorized()
    {
        var auth = Context!.AddAuthorization();
        auth.SetNotAuthorized();

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/5");

        var cut = RenderComponent<AutoAuthHost>();

        // No [Authorize] on DiscoveredPage: AuthorizeRouteView renders it without an auth check,
        // exactly like the built-in Router stack.
        cut.WaitForAssertion(() =>
            StringAssert.StartsWith(cut.Find("[data-testid=discovered]").TextContent, "id:5"));
    }

    [TestMethod]
    public void DefaultLayout_wraps_pages_without_their_own_layout()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/1");

        var cut = RenderComponent<AutoAuthLayoutHost>();

        cut.WaitForAssertion(() =>
        {
            var layout = cut.Find("[data-testid=layout-marker]");
            Assert.IsNotNull(layout.QuerySelector("[data-testid=discovered]"));
        });
    }

    [TestMethod]
    public void Explicit_Found_parameter_wins_over_the_builtin_composition()
    {
        var auth = Context!.AddAuthorization();
        auth.SetAuthorized("alice");

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/1");

        var cut = RenderComponent<FoundOverrideHost>();

        cut.WaitForAssertion(() =>
        {
            // The explicit Found rendered (its marker is present) and the built-in composition
            // did not (the host's DefaultLayout never wrapped the page).
            Assert.AreEqual(nameof(DiscoveredPage), cut.Find("[data-testid=explicit-found]").TextContent);
            Assert.AreEqual(0, cut.FindAll("[data-testid=layout-marker]").Count);
        });
    }

    [TestMethod]
    public void Authorize_page_fails_closed_under_the_DefaultLayout_only_composition()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/secure");

        // AutoAuthLayoutHost sets only DefaultLayout, composing the framework RouteView - which
        // performs no authorization check at all - so Brouter's own guard must throw rather than
        // silently render the [Authorize] page. The single-batch initial mount (see
        // BrouterInitializer) renders the matched page inside the first render pass, so the
        // fail-closed throw surfaces synchronously from the mount itself.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => RenderComponent<AutoAuthLayoutHost>());
        StringAssert.Contains(exception.Message, "authorization");
    }

    [TestMethod]
    public void Authorize_page_rendered_natively_fails_closed()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/secure");

        // DiscoveryHost sets no Found and no auth parameters, so SecurePage would render natively -
        // which must throw rather than silently skip the [Authorize] check (mirroring RouteView).
        // As above, the initial mount renders the page in the first render pass, so the throw
        // surfaces synchronously from the mount.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => RenderComponent<DiscoveryHost>());
        StringAssert.Contains(exception.Message, "authorization");
    }
}
