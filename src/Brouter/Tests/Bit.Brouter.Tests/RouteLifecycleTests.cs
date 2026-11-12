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

        cut.WaitForAssertion(() => Assert.Contains("activated:first=True", log));

        await cut.InvokeAsync(() => brouter.Navigate("/other"));
        cut.WaitForAssertion(() =>
        {
            // The content was unmounted - but the deactivation (with the Disposing reason) reached
            // the component before teardown, which plain IDisposable can't express.
            Assert.IsEmpty(cut.FindAll("[data-testid=probe]"));
            Assert.Contains("deactivated:Disposing", log);
        });

        // Returning creates a fresh instance and a fresh session: first activation again.
        await cut.InvokeAsync(() => brouter.Navigate("/tl"));
        cut.WaitForAssertion(() =>
            Assert.HasCount(2, log.FindAll(e => e == "activated:first=True")));
    }

    [TestMethod]
    public async Task Singleton_route_parameter_change_fires_renavigation_on_the_same_instance()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/ren/1");
        cut.WaitForAssertion(() =>
            Assert.Contains("activated:1", cut.Find("[data-testid=lifecycle]").TextContent));

        await cut.InvokeAsync(() => brouter.Navigate("/ren/2"));
        cut.WaitForAssertion(() =>
        {
            var el = cut.Find("[data-testid=lifecycle]");
            // Same instance re-bound: no new activation, a renavigation instead - the discrete
            // "the user arrived here again" signal OnInitialized misses on instance reuse.
            Assert.Contains("activated:1", el.TextContent);
            Assert.Contains("renavigations:1", el.TextContent);
            Assert.Contains("deactivations:0", el.TextContent);
        });
    }

    [TestMethod]
    public async Task PerParameter_keepalive_parameter_change_is_an_activate_deactivate_pair()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/ppl/1");
        cut.WaitForAssertion(() =>
            Assert.Contains("activated:1", cut.Find("[data-testid=lifecycle]").TextContent));

        await cut.InvokeAsync(() => brouter.Navigate("/ppl/2"));
        cut.WaitForAssertion(() =>
        {
            // Above KeepAliveMax 1, /ppl/1 and /ppl/2 are separate retained instances: the switch
            // hides instance 1 (Hidden deactivation) and first-activates instance 2 - never a
            // renavigation.
            var hidden = cut.Find("div[hidden] [data-testid=lifecycle]");
            Assert.Contains("deactivations:1", hidden.TextContent);
            Assert.Contains("reasons:Hidden", hidden.TextContent);
            Assert.Contains("renavigations:0", hidden.TextContent);

            var visible = cut.FindAll("[data-testid=lifecycle]").Single(e => e.TextContent.Contains("active:True"));
            Assert.Contains("activated:1", visible.TextContent);
            Assert.Contains("first:1", visible.TextContent);
        });

        await cut.InvokeAsync(() => brouter.Navigate("/ppl/1"));
        cut.WaitForAssertion(() =>
        {
            // Instance 1 resumes: a repeat activation (not flagged first), still no renavigation.
            var visible = cut.FindAll("[data-testid=lifecycle]").Single(e => e.TextContent.Contains("active:True"));
            Assert.Contains("activated:2", visible.TextContent);
            Assert.Contains("first:1", visible.TextContent);
            Assert.Contains("renavigations:0", visible.TextContent);
        });
    }

    [TestMethod]
    public async Task Component_route_page_implementing_the_interface_is_discovered_automatically()
    {
        var (cut, brouter) = RenderAt<KeepAliveHost>("http://localhost/page");
        cut.WaitForAssertion(() =>
            Assert.Contains("activations:1", cut.Find("[data-testid=lifecyclepage]").TextContent));

        // A query-only navigation re-commits the same route/instance: renavigation.
        await cut.InvokeAsync(() => brouter.Navigate("/page?tab=2"));
        cut.WaitForAssertion(() =>
        {
            var el = cut.Find("[data-testid=lifecyclepage]");
            Assert.Contains("activations:1", el.TextContent);
            Assert.Contains("renavigations:1", el.TextContent);
        });
    }
}
