using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class OnNavigateAsyncTests : BunitTestContext
{
    [TestMethod]
    public void Assembly_returned_by_the_hook_matches_within_the_same_navigation()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<OnNavigateHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=home]"));

        // /discovered/7 belongs to a page the router has never seen: the hook loads its assembly
        // mid-navigation and the SAME navigation matches it.
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/discovered/7"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=discovered]"));
            Assert.IsTrue(cut.Find("[data-testid=discovered]").TextContent.Contains("id:7"));
        });
    }

    [TestMethod]
    public void Hook_runs_for_the_initial_deep_link_too()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/discovered/3");

        var cut = RenderComponent<OnNavigateHost>();

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=discovered]"));
            Assert.IsTrue(cut.Instance.HookPaths.Contains("/discovered/3"));
        });
    }

    [TestMethod]
    public void Hook_returning_null_changes_nothing()
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<OnNavigateHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=home]"));

        Assert.IsTrue(cut.Instance.HookRuns >= 1);
    }
}
