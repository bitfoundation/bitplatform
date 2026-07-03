using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class NavigationEffectsTests : BunitTestContext
{
    private const string ModuleUrl = "./_content/Bit.Brouter/BitBrouter.js";

    [TestMethod]
    public void Fragment_navigation_invokes_applyNavigationEffects_with_the_hash()
    {
        // Default options: ScrollToFragment = true. Navigating to /docs#install should hand the
        // fragment to the JS effect so it can scroll #install into view.
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/docs#install");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() =>
        {
            var invocation = module.Invocations["applyNavigationEffects"].Single();
            Assert.AreEqual("#install", invocation.Arguments[0]);   // hash
            Assert.IsNull(invocation.Arguments[1]);                 // focus selector (unset)
            Assert.AreEqual(false, invocation.Arguments[2]);        // scrollToTop
        });
    }

    [TestMethod]
    public void Fragment_scroll_can_be_disabled()
    {
        // With ScrollToFragment = false and nothing else configured, a fragment navigation has no
        // DOM effect to apply, so the JS module is never even invoked.
        Services.Configure<BrouterOptions>(o => o.ScrollToFragment = false);
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/docs#install");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=docs]")));
        Assert.AreEqual(0, module.Invocations["applyNavigationEffects"].Count);
    }

    [TestMethod]
    public void Focus_selector_is_forwarded_after_navigation()
    {
        // FocusOnNavigateSelector configured -> the selector is forwarded so the JS effect can move
        // focus for assistive technologies, even on a fragment-less navigation.
        Services.Configure<BrouterOptions>(o => o.FocusOnNavigateSelector = "h1");
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() =>
        {
            var invocation = module.Invocations["applyNavigationEffects"].Single();
            Assert.IsNull(invocation.Arguments[0]);                 // no hash
            Assert.AreEqual("h1", invocation.Arguments[1]);         // focus selector
            Assert.AreEqual(false, invocation.Arguments[2]);        // scrollToTop
        });
    }

    [TestMethod]
    public void ScrollToTop_is_forwarded_when_configured()
    {
        Services.Configure<BrouterOptions>(o => o.ScrollBehavior = BrouterScrollMode.ToTop);
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() =>
        {
            var invocation = module.Invocations["applyNavigationEffects"].Single();
            Assert.IsNull(invocation.Arguments[0]);                 // no hash
            Assert.IsNull(invocation.Arguments[1]);                 // no focus selector
            Assert.AreEqual(true, invocation.Arguments[2]);         // scrollToTop
        });
    }

    [TestMethod]
    public void No_effects_configured_does_not_invoke_the_module()
    {
        // Default options with a fragment-less navigation: ScrollBehavior = None, no focus selector,
        // and ScrollToFragment has no fragment to act on. The JS module must not be imported/called.
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=home]")));
        Assert.AreEqual(0, module.Invocations["applyNavigationEffects"].Count);
    }

    [TestMethod]
    public void Fragment_takes_precedence_over_scroll_to_top()
    {
        // Both a fragment and ScrollBehavior.ToTop are in play. The service still forwards both
        // flags; the JS side is responsible for letting the fragment win. Assert the hash and the
        // scrollToTop flag are both forwarded so that precedence decision lives in one place (JS).
        Services.Configure<BrouterOptions>(o => o.ScrollBehavior = BrouterScrollMode.ToTop);
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/docs#install");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() =>
        {
            var invocation = module.Invocations["applyNavigationEffects"].Single();
            Assert.AreEqual("#install", invocation.Arguments[0]);
            Assert.AreEqual(true, invocation.Arguments[2]);
        });
    }
}
