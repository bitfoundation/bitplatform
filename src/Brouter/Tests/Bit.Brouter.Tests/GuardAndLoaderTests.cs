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
}
