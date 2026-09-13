using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class ViewTransitionTests : BunitTestContext
{
    private BunitJSModuleInterop SetupModule(bool beginReturns)
    {
        var module = Context!.JSInterop.SetupModule("./_content/Bit.Brouter/bit-brouter.js");
        module.Mode = JSRuntimeMode.Loose;
        // beginViewTransition(navKind, useDefaultAnimations): match any argument values.
        module.Setup<bool>("beginViewTransition", _ => true).SetResult(beginReturns);
        module.SetupVoid("completeViewTransition").SetVoidResult();
        return module;
    }

    // Waits for a JS invocation that no render follows (e.g. the transition completion fired from
    // OnAfterRenderAsync after the navigation's final render): render-driven WaitForAssertion never
    // re-checks in that window, so poll the interop log instead.
    private static async Task WaitForInvocationAsync(BunitJSModuleInterop module, string identifier)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (module.Invocations.All(i => i.Identifier != identifier))
        {
            if (DateTime.UtcNow > deadline) Assert.Fail($"JS invocation '{identifier}' was never made.");
            await Task.Delay(10);
        }
    }

    [TestMethod]
    public async Task Navigation_with_ViewTransitions_enabled_runs_the_begin_complete_handshake()
    {
        Services.Configure<BrouterOptions>(o => o.ViewTransitions = true);
        var module = SetupModule(beginReturns: true);

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");
        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        await cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() => cut.Find("[data-testid=b]"));

        // Begin fired before the render, complete after it landed.
        Assert.IsTrue(module.Invocations.Count(i => i.Identifier == "beginViewTransition") >= 1);
        await WaitForInvocationAsync(module, "completeViewTransition");

        // Begin carries the direction token (a programmatic Navigate is a push), the
        // default-animations flag and the reduced-motion flag (both on by default), which
        // drive the built-in direction-aware animations.
        var begin = module.Invocations.First(i => i.Identifier == "beginViewTransition");
        Assert.AreEqual("push", begin.Arguments[0]);
        Assert.AreEqual(true, begin.Arguments[1]);
        Assert.AreEqual(true, begin.Arguments[2]);
    }

    [TestMethod]
    public void Unsupported_browser_skips_the_completion_round_trip()
    {
        Services.Configure<BrouterOptions>(o => o.ViewTransitions = true);
        var module = SetupModule(beginReturns: false);

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");
        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.IsTrue(module.Invocations.Count(i => i.Identifier == "beginViewTransition") >= 1);
            Assert.AreEqual(0, module.Invocations.Count(i => i.Identifier == "completeViewTransition"));
        });
    }

    [TestMethod]
    public void Initial_load_does_not_start_a_transition()
    {
        Services.Configure<BrouterOptions>(o => o.ViewTransitions = true);
        var module = SetupModule(beginReturns: true);

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");
        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        // The first mount renders the initial route without any transition interop: there is no
        // outgoing page to animate from, and after prerendering a transition here would replay the
        // animation over the already-visible static HTML (a "double render" of the first page).
        Assert.AreEqual(0, module.Invocations.Count(i => i.Identifier == "beginViewTransition"));
        Assert.AreEqual(0, module.Invocations.Count(i => i.Identifier == "completeViewTransition"));

        // A real navigation afterwards still animates.
        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.IsTrue(module.Invocations.Count(i => i.Identifier == "beginViewTransition") >= 1);
        });
    }

    [TestMethod]
    public void Disabled_by_default_no_transition_interop_happens()
    {
        var module = SetupModule(beginReturns: true);

        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo("http://localhost/a");
        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            Assert.AreEqual(0, module.Invocations.Count(i => i.Identifier == "beginViewTransition"));
            Assert.AreEqual(0, module.Invocations.Count(i => i.Identifier == "completeViewTransition"));
        });
    }
}
