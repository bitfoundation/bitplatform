using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class NamedOutletTests : BunitTestContext
{
    private (IRenderedComponent<NamedOutletHost> Cut, IBrouter Brouter) RenderAt(string url)
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo(url);
        var cut = RenderComponent<NamedOutletHost>();
        return (cut, Services.GetRequiredService<IBrouter>());
    }

    [TestMethod]
    public void One_route_fills_both_the_primary_and_the_named_outlet()
    {
        var (cut, _) = RenderAt("http://localhost/dash/a");

        cut.WaitForAssertion(() =>
        {
            var main = cut.Find("[data-testid=main-outlet]");
            var side = cut.Find("[data-testid=side-outlet]");
            Assert.IsNotNull(main.QuerySelector("[data-testid=a-main]"));
            Assert.IsNotNull(side.QuerySelector("[data-testid=a-side]"));
        });
    }

    [TestMethod]
    public void A_route_without_the_view_leaves_the_named_outlet_empty()
    {
        var (cut, brouter) = RenderAt("http://localhost/dash/a");
        cut.WaitForAssertion(() => cut.Find("[data-testid=a-side]"));

        cut.InvokeAsync(() => brouter.Navigate("/dash/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b-main]"));
            // Switching to a view-less sibling clears the sidebar.
            Assert.AreEqual(0, cut.FindAll("[data-testid=a-side]").Count);
            Assert.AreEqual(string.Empty, cut.Find("[data-testid=side-outlet]").TextContent.Trim());
        });
    }

    [TestMethod]
    public void Route_parameters_flow_into_named_view_fragments()
    {
        var (cut, _) = RenderAt("http://localhost/dash/u/42");

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Find("[data-testid=u-main]").TextContent.Contains("42"));
            Assert.IsTrue(cut.Find("[data-testid=u-side]").TextContent.Contains("42"));
        });
    }
}
