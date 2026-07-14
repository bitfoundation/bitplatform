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

    [TestMethod]
    public void Cascaded_RouteData_resets_to_null_when_a_navigation_fails_into_an_error_boundary()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/ok-observed");

        var cut = RenderComponent<RouteDataErrorObserverHost>();
        cut.WaitForAssertion(() =>
            Assert.AreEqual(nameof(MultiRoutePage), cut.Find("[data-testid=probe]").TextContent));

        nav.NavigateTo("http://localhost/fail-observed");

        // The failed target's page never rendered - the error boundary is on screen instead -
        // so observers must not keep seeing the previous page's RouteData.
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=observer-boundary]"));
            Assert.AreEqual("null", cut.Find("[data-testid=probe]").TextContent);
        });
    }
}
