using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Covers the registration-time ambiguity check in <c>Brouter.RegisterRoute</c>: two routes whose
/// templates match exactly the same URLs (so winner selection could only tie-break by registration
/// order) must be rejected, mirroring the built-in router's <c>AmbiguousMatchException</c>.
/// </summary>
[TestClass]
public class AmbiguousRouteTests : BunitTestContext
{
    private IRenderedComponent<AmbiguousRoutesHost> RenderPair(string pathA, string pathB)
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/__test__");

        return RenderComponent<AmbiguousRoutesHost>(p => p
            .Add(h => h.PathA, pathA)
            .Add(h => h.PathB, pathB));
    }

    [TestMethod]
    public void Exact_duplicate_template_throws()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => RenderPair("/users", "/users"));

        StringAssert.Contains(ex.Message, "ambiguous");
    }

    [TestMethod]
    public void Templates_differing_only_by_parameter_name_throw()
    {
        // "/users/{id}" and "/users/{userId}" match exactly the same URLs.
        Assert.ThrowsExactly<InvalidOperationException>(() => RenderPair("/users/{id}", "/users/{userId}"));
    }

    [TestMethod]
    public void Templates_differing_only_by_casing_throw_under_case_insensitive_matching()
    {
        // Options.CaseSensitive defaults to false, so "/Users" and "/users" match the same URLs.
        Assert.ThrowsExactly<InvalidOperationException>(() => RenderPair("/Users", "/users"));
    }

    [TestMethod]
    public void Templates_differing_only_by_casing_are_allowed_under_case_sensitive_matching()
    {
        Services.Configure<BrouterOptions>(o => o.CaseSensitive = true);

        RenderPair("/Users", "/users");
    }

    [TestMethod]
    public void Literal_and_parameter_catch_all_forms_throw()
    {
        // "**" and "{**path}" both match zero-or-more remaining segments identically.
        Assert.ThrowsExactly<InvalidOperationException>(() => RenderPair("/files/**", "/files/{**path}"));
    }

    [TestMethod]
    public void Same_parameter_with_different_constraints_is_allowed()
    {
        // Different constraint sets match different URL sets (and score different specificity),
        // so the pair is resolvable without falling back to registration order.
        RenderPair("/users/{id}", "/users/{id:int}");
    }

    [TestMethod]
    public void Overlapping_but_distinct_literals_are_allowed()
    {
        RenderPair("/users", "/users/list");
    }

    [TestMethod]
    public void Parent_and_index_child_share_a_template_without_throwing()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/docs");

        var cut = RenderComponent<NestedIndexHost>();

        // The documented depth/index tiebreak resolves this pair: the index child wins.
        cut.WaitForAssertion(() => Assert.AreEqual("index", cut.Find("[data-testid=index]").TextContent));
    }

    [TestMethod]
    public void Hand_declared_route_may_shadow_a_discovered_page_with_the_same_template()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/7");

        // DiscoveryOverrideHost hand-declares "/discovered/{id:int}", the exact template of the
        // attribute-discovered DiscoveredPage. Registration must accept the pair (documented
        // override pattern) and the hand-declared route must win the order tie.
        var cut = RenderComponent<DiscoveryOverrideHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("override", cut.Find("[data-testid=override]").TextContent));
    }

    [TestMethod]
    public void Disposing_a_route_frees_its_template_for_re_registration()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/dup");

        var cut = RenderComponent<SwapRouteHost>(p => p.Add(h => h.Show, "a"));
        cut.WaitForAssertion(() => Assert.AreEqual("a", cut.Find("[data-testid=a]").TextContent));

        // Dispose the first route in its own render pass, then mount a second route with the
        // same template: the freed template must be accepted again (no ambiguity exception).
        cut.Render(p => p.Add(h => h.Show, "none"));
        cut.Render(p => p.Add(h => h.Show, "b"));

        // Matching runs per navigation, not on registration, so navigate again to see route b win.
        nav.NavigateTo("http://localhost/elsewhere");
        nav.NavigateTo("http://localhost/dup");

        cut.WaitForAssertion(() => Assert.AreEqual("b", cut.Find("[data-testid=b]").TextContent));
    }
}
