using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class KeepAliveTests : BunitTestContext
{
    private (IRenderedComponent<KeepAliveHost> Cut, IBrouter Brouter) RenderAt(string url)
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo(url);
        var cut = RenderComponent<KeepAliveHost>();
        return (cut, Services.GetRequiredService<IBrouter>());
    }

    [TestMethod]
    public void KeepAlive_route_preserves_component_state_across_navigations()
    {
        var (cut, brouter) = RenderAt("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);

        cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            // Still mounted, but inside the hidden wrapper.
            Assert.IsNotNull(cut.Find("div[hidden] [data-testid=stateful]"));
        });

        cut.InvokeAsync(() => brouter.Navigate("/ka"));
        cut.WaitForAssertion(() =>
        {
            // Visible again - and the component instance (with its state) survived.
            Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count);
            Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);
        });
    }

    [TestMethod]
    public void Plain_route_recreates_its_component_on_return()
    {
        var (cut, brouter) = RenderAt("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);

        cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            // Not keep-alive: unmounted entirely.
            Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count);
        });

        cut.InvokeAsync(() => brouter.Navigate("/plain"));
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public void KeepAlive_works_through_a_parent_outlet_for_sibling_switches()
    {
        var (cut, brouter) = RenderAt("http://localhost/parent/k1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);

        cut.InvokeAsync(() => brouter.Navigate("/parent/k2"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=k2]"));
            // k1 is retained hidden inside the outlet's kept region.
            Assert.IsNotNull(cut.Find("div[hidden] [data-testid=stateful]"));
        });

        cut.InvokeAsync(() => brouter.Navigate("/parent/k1"));
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);
            Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count);
            // k2 was transient: gone.
            Assert.AreEqual(0, cut.FindAll("[data-testid=k2]").Count);
        });
    }
}
