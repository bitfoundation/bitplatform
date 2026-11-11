using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class QueryBuilderTests : BunitTestContext
{
    private (IRenderedComponent<QueryHost> Cut, IBrouter Brouter, BunitNavigationManager Nav) RenderAt(string url)
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo(url);
        var cut = RenderComponent<QueryHost>();
        return (cut, Services.GetRequiredService<IBrouter>(), nav);
    }

    [TestMethod]
    public void Set_updates_one_parameter_and_preserves_the_rest()
    {
        var (cut, brouter, nav) = RenderAt("http://localhost/q?filter=red&sort=name&page=1");

        cut.InvokeAsync(() => brouter.NavigateWithQuery(q => q.Set("page", 2)));

        cut.WaitForAssertion(() =>
        {
            var uri = new Uri(nav.Uri);
            Assert.AreEqual("/q", uri.AbsolutePath);
            Assert.Contains("filter=red", uri.Query);
            Assert.Contains("sort=name", uri.Query);
            Assert.Contains("page=2", uri.Query);
            Assert.DoesNotContain("page=1", uri.Query);
        });
    }

    [TestMethod]
    public void Null_value_and_Remove_delete_the_parameter()
    {
        var (cut, brouter, nav) = RenderAt("http://localhost/q?filter=red&page=3");

        cut.InvokeAsync(() => brouter.NavigateWithQuery(q => q.Set("filter", null).Remove("page")));

        cut.WaitForAssertion(() =>
        {
            var uri = new Uri(nav.Uri);
            Assert.AreEqual(string.Empty, uri.Query);
        });
    }

    [TestMethod]
    public void SetAll_emits_one_pair_per_value()
    {
        var (cut, brouter, nav) = RenderAt("http://localhost/q");

        cut.InvokeAsync(() => brouter.NavigateWithQuery(q => q.SetAll("tag", ["a", "b"])));

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(nav.Uri.EndsWith("/q?tag=a&tag=b", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public void Replace_is_the_default_history_behavior()
    {
        var (cut, brouter, nav) = RenderAt("http://localhost/q?page=1");

        cut.InvokeAsync(() => brouter.NavigateWithQuery(q => q.Set("page", 2)));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("page=2", nav.Uri);
            Assert.IsTrue(nav.History.Last().Options.ReplaceHistoryEntry);
        });
    }

    [TestMethod]
    public void Values_are_formatted_invariantly()
    {
        var (cut, brouter, nav) = RenderAt("http://localhost/q");

        cut.InvokeAsync(() => brouter.NavigateWithQuery(q => q.Set("price", 3.5).Set("active", true)));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("price=3.5", nav.Uri);
            Assert.Contains("active=true", nav.Uri);
        });
    }
}
