using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class RevalidateTests : BunitTestContext
{
    private (IRenderedComponent<RevalidateHost> Cut, IBrouter Brouter) RenderAtData()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/data/view");
        var cut = RenderComponent<RevalidateHost>();
        cut.WaitForAssertion(() => Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-1")));
        return (cut, Services.GetRequiredService<IBrouter>());
    }

    [TestMethod]
    public void Revalidate_reruns_all_chain_loaders_and_rerenders_fresh_data()
    {
        var (cut, brouter) = RenderAtData();
        Assert.AreEqual(1, cut.Instance.ParentLoaderRuns);
        Assert.AreEqual(1, cut.Instance.ChildLoaderRuns);

        cut.InvokeAsync(() => brouter.RevalidateAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, cut.Instance.ParentLoaderRuns);
            Assert.AreEqual(2, cut.Instance.ChildLoaderRuns);
            // The cascaded data refreshed in the DOM.
            Assert.IsTrue(cut.Find("[data-testid=child-data]").TextContent.Contains("child-2"));
        });
    }

    [TestMethod]
    public void Revalidate_does_not_rerun_guards_and_flags_the_context()
    {
        var (cut, brouter) = RenderAtData();
        var guardRunsAfterNavigation = cut.Instance.GuardRuns;
        Assert.IsFalse(cut.Instance.LastLoadWasRevalidation!.Value);

        cut.InvokeAsync(() => brouter.RevalidateAsync().AsTask());

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, cut.Instance.ChildLoaderRuns);
            Assert.AreEqual(guardRunsAfterNavigation, cut.Instance.GuardRuns);
            Assert.IsTrue(cut.Instance.LastLoadWasRevalidation!.Value);
        });
    }

    [TestMethod]
    public async Task Revalidate_with_no_matched_chain_is_a_noop()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/nowhere");
        var cut = RenderComponent<RevalidateHost>();

        var brouter = Services.GetRequiredService<IBrouter>();
        await cut.InvokeAsync(() => brouter.RevalidateAsync().AsTask());

        Assert.AreEqual(0, cut.Instance.ParentLoaderRuns);
        Assert.AreEqual(0, cut.Instance.ChildLoaderRuns);
    }

    [TestMethod]
    public async Task Routes_without_loaders_revalidate_as_a_noop()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/other");
        var cut = RenderComponent<RevalidateHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=other]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        await cut.InvokeAsync(() => brouter.RevalidateAsync().AsTask());

        // Still rendered, nothing exploded, no loader ran.
        Assert.IsNotNull(cut.Find("[data-testid=other]"));
        Assert.AreEqual(0, cut.Instance.ParentLoaderRuns);
    }
}
