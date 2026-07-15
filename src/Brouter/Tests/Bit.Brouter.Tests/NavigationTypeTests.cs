using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class NavigationTypeTests : BunitTestContext
{
    [TestMethod]
    public void Initial_load_is_reported_as_Push()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<NavigationTypeHost>();

        cut.WaitForAssertion(() => Assert.AreEqual(BrouterNavigationType.Push, cut.Instance.LastType));
    }

    [TestMethod]
    public void Programmatic_navigate_is_a_Push()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => Assert.AreEqual(BrouterNavigationType.Push, cut.Instance.LastType));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.AreEqual(BrouterNavigationType.Push, cut.Instance.LastType);
        });
    }

    [TestMethod]
    public void Programmatic_navigate_with_replace_is_a_Replace()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => Assert.AreEqual(BrouterNavigationType.Push, cut.Instance.LastType));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b", replace: true));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.AreEqual(BrouterNavigationType.Replace, cut.Instance.LastType);
        });
    }

    [TestMethod]
    public void Non_intercepted_navigation_that_bypasses_IBrouter_is_reported_as_Pop()
    {
        // A raw NavigationManager.NavigateTo that does not go through IBrouter and is not an intercepted
        // link click is indistinguishable, at the framework level, from a browser Back/Forward. It is
        // therefore reported as Pop - the same classification a genuine history traversal receives.
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");

        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => Assert.AreEqual(BrouterNavigationType.Push, cut.Instance.LastType));

        cut.InvokeAsync(() => nav.NavigateTo("http://localhost/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.AreEqual(BrouterNavigationType.Pop, cut.Instance.LastType);
        });
    }
}
