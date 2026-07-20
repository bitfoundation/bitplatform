using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// The universal route lifecycle (<see cref="IBrouterRoute"/>) beyond the keep-alive transitions
/// covered in <see cref="KeepAliveTests"/>: transient routes (Disposing deactivations, fresh
/// sessions), same-instance renavigation, per-parameter keep-alive activate/deactivate pairs, and
/// interface discovery for router-instantiated Component pages.
/// </summary>
[TestClass]
public class RouteLifecycleTests : BunitTestContext
{
    [TestMethod]
    public async Task Transient_route_activates_on_show_and_gets_disposing_deactivation_on_leave()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/tl");
        var log = cut.Instance.ProbeLog;

        cut.WaitForAssertion(() => CollectionAssert.Contains(log, "activated:first=True"));

        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            // The content was unmounted - but the deactivation (with the Disposing reason) reached
            // the component before teardown, which plain IDisposable can't express.
            Assert.AreEqual(0, cut.FindAll("[data-testid=probe]").Count);
            CollectionAssert.Contains(log, "deactivated:Disposing");
        });

        // Returning creates a fresh instance and a fresh session: first activation again.
        await cut.InvokeAsync(() => brouter.Navigate("/tl"));
        cut.WaitForAssertion(() =>
            Assert.AreEqual(2, log.FindAll(e => e == "activated:first=True").Count));
    }

    [TestMethod]
    public async Task Singleton_route_parameter_change_fires_renavigation_on_the_same_instance()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/ren/1");
        cut.WaitForAssertion(() =>
            StringAssert.Contains(cut.Find("[data-testid=lifecycle]").TextContent, "activated:1"));

        await cut.InvokeAsync(() => brouter.Navigate("/ren/2"));
        cut.WaitForAssertion(() =>
        {
            var el = cut.Find("[data-testid=lifecycle]");
            // Same instance re-bound: no new activation, a renavigation instead - the discrete
            // "the user arrived here again" signal OnInitialized misses on instance reuse.
            StringAssert.Contains(el.TextContent, "activated:1");
            StringAssert.Contains(el.TextContent, "renavigations:1");
            StringAssert.Contains(el.TextContent, "deactivations:0");
        });
    }

    [TestMethod]
    public async Task PerParameter_keepalive_parameter_change_is_an_activate_deactivate_pair()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/ppl/1");
        cut.WaitForAssertion(() =>
            StringAssert.Contains(cut.Find("[data-testid=lifecycle]").TextContent, "activated:1"));

        await cut.InvokeAsync(() => brouter.Navigate("/ppl/2"));
        cut.WaitForAssertion(() =>
        {
            // Above KeepAliveMax 1, /ppl/1 and /ppl/2 are separate retained instances: the switch
            // hides instance 1 (Hidden deactivation) and first-activates instance 2 - never a
            // renavigation.
            var hidden = cut.Find("div[hidden] [data-testid=lifecycle]");
            StringAssert.Contains(hidden.TextContent, "deactivations:1");
            StringAssert.Contains(hidden.TextContent, "reasons:Hidden");
            StringAssert.Contains(hidden.TextContent, "renavigations:0");

            var visible = cut.FindAll("[data-testid=lifecycle]").Single(e => e.TextContent.Contains("active:True"));
            StringAssert.Contains(visible.TextContent, "activated:1");
            StringAssert.Contains(visible.TextContent, "first:1");
        });

        await cut.InvokeAsync(() => brouter.Navigate("/ppl/1"));
        cut.WaitForAssertion(() =>
        {
            // Instance 1 resumes: a repeat activation (not flagged first), still no renavigation.
            var visible = cut.FindAll("[data-testid=lifecycle]").Single(e => e.TextContent.Contains("active:True"));
            StringAssert.Contains(visible.TextContent, "activated:2");
            StringAssert.Contains(visible.TextContent, "first:1");
            StringAssert.Contains(visible.TextContent, "renavigations:0");
        });
    }

    [TestMethod]
    public async Task Component_route_page_implementing_the_interface_is_discovered_automatically()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/page");
        cut.WaitForAssertion(() =>
            StringAssert.Contains(cut.Find("[data-testid=lifecyclepage]").TextContent, "activations:1"));

        // A query-only navigation re-commits the same route/instance: renavigation.
        await cut.InvokeAsync(() => brouter.Navigate("/page?tab=2"));
        cut.WaitForAssertion(() =>
        {
            var el = cut.Find("[data-testid=lifecyclepage]");
            StringAssert.Contains(el.TextContent, "activations:1");
            StringAssert.Contains(el.TextContent, "renavigations:1");
        });
    }
}
