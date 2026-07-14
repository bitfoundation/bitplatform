using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class BrouterTests : BunitTestContext
{
    [TestMethod]
    public void Matches_literal_route()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<SimpleHomeHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("home", cut.Find("[data-testid=home]").TextContent));
        // Sanity check: the /users route is registered but should NOT render at /home.
        Assert.AreEqual(0, cut.FindAll("[data-testid=u]").Count);
    }

    [TestMethod]
    public void Selects_most_specific_route_when_wildcard_is_declared_first()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/about");

        var cut = RenderComponent<SpecificityHost>();

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=about]")));
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=star]").Count));
    }

    [TestMethod]
    public void Optional_parameter_matches_with_or_without_value()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users");

        var cut = RenderComponent<OptionalParamHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("(none)", cut.Find("[data-testid=out]").TextContent));

        // Now navigate with a concrete value and verify the optional parameter is captured.
        nav.NavigateTo("http://localhost/users/42");

        cut.WaitForAssertion(() => Assert.AreEqual("42", cut.Find("[data-testid=out]").TextContent));
    }

    [TestMethod]
    public void Trailing_slash_is_ignored_by_default()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users/");

        var cut = RenderComponent<SimpleHomeHost>();

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=u]")));
    }

    [TestMethod]
    public void Trailing_slash_fills_the_empty_optional_final_segment_under_strict_matching()
    {
        // With IgnoreTrailingSlash = false, "/users/" is distinct from "/users". The trailing
        // slash legitimately stands in for the empty value of the unfilled optional final
        // segment of "/users/{id?}", so it must still match (with the optional value absent).
        Services.Configure<BrouterOptions>(o => o.IgnoreTrailingSlash = false);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/users/");

        var cut = RenderComponent<OptionalParamHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("(none)", cut.Find("[data-testid=out]").TextContent));

        // A trailing slash after a fully-satisfied template is a real extra slash and must NOT
        // match under strict trailing-slash handling.
        nav.NavigateTo("http://localhost/users/42/");

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=out]").Count));
    }
}
