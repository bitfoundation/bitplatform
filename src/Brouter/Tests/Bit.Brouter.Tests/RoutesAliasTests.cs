using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// The <c>Routes</c> parameter is an alias for <c>ChildContent</c> on both <see cref="Brouter"/>
/// and <see cref="Broute"/>, so hosts that must spell fragments out explicitly (because another
/// template like Found/NotFound/Content is present) can declare their routes under a
/// self-describing name. Setting both on the same component is a misconfiguration and throws.
/// </summary>
[TestClass]
public class RoutesAliasTests : BunitTestContext
{
    [TestMethod]
    public void Routes_declared_via_the_alias_register_and_render()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/top");

        var cut = RenderComponent<RoutesAliasHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("top", cut.Find("[data-testid=top]").TextContent));
    }

    [TestMethod]
    public void Nested_routes_declared_via_the_alias_on_a_Broute_match_too()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/parent/child");

        var cut = RenderComponent<RoutesAliasHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("child", cut.Find("[data-testid=child]").TextContent));
    }

    [TestMethod]
    public void Setting_both_ChildContent_and_Routes_on_Brouter_throws()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => RenderComponent<RoutesConflictBrouterHost>());
        StringAssert.Contains(ex.Message, "Routes is an alias");
    }

    [TestMethod]
    public void Setting_both_ChildContent_and_Routes_on_Broute_throws()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => RenderComponent<RoutesConflictBrouteHost>());
        StringAssert.Contains(ex.Message, "Routes is an alias");
    }
}
