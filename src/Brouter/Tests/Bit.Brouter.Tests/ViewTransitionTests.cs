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

    [TestMethod]
    public void Navigation_with_ViewTransitions_enabled_runs_the_begin_complete_handshake()
    {
        Services.Configure<BrouterOptions>(o => o.ViewTransitions = true);
        var module = SetupModule(beginReturns: true);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/a");
        var cut = RenderComponent<NavigationTypeHost>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=a]"));

        var brouter = Services.GetRequiredService<IBrouter>();
        cut.InvokeAsync(() => brouter.Navigate("/b"));

        cut.WaitForAssertion(() =>
        {
            Assert.IsNotNull(cut.Find("[data-testid=b]"));
            // Begin fired before the render, complete after it landed.
            Assert.IsTrue(module.Invocations.Count(i => i.Identifier == "beginViewTransition") >= 1);
            Assert.IsTrue(module.Invocations.Count(i => i.Identifier == "completeViewTransition") >= 1);

            // Begin carries the direction token (a programmatic Navigate is a push), the
            // default-animations flag and the reduced-motion flag (both on by default), which
            // drive the built-in direction-aware animations.
            var begin = module.Invocations.First(i => i.Identifier == "beginViewTransition");
            Assert.AreEqual("push", begin.Arguments[0]);
            Assert.AreEqual(true, begin.Arguments[1]);
            Assert.AreEqual(true, begin.Arguments[2]);
        });
    }

    [TestMethod]
    public void Unsupported_browser_skips_the_completion_round_trip()
    {
        Services.Configure<BrouterOptions>(o => o.ViewTransitions = true);
        var module = SetupModule(beginReturns: false);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
    public void Disabled_by_default_no_transition_interop_happens()
    {
        var module = SetupModule(beginReturns: true);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
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
