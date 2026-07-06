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
    public async Task KeepAlive_route_preserves_component_state_across_navigations()
    {
        var (cut, brouter) = RenderAt("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            // Still mounted, but inside the hidden wrapper.
            Assert.IsNotNull(cut.Find("div[hidden] [data-testid=stateful]"));
        });

        await cut.InvokeAsync(() => brouter.Navigate("/ka"));
        cut.WaitForAssertion(() =>
        {
            // Visible again - and the component instance (with its state) survived.
            Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count);
            Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);
        });
    }

    [TestMethod]
    public async Task Plain_route_recreates_its_component_on_return()
    {
        var (cut, brouter) = RenderAt("http://localhost/plain");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=other]"));
            // Not keep-alive: unmounted entirely.
            Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count);
        });

        await cut.InvokeAsync(() => brouter.Navigate("/plain"));
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public async Task KeepAlive_works_through_a_parent_outlet_for_sibling_switches()
    {
        var (cut, brouter) = RenderAt("http://localhost/parent/k1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => brouter.Navigate("/parent/k2"));
        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=k2]"));
            // k1 is retained hidden inside the outlet's kept region.
            Assert.IsNotNull(cut.Find("div[hidden] [data-testid=stateful]"));
        });

        await cut.InvokeAsync(() => brouter.Navigate("/parent/k1"));
        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);
            Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=stateful]").Count);
            // k2 was transient: gone.
            Assert.AreEqual(0, cut.FindAll("[data-testid=k2]").Count);
        });
    }

    [TestMethod]
    public async Task KeepAlive_context_signals_deactivate_and_reactivate_transitions()
    {
        var (cut, brouter) = RenderAt("http://localhost/kl");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find("[data-testid=lifecycle]").TextContent, "active:True"));

        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            // Kept mounted but hidden - and the component was told it is now inactive.
            var el = cut.Find("div[hidden] [data-testid=lifecycle]");
            StringAssert.Contains(el.TextContent, "active:False");
            StringAssert.Contains(el.TextContent, "deactivations:1");
        });

        await cut.InvokeAsync(() => brouter.Navigate("/kl"));
        cut.WaitForAssertion(() =>
        {
            var el = cut.Find("[data-testid=lifecycle]");
            StringAssert.Contains(el.TextContent, "active:True");
            // Same instance survived, so the transition counters accumulated rather than reset.
            StringAssert.Contains(el.TextContent, "deactivations:1");
            StringAssert.Contains(el.TextContent, "activations:1");
        });
    }

    [TestMethod]
    public async Task ClearKeepAlive_drops_retained_state_so_returning_recreates()
    {
        var (cut, brouter) = RenderAt("http://localhost/ka");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:2", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("div[hidden] [data-testid=stateful]")));

        // Evict the retained page: the hidden instance is disposed and its state released.
        await cut.InvokeAsync(() => brouter.ClearKeepAlive());
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("[data-testid=stateful]").Count));

        // Returning now recreates it fresh (count back to 0), proving the state was really dropped.
        await cut.InvokeAsync(() => brouter.Navigate("/ka"));
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    [TestMethod]
    public async Task KeepAlive_is_keyed_per_route_not_per_parameter_value()
    {
        // Documents the deliberate DEFAULT (KeepAliveMax unset => 1): a parameterized keep-alive
        // route keeps ONE live instance, reused across parameter values - not a separate cached
        // page per value. Set KeepAliveMax > 1 for per-parameter retention (tests below).
        var (cut, brouter) = RenderAt("http://localhost/item/1");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "id:1"));

        cut.Find("[data-testid=kpinc]").Click();
        StringAssert.Contains(cut.Find("[data-testid=kp]").TextContent, "count:1");

        await cut.InvokeAsync(() => brouter.Navigate("/item/2"));
        cut.WaitForAssertion(() =>
        {
            var t = cut.Find("[data-testid=kp]").TextContent;
            StringAssert.Contains(t, "id:2");     // the same instance re-binds to the new parameter value
            StringAssert.Contains(t, "count:1");  // and its state carries over - it was NOT reset or separately cached
        });

        // There is only ever one instance for the route: no hidden retained variant for id=1.
        Assert.AreEqual(0, cut.FindAll("div[hidden] [data-testid=kp]").Count);
    }

    [TestMethod]
    public async Task KeepAlive_state_is_lost_across_the_hosting_layout_unmount()
    {
        // Documents the lifetime boundary: keep-alive survives sibling switches under a layout, but
        // not the layout's own unmount. Leaving /parent disposes the outlet-hosted k1, so returning
        // rebuilds it fresh (count back to 0) rather than restoring the count:1 we left with.
        var (cut, brouter) = RenderAt("http://localhost/parent/k1");
        cut.WaitForAssertion(() => cut.Find("[data-testid=stateful]"));

        cut.Find("[data-testid=inc]").Click();
        Assert.AreEqual("count:1", cut.Find("[data-testid=stateful]").TextContent);

        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=other]")));

        await cut.InvokeAsync(() => brouter.Navigate("/parent/k1"));
        cut.WaitForAssertion(() => Assert.AreEqual("count:0", cut.Find("[data-testid=stateful]").TextContent));
    }

    // ---- Per-parameter keep-alive (KeepAliveMax > 1) ----

    private const string VisibleKp = "div:not([hidden]) > [data-testid=kp]";
    private const string HiddenKp = "div[hidden] > [data-testid=kp]";
    private const string VisibleInc = "div:not([hidden]) > [data-testid=kpinc]";

    [TestMethod]
    public async Task KeepAliveMax_keeps_separate_state_per_parameter_value()
    {
        var (cut, brouter) = RenderAt("http://localhost/multi/1");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(VisibleKp).TextContent, "id:1"));

        cut.Find(VisibleInc).Click();
        StringAssert.Contains(cut.Find(VisibleKp).TextContent, "count:1");

        // Visit id=2: a SEPARATE instance mounts (fresh count), while id=1 is retained hidden with
        // its state and parameter binding frozen.
        await cut.InvokeAsync(() => brouter.Navigate("/multi/2"));
        cut.WaitForAssertion(() =>
        {
            var visible = cut.Find(VisibleKp).TextContent;
            StringAssert.Contains(visible, "id:2");
            StringAssert.Contains(visible, "count:0");
            var hidden = cut.Find(HiddenKp).TextContent;
            StringAssert.Contains(hidden, "id:1");
            StringAssert.Contains(hidden, "count:1");
        });

        cut.Find(VisibleInc).Click();
        cut.Find(VisibleInc).Click();
        StringAssert.Contains(cut.Find(VisibleKp).TextContent, "count:2");

        // Return to id=1: its exact state resumes; id=2 is now the hidden one with count:2.
        await cut.InvokeAsync(() => brouter.Navigate("/multi/1"));
        cut.WaitForAssertion(() =>
        {
            var visible = cut.Find(VisibleKp).TextContent;
            StringAssert.Contains(visible, "id:1");
            StringAssert.Contains(visible, "count:1");
            var hidden = cut.Find(HiddenKp).TextContent;
            StringAssert.Contains(hidden, "id:2");
            StringAssert.Contains(hidden, "count:2");
        });
    }

    [TestMethod]
    public async Task KeepAliveMax_evicts_the_least_recently_used_entry_beyond_the_budget()
    {
        var (cut, brouter) = RenderAt("http://localhost/multi/1");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(VisibleKp).TextContent, "id:1"));
        cut.Find(VisibleInc).Click();

        await cut.InvokeAsync(() => brouter.Navigate("/multi/2"));
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(VisibleKp).TextContent, "id:2"));
        cut.Find(VisibleInc).Click();

        // Third distinct value with a budget of 2: id=1 (least recently used) is evicted/disposed.
        await cut.InvokeAsync(() => brouter.Navigate("/multi/3"));
        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Find(VisibleKp).TextContent, "id:3");
            Assert.AreEqual(2, cut.FindAll("[data-testid=kp]").Count); // id:3 visible + id:2 hidden
            StringAssert.Contains(cut.Find(HiddenKp).TextContent, "id:2");
        });

        // id=2 survived within the budget: its state resumes.
        await cut.InvokeAsync(() => brouter.Navigate("/multi/2"));
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(VisibleKp).TextContent, "count:1"));

        // id=1 was evicted: recreated fresh.
        await cut.InvokeAsync(() => brouter.Navigate("/multi/1"));
        cut.WaitForAssertion(() =>
        {
            var visible = cut.Find(VisibleKp).TextContent;
            StringAssert.Contains(visible, "id:1");
            StringAssert.Contains(visible, "count:0");
        });
    }

    [TestMethod]
    public async Task KeepAliveMax_keeps_per_parameter_state_through_a_parent_outlet()
    {
        var (cut, brouter) = RenderAt("http://localhost/mparent/mi/1");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(VisibleKp).TextContent, "id:1"));
        cut.Find(VisibleInc).Click();
        StringAssert.Contains(cut.Find(VisibleKp).TextContent, "count:1");

        await cut.InvokeAsync(() => brouter.Navigate("/mparent/mi/2"));
        cut.WaitForAssertion(() =>
        {
            StringAssert.Contains(cut.Find(VisibleKp).TextContent, "id:2");
            // id=1's subtree is retained hidden inside the outlet, parameters frozen.
            var hidden = cut.Find(HiddenKp).TextContent;
            StringAssert.Contains(hidden, "id:1");
            StringAssert.Contains(hidden, "count:1");
        });

        await cut.InvokeAsync(() => brouter.Navigate("/mparent/mi/1"));
        cut.WaitForAssertion(() =>
        {
            var visible = cut.Find(VisibleKp).TextContent;
            StringAssert.Contains(visible, "id:1");
            StringAssert.Contains(visible, "count:1");
        });
    }

    [TestMethod]
    public async Task ClearKeepAlive_drops_hidden_per_parameter_entries_but_keeps_the_active_one()
    {
        var (cut, brouter) = RenderAt("http://localhost/multi/1");
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(VisibleKp).TextContent, "id:1"));
        cut.Find(VisibleInc).Click();

        await cut.InvokeAsync(() => brouter.Navigate("/multi/2"));
        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll("[data-testid=kp]").Count));
        cut.Find(VisibleInc).Click();

        await cut.InvokeAsync(() => brouter.ClearKeepAlive());
        cut.WaitForAssertion(() =>
        {
            // Only the active (id=2) instance survives, its state intact.
            Assert.AreEqual(1, cut.FindAll("[data-testid=kp]").Count);
            var visible = cut.Find(VisibleKp).TextContent;
            StringAssert.Contains(visible, "id:2");
            StringAssert.Contains(visible, "count:1");
        });

        // The dropped id=1 entry really was disposed: returning recreates it fresh.
        await cut.InvokeAsync(() => brouter.Navigate("/multi/1"));
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(VisibleKp).TextContent, "count:0"));
    }
}
