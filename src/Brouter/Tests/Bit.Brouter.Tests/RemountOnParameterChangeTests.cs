using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// What happens to a route's content when a navigation keeps the route matched but changes its
/// parameter values (<c>/settings/profile</c> -> <c>/settings/account</c>).
/// <para>
/// By default the instance survives and the change arrives as a renavigation - the same reuse the
/// built-in <c>Router</c> gives a page. <see cref="BrouterOptions.RemountOnParameterChange"/>
/// (globally) and <see cref="Broute.RemountOnParameterChange"/> (per route) switch to rebuilding
/// the content instead, so everything a component reads once - <c>OnInitialized</c>, a child that
/// captures a value when it registers with its parent, an uncontrolled <c>Default*</c> parameter on
/// a UI component - sees the new values. A rebuild disposes the content, so it votes and is notified
/// exactly like a route being left.
/// </para>
/// </summary>
[TestClass]
public class RemountOnParameterChangeTests : BunitTestContext
{
    private void UseRemount() => Services.Configure<BrouterOptions>(o => o.RemountOnParameterChange = true);

    private static void Increment(IRenderedComponent<RemountHost> cut) => cut.Find("[data-testid=inc]").Click();

    private static string Counter(IRenderedComponent<RemountHost> cut)
        => cut.Find("[data-testid=stateful]").TextContent;

