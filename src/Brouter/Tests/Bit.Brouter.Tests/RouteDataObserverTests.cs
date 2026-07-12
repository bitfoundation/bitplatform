using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class RouteDataObserverTests : BunitTestContext
{
    [TestMethod]
    public void Cascaded_RouteData_carries_the_matched_page_type()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/7");

        var cut = RenderComponent<RouteDataObserverHost>();

        cut.WaitForAssertion(() =>
            Assert.AreEqual(nameof(DiscoveredPage), cut.Find("[data-testid=probe]").TextContent));
    }

    [TestMethod]
    public void Cascaded_RouteData_updates_across_navigations()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/7");

        var cut = RenderComponent<RouteDataObserverHost>();
        cut.WaitForAssertion(() =>
            Assert.AreEqual(nameof(DiscoveredPage), cut.Find("[data-testid=probe]").TextContent));

        nav.NavigateTo("http://localhost/multi-a");

        cut.WaitForAssertion(() =>
            Assert.AreEqual(nameof(MultiRoutePage), cut.Find("[data-testid=probe]").TextContent));
    }

    [TestMethod]
    public void Cascaded_RouteData_is_null_when_nothing_matches()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/definitely-missing");

        var cut = RenderComponent<RouteDataObserverHost>();

        cut.WaitForAssertion(() =>
            Assert.AreEqual("null", cut.Find("[data-testid=probe]").TextContent));
    }
}
