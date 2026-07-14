using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class LeaveGuardTests : BunitTestContext
{
    [TestMethod]
    public void Cancelling_leave_guard_keeps_url_and_content()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var (cut, _) = RenderAt<LeaveGuardHost>("http://localhost/a");
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        cut.Instance.BlockLeavingA = true;
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Instance.Fired.Contains("a"));
            // Preventive: the navigation never committed - content and URL both still /a.
            Assert.IsNotNull(cut.Find("[data-testid=a]"));
            Assert.AreEqual(0, cut.FindAll("[data-testid=b]").Count);
            Assert.IsTrue(nav.Uri.EndsWith("/a", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public void Non_blocking_leave_guard_lets_the_navigation_proceed()
    {
        var (cut, _) = RenderAt<LeaveGuardHost>("http://localhost/a");
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            CollectionAssert.AreEqual(new[] { "a" }, cut.Instance.Fired);
        });
    }

    [TestMethod]
    public void Leave_guard_can_redirect()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var (cut, _) = RenderAt<LeaveGuardHost>("http://localhost/a");
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        cut.Instance.RedirectLeavingATo = "/parent/child2";
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=child2]"));
            Assert.IsTrue(nav.Uri.EndsWith("/parent/child2", StringComparison.Ordinal));
        });
    }

    [TestMethod]
    public void Nested_leave_guards_fire_leaf_to_root_when_leaving_the_whole_chain()
    {
        var (cut, _) = RenderAt<LeaveGuardHost>("http://localhost/parent/child1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=child1]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            CollectionAssert.AreEqual(new[] { "child1", "parent" }, cut.Instance.Fired);
        });
    }

    [TestMethod]
    public void Sibling_navigation_fires_only_the_deactivated_childs_guard()
    {
        var (cut, _) = RenderAt<LeaveGuardHost>("http://localhost/parent/child1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=child1]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/parent/child2"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=child2]"));
            // The parent stays matched under the new URL, so only child1 was "left".
            CollectionAssert.AreEqual(new[] { "child1" }, cut.Instance.Fired);
        });
    }

    [TestMethod]
    public void Parameter_only_change_on_the_same_route_is_not_a_leave()
    {
        var (cut, _) = RenderAt<LeaveGuardHost>("http://localhost/users/1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=user]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/users/2"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsTrue(cut.Find("[data-testid=user]").TextContent.Contains("2"));
            Assert.AreEqual(0, cut.Instance.Fired.Count);
        });
    }
}
