using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BrouterComp = Bit.Brouter.Brouter;

namespace Bit.Brouter.Tests;

[TestClass]
public class BrouterTests : BunitTestContext
{
    [TestMethod]
    public void Matches_root_route()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<BrouterComp>(p => p.AddChildContent(@"
<Route Path=""/home"">
    <Content><div data-testid=""home"">home</div></Content>
</Route>"));

        cut.WaitForAssertion(() => Assert.AreEqual("home", cut.Find("[data-testid=home]").TextContent));
    }

    [TestMethod]
    public void Selects_most_specific_route_when_wildcard_is_declared_first()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/about");

        var cut = RenderComponent<BrouterComp>(p => p.AddChildContent(@"
<Route Path=""/*"">
    <Content><div data-testid=""star"">star</div></Content>
</Route>
<Route Path=""/about"">
    <Content><div data-testid=""about"">about</div></Content>
</Route>"));

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

        var cut = RenderComponent<BrouterComp>(p => p.AddChildContent(@"
<Route Path=""/users""><Content><div data-testid=""u"">users</div></Content></Route>"));

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=u]")));
    }
}
