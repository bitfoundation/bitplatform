using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class RelativeNavigationTests : BunitTestContext
{
    [TestMethod]
    public void IsRelative_accepts_only_dot_prefixed_paths()
    {
        Assert.IsTrue(BrouterRelativeUrl.IsRelative("."));
        Assert.IsTrue(BrouterRelativeUrl.IsRelative(".."));
        Assert.IsTrue(BrouterRelativeUrl.IsRelative("./edit"));
        Assert.IsTrue(BrouterRelativeUrl.IsRelative("../sibling"));
        Assert.IsTrue(BrouterRelativeUrl.IsRelative("../../x"));

        // A query or hash directly after the dot form is still relative (Resolve preserves it).
        Assert.IsTrue(BrouterRelativeUrl.IsRelative(".?tab=info"));
        Assert.IsTrue(BrouterRelativeUrl.IsRelative("..?tab=info"));
        Assert.IsTrue(BrouterRelativeUrl.IsRelative(".#top"));
        Assert.IsTrue(BrouterRelativeUrl.IsRelative("..#top"));

        // Bare and dotted-but-not-relative names keep their base-relative meaning.
        Assert.IsFalse(BrouterRelativeUrl.IsRelative("sibling"));
        Assert.IsFalse(BrouterRelativeUrl.IsRelative("/absolute"));
        Assert.IsFalse(BrouterRelativeUrl.IsRelative(".well-known"));
        Assert.IsFalse(BrouterRelativeUrl.IsRelative("..foo"));
        Assert.IsFalse(BrouterRelativeUrl.IsRelative(""));
    }

    [TestMethod]
    public void Resolve_uses_segment_math_against_the_current_path()
    {
        Assert.AreEqual("/users/42/edit", BrouterRelativeUrl.Resolve("/users/42", "./edit"));
        Assert.AreEqual("/users/7", BrouterRelativeUrl.Resolve("/users/42", "../7"));
        Assert.AreEqual("/users", BrouterRelativeUrl.Resolve("/users/42", ".."));
        Assert.AreEqual("/users/42", BrouterRelativeUrl.Resolve("/users/42", "."));
        Assert.AreEqual("/admin", BrouterRelativeUrl.Resolve("/users/42", "../../admin"));
        Assert.AreEqual("/x", BrouterRelativeUrl.Resolve("/", "./x"));
    }

    [TestMethod]
    public void Resolve_clamps_excess_parent_references_at_the_root()
    {
        Assert.AreEqual("/x", BrouterRelativeUrl.Resolve("/users/42", "../../../../x"));
        Assert.AreEqual("/", BrouterRelativeUrl.Resolve("/users", "../.."));
    }

    [TestMethod]
    public void Resolve_preserves_query_and_hash()
    {
        Assert.AreEqual("/users/7?tab=info#top", BrouterRelativeUrl.Resolve("/users/42", "../7?tab=info#top"));
        Assert.AreEqual("/users/42?tab=info", BrouterRelativeUrl.Resolve("/users/42", ".?tab=info"));
    }

    [TestMethod]
    public void Navigate_resolves_relative_url_against_current_location()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users/42");

        var cut = RenderComponent<NamedRouteHost>(p => p
            .Add(h => h.Name, "user")
            .Add(h => h.Path, "/users/{id}"));

        var brouter = Services.GetRequiredService<IBrouter>();
        // Ensure the initial location has committed before resolving against it.
        cut.WaitForAssertion(() => Assert.AreEqual("/users/42", brouter.Location.Path));

        brouter.Navigate("../7");

        cut.WaitForAssertion(() => StringAssert.EndsWith(nav.Uri, "/users/7"));
    }

    [TestMethod]
    public void Guard_redirect_resolves_relative_url_against_the_target_location()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/admin/secret");

        var cut = RenderComponent<RelativeRedirectHost>();

        cut.WaitForAssertion(() =>
        {
            StringAssert.EndsWith(nav.Uri, "/admin/login");
            Assert.IsNotNull(cut.Find("[data-testid=login]"));
        });
    }

    [TestMethod]
    public void Link_renders_resolved_href_and_re_resolves_after_navigation()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users/42");

        // RelativeLinkHost's catch-all route matches every path, so each navigation fires
        // OnNavigated and the link re-resolves (links only refresh on matched navigations).
        var cut = RenderComponent<RelativeLinkHost>(p => p.Add(x => x.Href, "../sibling"));

        cut.WaitForAssertion(() =>
            Assert.AreEqual("/users/sibling", cut.Find("[data-testid=link]").GetAttribute("href")));

        // A route-relative link points somewhere new after navigating; the DOM href must follow.
        nav.NavigateTo("http://localhost/a/b/c");

        cut.WaitForAssertion(() =>
            Assert.AreEqual("/a/b/sibling", cut.Find("[data-testid=link]").GetAttribute("href")));
    }
}