    [TestMethod]
    public async Task Parameter_change_re_binds_the_instance_by_default()
    {
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rm/1");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));

        await cut.InvokeAsync(() => brouter.Navigate("/rm/2"));

        // Same instance, and the lifecycle says renavigation - what the built-in Router's RouteView
        // does with a page too (OnParametersSet runs, OnInitialized does not).
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));
        cut.WaitForAssertion(() => CollectionAssert.AreEqual(
            new[] { "activated:first=True", "renavigated:/rm/1->/rm/2" }, cut.Instance.Log));
    }

    [TestMethod]
    public async Task Option_rebuilds_the_content_on_a_parameter_change()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rm/1");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));

        await cut.InvokeAsync(() => brouter.Navigate("/rm/2"));

        // A brand-new instance: the state the old one accumulated is gone, and the lifecycle reports
        // a Disposing deactivation plus a first activation - never a renavigation.
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));
        cut.WaitForAssertion(() => CollectionAssert.AreEqual(
            new[] { "activated:first=True", "deactivated:Disposing", "activated:first=True" },
            cut.Instance.Log));
    }

    [TestMethod]
    public async Task Query_only_change_does_not_rebuild_the_content()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rm/1");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));

        await cut.InvokeAsync(() => brouter.Navigate("/rm/1?tab=2"));

        // The route parameters are unchanged, so there is nothing to rebuild (a
        // [SupplyParameterFromQuery] property is re-supplied on the live instance).
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));
        cut.WaitForAssertion(() => CollectionAssert.AreEqual(
            new[] { "activated:first=True", "renavigated:/rm/1->/rm/1" }, cut.Instance.Log));
    }

    [TestMethod]
    public async Task Route_can_opt_out_of_rebuilding()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rmoff/1");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));

        await cut.InvokeAsync(() => brouter.Navigate("/rmoff/2"));

        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));
    }

    [TestMethod]
    public async Task Route_can_opt_in_under_the_re_binding_default()
    {
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rmon/1");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        await cut.InvokeAsync(() => brouter.Navigate("/rmon/2"));

        // The route asked for a rebuild explicitly, so it rebuilds regardless of the default.
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));
    }

    [TestMethod]
    public async Task KeepAlive_route_keeps_its_instance_across_a_parameter_change()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rmka/1");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        await cut.InvokeAsync(() => brouter.Navigate("/rmka/2"));

        // Retaining the instance is the entire point of KeepAlive: the option must not undo it.
        cut.WaitForAssertion(() => Assert.AreEqual("count:1", Counter(cut)));
    }

    [TestMethod]
    public async Task Component_route_page_is_rebuilt_too()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rmpage/1");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        await cut.InvokeAsync(() => brouter.Navigate("/rmpage/2"));

        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));
    }

    [TestMethod]
    public async Task Outlet_hosted_child_is_rebuilt_while_its_layout_is_left_alone()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/shell/doc/1");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find("[data-testid=doc]").TextContent, "doc 1"));

        // State in the layout (the shell's own counter) and in the child (the outlet-hosted one).
        Increment(cut);
        cut.Find("[data-testid=kpinc]").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:1", Counter(cut));
            StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:1");
        });

        await cut.InvokeAsync(() => brouter.Navigate("/shell/doc/2"));

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Find("[data-testid=doc]").TextContent, "doc 2");
            // Only the route whose parameters changed is rebuilt; the layout above it never left
            // the chain and keeps its state.
            Assert.AreEqual("count:1", Counter(cut));
            StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:0");
        });
    }

    [TestMethod]
    public async Task Rebuilding_a_parent_rebuilds_the_child_nested_inside_it()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/user/1/edit");
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", Counter(cut)));

        Increment(cut);
        cut.Find("[data-testid=kpinc]").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:1", Counter(cut));
            StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:1");
        });

        await cut.InvokeAsync(() => brouter.Navigate("/user/2/edit"));

        cut.WaitForAssertion(() =>
        {
            // The child's own parameters didn't change, but its content lives inside the subtree the
            // parent's rebuild replaces - keeping that instance would mean keeping a component whose
            // host is gone.
            Assert.AreEqual("count:0", Counter(cut));
            StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:0");
        });
    }

    [TestMethod]
    public async Task Deactivating_lock_vetoes_a_remounting_parameter_change()
    {
        UseRemount();
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rmlock/1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=locked]"));

        cut.Instance.LockState.Locked = true;
        await cut.InvokeAsync(() => brouter.Navigate("/rmlock/2"));

        cut.WaitForAssertion(() =>
        {
            // The instance about to be disposed votes with its deactivating lock (not the
            // renavigating one, which belongs to content that survives the change) - and the veto
            // holds: still doc 1, URL untouched.
            CollectionAssert.Contains(cut.Instance.LockState.Log, "doc:deactivating:Disposing:to=/rmlock/2");
            CollectionAssert.DoesNotContain(cut.Instance.LockState.Log, "doc:renavigating:to=/rmlock/2");
            StringAssert.Contains(cut.Find("[data-testid=locked]").TextContent, "doc 1");
            Assert.IsTrue(nav.Uri.EndsWith("/rmlock/1"));
        });
    }

    [TestMethod]
    public async Task LeaveGuard_fires_for_a_remounting_parameter_change()
    {
        // A parameter change on a route that keeps its instance is not a leave, so LeaveGuard stays
        // silent there (see LeaveGuardTests). Under a rebuild the content is disposed exactly as if
        // the route were left, so the route-declared guard gets its veto too.
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/rmguard/1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=guarded]"));

        cut.Instance.BlockGuard = true;
        await cut.InvokeAsync(() => brouter.Navigate("/rmguard/2"));

        cut.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new[] { "doc" }, cut.Instance.GuardFired);
            StringAssert.Contains(cut.Find("[data-testid=guarded]").TextContent, "doc 1");
        });

        cut.Instance.BlockGuard = false;
        await cut.InvokeAsync(() => brouter.Navigate("/rmguard/2"));

        cut.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new[] { "doc", "doc" }, cut.Instance.GuardFired);
            StringAssert.Contains(cut.Find("[data-testid=guarded]").TextContent, "doc 2");
        });
    }

    [TestMethod]
    public async Task Kept_child_hosted_in_a_rebuilt_parent_is_disposed_not_hidden()
    {
        UseRemount();
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/acct/1/draft");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:0"));

        cut.Find("[data-testid=kpinc]").Click();
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:1"));

        // Leaving the kept child while its host is rebuilt: retention cannot save it (the outlet it
        // lives in goes with the host's content), so its lock vote and its deactivation both say
        // Disposing - never Hidden, which would promise a survival that doesn't happen.
        await cut.InvokeAsync(() => brouter.Navigate("/acct/2"));

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Find("[data-testid=acct]").TextContent, "acct 2");
            Assert.AreEqual(0, cut.FindAll("[data-testid=kp]").Count);
            CollectionAssert.Contains(cut.Instance.DraftLockState.Log, "draft:deactivating:Disposing:to=/acct/2");
            CollectionAssert.AreEqual(
                new[] { "activated:first=True", "deactivated:Disposing" }, cut.Instance.DraftLog);
        });

        // Coming back starts a fresh session, consistent with what the content was told.
        await cut.InvokeAsync(() => brouter.Navigate("/acct/2/draft"));

        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:0");
            CollectionAssert.AreEqual(
                new[] { "activated:first=True", "deactivated:Disposing", "activated:first=True" },
                cut.Instance.DraftLog);
        });
    }

    [TestMethod]
    public async Task Kept_child_hosted_in_a_re_bound_parent_is_hidden_by_default()
    {
        // The control for the test above: with the host re-binding (the default), the kept child's
        // outlet survives the parameter change, so leaving the child is an ordinary Hidden and
        // returning resumes the retained instance.
        var (cut, brouter) = RenderAt<RemountHost>("http://localhost/acct/1/draft");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:0"));

        cut.Find("[data-testid=kpinc]").Click();
        await cut.InvokeAsync(() => brouter.Navigate("/acct/2"));
        cut.WaitForAssertion(() => CollectionAssert.Contains(cut.Instance.DraftLockState.Log, "draft:deactivating:Hidden:to=/acct/2"));

        await cut.InvokeAsync(() => brouter.Navigate("/acct/2/draft"));
        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:1");
            CollectionAssert.AreEqual(
                new[] { "activated:first=True", "deactivated:Hidden", "activated:first=False" },
                cut.Instance.DraftLog);
        });
    }
}
