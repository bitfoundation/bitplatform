using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class NamedOutletTests : BunitTestContext
{
    [TestMethod]
    public void One_route_fills_both_the_primary_and_the_named_outlet()
    {
        var (cut, _) = RenderAt<NamedOutletHost>("http://localhost/dash/a");

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
        var (cut, brouter) = RenderAt<NamedOutletHost>("http://localhost/dash/a");
        cut.WaitForAssertion(() => cut.Find("[data-testid=a-side]"));

        cut.InvokeAsync(() => brouter.Navigate("/dash/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b-main]"));
            // Switching to a view-less sibling clears the sidebar.
            Assert.IsEmpty(cut.FindAll("[data-testid=a-side]"));
            Assert.AreEqual(string.Empty, cut.Find("[data-testid=side-outlet]").TextContent.Trim());
        });
    }

    [TestMethod]
    public void Route_parameters_flow_into_named_view_fragments()
    {
        var (cut, _) = RenderAt<NamedOutletHost>("http://localhost/dash/u/42");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("42", cut.Find("[data-testid=u-main]").TextContent);
            Assert.Contains("42", cut.Find("[data-testid=u-side]").TextContent);
        });
    }

    [TestMethod]
    public async Task Parameter_change_preserves_outlet_hosted_content_and_named_views_as_a_renavigation()
    {
        var (cut, brouter) = RenderAt<NamedOutletHost>("http://localhost/dash/u/1");
        cut.WaitForAssertion(() => Assert.Contains("user 1", cut.Find("[data-testid=u-main]").TextContent));

        await cut.InvokeAsync(() => brouter.Navigate("/dash/u/2"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("user 2", cut.Find("[data-testid=u-main]").TextContent);
            Assert.Contains("sidebar for 2", cut.Find("[data-testid=u-side]").TextContent);

            // Same entries re-bound, not fresh sessions: the primary content AND the named view
            // each saw exactly one activation and resolved the parameter change as a renavigation -
            // never a deactivation - proving the outlet preserved their component state.
            Assert.AreSequenceEqual(
                new[] { "activated:first=True", "renavigated:/dash/u/1->/dash/u/2" }, cut.Instance.UMainLog);
            Assert.AreSequenceEqual(
                new[] { "activated:first=True", "renavigated:/dash/u/1->/dash/u/2" }, cut.Instance.USideLog);
        });
    }

    [TestMethod]
    public async Task Parameter_change_rebuilds_outlet_hosted_content_and_named_views_when_opted_in()
    {
        Services.Configure<BrouterOptions>(o => o.RemountOnParameterChange = true);

        var (cut, brouter) = RenderAt<NamedOutletHost>("http://localhost/dash/u/1");
        cut.WaitForAssertion(() => Assert.Contains("user 1", cut.Find("[data-testid=u-main]").TextContent));

        await cut.InvokeAsync(() => brouter.Navigate("/dash/u/2"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("user 2", cut.Find("[data-testid=u-main]").TextContent);
            Assert.Contains("sidebar for 2", cut.Find("[data-testid=u-side]").TextContent);

            // Fresh sessions in BOTH outlets: the primary content and the named view were each
            // disposed with the route's rebuild and mounted anew - never re-bound as a renavigation.
            var rebuilt = new[] { "activated:first=True", "deactivated:Disposing", "activated:first=True" };
            Assert.AreSequenceEqual(rebuilt, cut.Instance.UMainLog);
            Assert.AreSequenceEqual(rebuilt, cut.Instance.USideLog);
        });
    }
}
