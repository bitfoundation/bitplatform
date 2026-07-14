using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class NavigationEffectsTests : BunitTestContext
{
    private const string ModuleUrl = "./_content/Bit.Brouter/bit-brouter.js";

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
    public void Restore_scroll_forwards_the_destination_url_as_restore_key()
    {
        // With RestoreScrollPosition enabled the destination's absolute URL is forwarded as the
        // restore key so the JS side can look up (on a Back/Forward) the position saved for it.
        Services.Configure<BrouterOptions>(o => o.RestoreScrollPosition = true);
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() =>
        {
            var invocation = module.Invocations["applyNavigationEffects"].Last();
            Assert.AreEqual("http://localhost/home", invocation.Arguments[3]);   // restore key = destination URL
        });
    }

    [TestMethod]
    public void Restore_scroll_disabled_sends_null_restore_key_and_never_saves()
    {
        // Restoration off: the restore key must be null (so the JS side leaves browser-native
        // restoration untouched) and no outgoing position is ever saved. ScrollBehavior.ToTop is set
        // only to force the module to be invoked so we can inspect the forwarded arguments.
        Services.Configure<BrouterOptions>(o => o.ScrollBehavior = BrouterScrollMode.ToTop);
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();

        cut.WaitForAssertion(() =>
        {
            var invocation = module.Invocations["applyNavigationEffects"].Last();
            Assert.IsNull(invocation.Arguments[3]);                 // no restore key
        });
        Assert.AreEqual(0, module.Invocations["saveScrollPosition"].Count);
    }

    [TestMethod]
    public void Restore_scroll_forwards_the_configured_storage_kind()
    {
        // LocalStorage persistence -> the "local" token is forwarded to both interop entry points so
        // the JS side mirrors positions into localStorage (and hydrates them on reload).
        Services.Configure<BrouterOptions>(o =>
        {
            o.RestoreScrollPosition = true;
            o.ScrollPositionStorage = BrouterScrollPositionStorage.LocalStorage;
        });
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();
        cut.WaitForAssertion(() =>
        {
            var effects = module.Invocations["applyNavigationEffects"].Last();
            Assert.AreEqual("local", effects.Arguments[4]);         // storage kind on the effects call
        });

        nav.NavigateTo("http://localhost/docs");
        cut.WaitForAssertion(() =>
        {
            var save = module.Invocations["saveScrollPosition"].Single();
            Assert.AreEqual("local", save.Arguments[1]);            // storage kind on the save call
        });
    }

    [TestMethod]
    public void Restore_scroll_defaults_to_in_memory_storage()
    {
        // Restoration on but storage left at the default -> null storage kind (in-memory only).
        Services.Configure<BrouterOptions>(o => o.RestoreScrollPosition = true);
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();
        cut.WaitForAssertion(() =>
        {
            var effects = module.Invocations["applyNavigationEffects"].Last();
            Assert.IsNull(effects.Arguments[4]);                    // no persistence
        });
    }

    [TestMethod]
    public void Restore_scroll_saves_the_outgoing_page_on_navigation_away()
    {
        // The scroll position is saved for the page being LEFT, keyed by its URL, and only once there
        // is a real page to leave: the initial load (from == Empty) saves nothing.
        Services.Configure<BrouterOptions>(o => o.RestoreScrollPosition = true);
        var module = Context!.JSInterop.SetupModule(ModuleUrl);

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/home");

        var cut = RenderComponent<NavigationEffectsHost>();
        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("[data-testid=home]")));

        // Initial load has no page to leave -> nothing saved yet.
        Assert.AreEqual(0, module.Invocations["saveScrollPosition"].Count);

        nav.NavigateTo("http://localhost/docs");

        cut.WaitForAssertion(() =>
        {
            var save = module.Invocations["saveScrollPosition"].Single();
            Assert.AreEqual("http://localhost/home", save.Arguments[0]);   // saved the page we left
        });
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
