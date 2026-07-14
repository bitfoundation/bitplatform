using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class FoundTemplateTests : BunitTestContext
{
    [TestMethod]
    public void Found_receives_a_framework_RouteData_for_a_discovered_page()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/42");

        var cut = RenderComponent<FoundHost>();

        cut.WaitForAssertion(() =>
        {
            // The Found template observed the matched page type via RouteData.PageType.
            Assert.AreEqual(nameof(DiscoveredPage), cut.Find("[data-testid=found-page-type]").TextContent);
            // The page itself was rendered by the built-in RouteView from the supplied RouteData,
            // binding the {id:int} route value to the [Parameter] property - Brouter did not
            // instantiate the component itself.
            StringAssert.StartsWith(cut.Find("[data-testid=discovered]").TextContent, "id:42");
        });
    }

    [TestMethod]
    public void Found_wraps_hand_declared_Component_routes_too()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/hand");

        var cut = RenderComponent<FoundHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(nameof(HandComponentPage), cut.Find("[data-testid=found-page-type]").TextContent);
            Assert.AreEqual("hand", cut.Find("[data-testid=hand]").TextContent);
        });
    }

    [TestMethod]
    public void Content_routes_render_natively_and_ignore_Found()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/content-route");

        var cut = RenderComponent<FoundHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("content", cut.Find("[data-testid=content-route]").TextContent);
            // No page type exists for a Content route, so the Found template must not have run.
            Assert.AreEqual(0, cut.FindAll("[data-testid=found-page-type]").Count);
        });
    }

    [TestMethod]
    public void NotFound_fallback_still_renders_when_Found_is_set()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/definitely-missing");

        var cut = RenderComponent<FoundHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("nf:/definitely-missing", cut.Find("[data-testid=not-found]").TextContent);
            Assert.AreEqual(0, cut.FindAll("[data-testid=found-page-type]").Count);
        });
    }

    [TestMethod]
    public void Found_updates_across_navigations_between_pages()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/1");

        var cut = RenderComponent<FoundHost>();
        cut.WaitForAssertion(() =>
            Assert.AreEqual(nameof(DiscoveredPage), cut.Find("[data-testid=found-page-type]").TextContent));

        nav.NavigateTo("http://localhost/hand");

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(nameof(HandComponentPage), cut.Find("[data-testid=found-page-type]").TextContent);
            // The previous page was unrendered along with its Found wrapper.
            Assert.AreEqual(0, cut.FindAll("[data-testid=discovered]").Count);
        });
    }

    [TestMethod]
    public void Page_layout_attribute_is_honored_by_RouteView_inside_Found()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/layouted");

        var cut = RenderComponent<FoundHost>();

        cut.WaitForAssertion(() =>
        {
            // @layout FoundTestLayout on the page is resolved by the built-in RouteView, so the
            // page body renders inside the layout's marker element.
            var layout = cut.Find("[data-testid=layout-marker]");
            Assert.IsNotNull(layout.QuerySelector("[data-testid=layouted]"));
        });
    }

    [TestMethod]
    public void Component_with_multiple_page_directives_contributes_one_route_each()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/multi-a");

        var cut = RenderComponent<FoundHost>();
        cut.WaitForAssertion(() => Assert.AreEqual("multi", cut.Find("[data-testid=multi]").TextContent));

        nav.NavigateTo("http://localhost/multi-b");
        cut.WaitForAssertion(() => Assert.AreEqual("multi", cut.Find("[data-testid=multi]").TextContent));
    }

    [TestMethod]
    public void RouteView_clears_an_optional_route_value_left_unfilled_by_the_next_navigation()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/opt/saleh");

        var cut = RenderComponent<OptionalRouteViewHost>();
        cut.WaitForAssertion(() => Assert.AreEqual("saleh", cut.Find("[data-testid=opt-name]").TextContent));

        // Same Broute and page type: the component instance is reused, so the unfilled optional
        // must arrive as an explicit null route value (framework RouteData parity) - omitting the
        // entry would leave the previous navigation's value on the parameter.
        nav.NavigateTo("http://localhost/opt");

        cut.WaitForAssertion(() => Assert.AreEqual("(none)", cut.Find("[data-testid=opt-name]").TextContent));
    }

    [TestMethod]
    public void AuthorizeRouteView_inside_Found_renders_the_page_when_authorized()
    {
        var auth = Context!.AddTestAuthorization();
        auth.SetAuthorized("alice");

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/secure");

        var cut = RenderComponent<FoundAuthHost>();

        cut.WaitForAssertion(() => Assert.AreEqual("secure", cut.Find("[data-testid=secure]").TextContent));
    }

    [TestMethod]
    public void AuthorizeRouteView_inside_Found_renders_NotAuthorized_when_denied()
    {
        var auth = Context!.AddTestAuthorization();
        auth.SetNotAuthorized();

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/secure");

        var cut = RenderComponent<FoundAuthHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("denied", cut.Find("[data-testid=not-authorized]").TextContent);
            Assert.AreEqual(0, cut.FindAll("[data-testid=secure]").Count);
        });
    }
}
