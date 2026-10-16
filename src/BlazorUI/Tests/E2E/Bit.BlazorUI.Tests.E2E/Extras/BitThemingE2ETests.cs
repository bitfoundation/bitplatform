using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of the theming API on /e2e/theming and /e2e/theming/presets: BitThemeProvider scoping a theme to
/// its region (inline tokens, the family alias overlay, nesting and merging, the named cascade, RootElement and the
/// splatted attributes, Frozen, runtime reassignment and a provider with nothing to apply), BitThemeManager at runtime
/// (ApplyBitThemeAsync / ClearBitThemeOverridesAsync on the body and on an element, a seed-derived theme, SetThemeAsync
/// with name normalization and typed names, ToggleDarkLightAsync, SetThemePairAsync, following the OS, the persisted
/// preference), BitThemeNotifications, the JavaScript API reaching .NET, BitExternalThemeLoader, every packaged
/// preset - core light / dark and the Fluent 2, Material and Cupertino bundles - painting the document and scoped
/// regions with its own tokens, and the html attributes on /e2e/theming/attributes (bit-theme, bit-theme-default,
/// bit-theme-system with a renamed light / dark pair, bit-theme-color-meta, bit-theme-view-transition).
/// </summary>
[TestClass]
public class BitThemingE2ETests : E2ETestBase
{
    private const string Route = "/e2e/theming";
    private const string PresetsRoute = "/e2e/theming/presets";
    private const string AttributesRoute = "/e2e/theming/attributes";

    private const string DefaultPrimary = "rgb(18, 118, 198)";
    private const string Purple = "rgb(138, 43, 226)";
    private const string Cornsilk = "rgb(255, 248, 220)";
    private const string Green = "rgb(34, 139, 34)";
    private const string Orange = "rgb(255, 140, 0)";
    private const string Crimson = "rgb(220, 20, 60)";
    private const string White = "rgb(255, 255, 255)";

    [TestMethod]
    public async Task Prerender_TheProviderWritesItsTokensIntoTheServerHtml()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Route}")).TextAsync();

        var root = Regex.Match(html, "<div[^>]*id=\"provider-root\"[^>]*>");
        Assert.IsTrue(root.Success, "The provider's element is not in the prerendered HTML.");
        StringAssert.Contains(root.Value, "display:contents");
        StringAssert.Contains(root.Value, "--bit-clr-pri:#8A2BE2");
        StringAssert.Contains(root.Value, "bit-theme-overlay=");

        await OpenAsync(Route);
        await Expect(Page.Locator("#provider-button")).ToHaveCSSAsync("background-color", Purple);
    }

    [TestMethod]
    public async Task Provider_ScopesItsTheme_ToItsRegionOnly()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#provider-button")).ToHaveCSSAsync("background-color", Purple);
        await Expect(Page.Locator("#provider-surface")).ToHaveCSSAsync("background-color", Cornsilk);
        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(Page.Locator("#outside-surface")).ToHaveCSSAsync("background-color", White);

        var root = Page.Locator("#provider-root");
        await Expect(root).ToHaveCSSAsync("display", "contents");
        await Expect(root).ToHaveClassAsync(HasClass("provider-class"));
        await Expect(root).ToHaveAttributeAsync("data-region", "brand");
        await Expect(root).ToHaveAttributeAsync("bit-theme-overlay", new Regex(@"(^|\s)pri(\s|$)"));
        await Expect(root).ToHaveAttributeAsync("style", new Regex("--bit-clr-bg-pri:#FFF8DC"));

        // The cascaded value is the theme the region wears.
        await Expect(Page.Locator("#provider-reader")).ToHaveTextAsync("#8A2BE2|#FFF8DC");
    }

    [TestMethod]
    public async Task Provider_AThemeAssignedAtRuntime_RepaintsTheRegion_AndClearingItRemovesTheElement()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-provider-change").ClickAsync();

        await Expect(Page.Locator("#provider-button")).ToHaveCSSAsync("background-color", Orange);
        await Expect(Page.Locator("#provider-surface")).ToHaveCSSAsync("background-color", "rgb(240, 255, 255)");
        await Expect(Page.Locator("#provider-reader")).ToHaveTextAsync("#FF8C00|#F0FFFF");
        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", DefaultPrimary);

        await Page.Locator("#btn-provider-clear").ClickAsync();

        await Expect(Page.Locator("#provider-root")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#provider-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(Page.Locator("#provider-reader")).ToHaveTextAsync("(none)");
    }

    [TestMethod]
    public async Task Provider_AFamilyAliasBuiltFromAReValuedToken_IsReDeclaredInsideTheRegion()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#shape-root")).ToHaveAttributeAsync("bit-theme-overlay", new Regex(@"(^|\s)radius(\s|$)"));
        await Expect(Page.Locator("#shape-button")).ToHaveCSSAsync("border-top-left-radius", "12px");

        var outside = await ComputedStyleAsync("#outside-button", "border-top-left-radius");
        Assert.AreNotEqual("12px", outside);
    }

    [TestMethod]
    public async Task Nested_TheInnerProviderMergesOverTheOuter_AndAThemelessOnePassesTheCascadeThrough()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#nested-outer-button")).ToHaveCSSAsync("background-color", Purple);
        await Expect(Page.Locator("#nested-inner-button")).ToHaveCSSAsync("background-color", Green);

        // The inner theme sets no background, so the outer one's reaches its region - both as a token and in the cascade.
        await Expect(Page.Locator("#nested-inner-surface")).ToHaveCSSAsync("background-color", Cornsilk);
        await Expect(Page.Locator("#nested-reader")).ToHaveTextAsync("#228B22|#FFF8DC");

        await Expect(Page.Locator("#nested-pass-through")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#pass-through-reader")).ToHaveTextAsync("#8A2BE2|#FFF8DC");
    }

    [TestMethod]
    public async Task Named_ThemeNameRootElementAndAttributes_ShapeTheProviderElement()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#named-root");
        Assert.AreEqual("SECTION", await root.EvaluateAsync<string>("el => el.tagName"));

        // A style passed in is written after the provider's own, so it wins over display:contents.
        await Expect(root).ToHaveCSSAsync("display", "block");
        await Expect(root).ToHaveCSSAsync("border-top-width", "3px");

        await Expect(Page.Locator("#named-button")).ToHaveCSSAsync("background-color", Crimson);
        await Expect(Page.Locator("#named-reader")).ToHaveTextAsync("#DC143C");
        await Expect(Page.Locator("#unnamed-reader")).ToHaveTextAsync("(none)");
    }

    [TestMethod]
    public async Task Frozen_AnInPlaceMutationIsIgnored_WhileANewInstanceIsApplied()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#live-button")).ToHaveCSSAsync("background-color", Purple);
        await Expect(Page.Locator("#frozen-button")).ToHaveCSSAsync("background-color", Purple);

        await Page.Locator("#btn-mutate").ClickAsync();

        await Expect(Page.Locator("#live-button")).ToHaveCSSAsync("background-color", Green);
        await Expect(Page.Locator("#frozen-button")).ToHaveCSSAsync("background-color", Purple);

        await Page.Locator("#btn-reassign").ClickAsync();

        await Expect(Page.Locator("#live-button")).ToHaveCSSAsync("background-color", Crimson);
        await Expect(Page.Locator("#frozen-button")).ToHaveCSSAsync("background-color", Crimson);
    }

    [TestMethod]
    public async Task NoTheme_AProviderWithNothingToApply_RendersOnlyItsContent()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#noop-provider")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".noop-class")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#noop-child")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Apply_ToTheBody_RepaintsEveryComponent_AndClearingTakesItBack()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-apply-body").ClickAsync();

        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", Orange);
        await Expect(Page.Locator("#scoped-button")).ToHaveCSSAsync("background-color", Orange);
        await Expect(Page.Locator("body")).ToHaveAttributeAsync("style", new Regex("--bit-clr-pri"));
        await Expect(Page.Locator("body")).ToHaveAttributeAsync("bit-theme-overlay", new Regex(@"(^|\s)pri(\s|$)"));

        // A provider's own token still wins inside its region.
        await Expect(Page.Locator("#provider-button")).ToHaveCSSAsync("background-color", Purple);

        await Page.Locator("#btn-clear-body").ClickAsync();

        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(Page.Locator("body")).Not.ToHaveAttributeAsync("bit-theme-overlay", new Regex(".*"));
    }

    [TestMethod]
    public async Task Apply_ToAnElement_RepaintsOnlyItsSubtree()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-apply-scoped").ClickAsync();

        await Expect(Page.Locator("#scoped-button")).ToHaveCSSAsync("background-color", Green);
        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(Page.Locator("#scoped-target")).ToHaveAttributeAsync("bit-theme-overlay", new Regex("pri"));

        await Page.Locator("#btn-clear-scoped").ClickAsync();

        await Expect(Page.Locator("#scoped-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
    }

    [TestMethod]
    public async Task Apply_ADarkThemeDerivedFromASeed_DarkensTheSurfaces()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-apply-seed").ClickAsync();

        await Expect(Page.Locator("#outside-surface")).Not.ToHaveCSSAsync("background-color", White);
        var background = await ComputedStyleAsync("#outside-surface", "background-color");
        Assert.IsTrue(Luminance(background) < 0.2, $"The seeded dark theme left a light surface ({background}).");

        await Page.Locator("#btn-clear-body").ClickAsync();
        await Expect(Page.Locator("#outside-surface")).ToHaveCSSAsync("background-color", White);
    }

    [TestMethod]
    public async Task Manager_SetToggleAndGet_ChangeTheDocumentTheme_AndAreAnnounced()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");

        // The name is trimmed and lower-cased before it is written.
        await Page.Locator("#btn-set-dark").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#manager-result")).ToHaveTextAsync("dark");
        await Expect(Page.Locator("#outside-surface")).ToHaveCSSAsync("background-color", "rgb(15, 19, 24)");
        await Expect(Page.Locator("#notified-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#notified-new")).ToHaveTextAsync("dark");
        await Expect(Page.Locator("#notified-old")).ToHaveTextAsync("light");

        await Page.Locator("#btn-set-light-typed").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        await Expect(Page.Locator("#notified-count")).ToHaveTextAsync("2");

        await Page.Locator("#btn-toggle").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#manager-result")).ToHaveTextAsync("dark");

        await Page.Locator("#btn-get").ClickAsync();
        await Expect(Page.Locator("#manager-result")).ToHaveTextAsync("dark");

        // A blank name changes nothing.
        await Page.Locator("#btn-set-blank").ClickAsync();
        await Expect(Page.Locator("#manager-result")).ToHaveTextAsync("dark");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#notified-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Manager_System_FollowsTheOperatingSystemLive()
    {
        await OpenAsync(Route);

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });

        await Page.Locator("#btn-is-system-dark").ClickAsync();
        await Expect(Page.Locator("#system-dark")).ToHaveTextAsync("True");

        await Page.Locator("#btn-set-system").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        await Expect(Page.Locator("#notified-new")).ToHaveTextAsync("light");

        await Page.Locator("#btn-is-system-dark").ClickAsync();
        await Expect(Page.Locator("#system-dark")).ToHaveTextAsync("False");

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");

        // Pinning a theme stops the following.
        await Page.Locator("#btn-set-light-typed").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await NextFramesAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
    }

    [TestMethod]
    public async Task Manager_WithoutPersistence_HasNoPersistedPreference()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-set-dark").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");

        await Page.Locator("#btn-get-persisted").ClickAsync();
        await Expect(Page.Locator("#persisted")).ToHaveTextAsync("(none)");
        Assert.IsNull(await Page.EvaluateAsync<string?>("() => localStorage.getItem('bit-current-theme')"));
    }

    [TestMethod]
    public async Task Manager_SetThemePair_RenamesLightAndDark_ForTheToggle()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-set-pair").ClickAsync();

        var html = Page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("bit-theme-light", "fluent-light");
        await Expect(html).ToHaveAttributeAsync("bit-theme-dark", "fluent-dark");
        await Expect(html).ToHaveAttributeAsync("bit-theme", "fluent-light");
        await Expect(Page.Locator("#manager-result")).ToHaveTextAsync("fluent-light");

        await Page.Locator("#btn-toggle").ClickAsync();
        await Expect(html).ToHaveAttributeAsync("bit-theme", "fluent-dark");
        await Expect(Page.Locator("#outside-surface")).Not.ToHaveCSSAsync("background-color", White);
    }

    [TestMethod]
    public async Task ExternalLoader_APresetPaintsNothingUntilItsBundleIsAttached()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-set-cupertino").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "cupertino-light");

        // This page links no preset bundle, so the document stays on the Fluent tokens.
        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", DefaultPrimary);

        await Page.Locator("#btn-attach-bundle").ClickAsync();

        await Expect(Page.Locator("link#e2e-cupertino-bundle")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", "rgb(0, 113, 227)");

        await Page.Locator("#btn-detach-bundle").ClickAsync();

        await Expect(Page.Locator("link#e2e-cupertino-bundle")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#outside-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
    }

    [TestMethod]
    public async Task JsApi_AThemeSetFromScript_IsAnnouncedToDotNetAndToTheDocument()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-ensure").ClickAsync();
        await Expect(Page.Locator("#ensured")).ToHaveTextAsync("yes");

        await Page.EvaluateAsync("() => { window.__themeEvents = []; document.addEventListener('bit-theme-change', e => window.__themeEvents.push(JSON.stringify(e.detail))); }");

        await Page.EvaluateAsync("() => BitBlazorUI.Theme.set('dark')");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#notified-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#notified-new")).ToHaveTextAsync("dark");
        await Expect(Page.Locator("#notified-old")).ToHaveTextAsync("light");

        var events = await Page.EvaluateAsync<string[]>("() => window.__themeEvents");
        Assert.AreEqual(1, events.Length);
        StringAssert.Contains(events[0], "dark");

        Assert.AreEqual("dark", await Page.EvaluateAsync<string>("() => BitBlazorUI.Theme.get()"));
    }

    [TestMethod]
    [DataRow("light", "#1276C6", "#FFFFFF", false)]
    [DataRow("dark", "#4FA3F4", "#0F1318", false)]
    [DataRow("fluent2-light", "#0F6CBD", "#FFFFFF", false)]
    [DataRow("fluent2-dark", "#479EF5", "#131313", false)]
    [DataRow("material-light", "#1276C6", "#FFFFFF", true)]
    [DataRow("material-dark", "#4FA3F4", "#0C131B", true)]
    [DataRow("cupertino-light", "#0071E3", "#FFFFFF", true)]
    [DataRow("cupertino-dark", "#5AA1FF", "#131313", true)]
    public async Task Presets_EachPackagedPreset_PaintsTheDocumentWithItsOwnTokens(string preset, string primary, string background, bool pillButtons)
    {
        await OpenAsync(PresetsRoute);

        await Page.Locator($"#btn-preset-{preset}").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", preset);
        await Expect(Page.Locator("#applied")).ToHaveTextAsync(preset);

        var tokens = await Page.EvaluateAsync<string[]>("() => { const s = getComputedStyle(document.documentElement); return [s.getPropertyValue('--bit-clr-pri').trim(), s.getPropertyValue('--bit-clr-bg-pri').trim()]; }");
        Assert.AreEqual(primary, tokens[0], ignoreCase: true, $"--bit-clr-pri of {preset}");
        Assert.AreEqual(background, tokens[1], ignoreCase: true, $"--bit-clr-bg-pri of {preset}");

        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", ToRgb(primary));
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", ToRgb(background));

        var radius = ParsePx(await ComputedStyleAsync("#probe-button", "border-top-left-radius"));
        Assert.AreEqual(pillButtons, radius >= 16, $"The button corner of {preset} is {radius}px.");
    }

    [TestMethod]
    public async Task Presets_AScopedPreset_RepaintsOnlyItsRegion_AndTheNearestRegionWins()
    {
        await OpenAsync(PresetsRoute);

        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", DefaultPrimary);

        await Expect(Page.Locator("#scoped-material-dark-button")).ToHaveCSSAsync("background-color", "rgb(79, 163, 244)");
        await Expect(Page.Locator("#scope-material-dark")).ToHaveCSSAsync("background-color", "rgb(12, 19, 27)");

        await Expect(Page.Locator("#scoped-cupertino-light-button")).ToHaveCSSAsync("background-color", "rgb(0, 113, 227)");
        await Expect(Page.Locator("#scope-cupertino-light")).ToHaveCSSAsync("background-color", White);

        await Expect(Page.Locator("#scoped-fluent2-dark-button")).ToHaveCSSAsync("background-color", "rgb(71, 158, 245)");
        await Expect(Page.Locator("#scope-fluent2-dark")).ToHaveCSSAsync("background-color", "rgb(19, 19, 19)");

        // A document theme switched underneath leaves the scoped regions as they are.
        await Page.Locator("#btn-preset-dark").ClickAsync();
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", "rgb(79, 163, 244)");
        await Expect(Page.Locator("#scoped-cupertino-light-button")).ToHaveCSSAsync("background-color", "rgb(0, 113, 227)");
    }

    [TestMethod]
    public async Task Presets_TheNamesAndTheRegisteredSurfaces_AreTheOnesTheBundlesUse()
    {
        await OpenAsync(PresetsRoute);

        await Expect(Page.Locator("#name-fluent2-light")).ToHaveTextAsync("fluent2-light");
        await Expect(Page.Locator("#name-material-dark")).ToHaveTextAsync("material-dark");
        await Expect(Page.Locator("#name-cupertino-light")).ToHaveTextAsync("cupertino-light");
        await Expect(Page.Locator("#name-extra-material-light")).ToHaveTextAsync("material-light");

        // The first-paint surface registered for Material dark is the background its bundle paints.
        var surface = (await Page.Locator("#surface-material-dark").TextContentAsync())!.Trim();
        Assert.AreEqual("#0C131B", surface, ignoreCase: true);

        await Page.Locator("#btn-preset-material-dark").ClickAsync();
        await Expect(Page.Locator("#probe-surface")).ToHaveCSSAsync("background-color", ToRgb(surface));

        await AssertNoServerErrorsAsync();
    }



    [TestMethod]
    [DataRow("bit-theme=dark", "dark")]
    [DataRow("bit-theme-default=dark", "dark")]
    [DataRow("bit-theme=fluent-dark", "fluent-dark")]
    [DataRow("bit-theme=light&bit-theme-default=dark", "light")]
    public async Task Attributes_TheStartingThemeIsReadOffTheHtmlElement(string query, string expected)
    {
        await OpenAsync($"{AttributesRoute}?{query}");

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", expected);

        await Page.Locator("#btn-get").ClickAsync();
        await Expect(Page.Locator("#result")).ToHaveTextAsync(expected);
    }

    [TestMethod]
    public async Task Attributes_System_FollowsTheOperatingSystem_WithARenamedPair()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        await OpenAsync($"{AttributesRoute}?bit-theme-system=&bit-theme-light=fluent-light&bit-theme-dark=fluent-dark");

        var html = Page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("bit-theme", "fluent-dark");
        await Expect(Page.Locator("#probe-surface")).Not.ToHaveCSSAsync("background-color", White);

        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Expect(html).ToHaveAttributeAsync("bit-theme", "fluent-light");
        await Expect(Page.Locator("#notified-new")).ToHaveTextAsync("fluent-light");

        // The toggle flips within the renamed pair.
        await Page.Locator("#btn-toggle").ClickAsync();
        await Expect(html).ToHaveAttributeAsync("bit-theme", "fluent-dark");
        await Expect(Page.Locator("#result")).ToHaveTextAsync("fluent-dark");
    }

    [TestMethod]
    public async Task Attributes_ColorMeta_KeepsTheThemeColorTagWithTheLivePalette()
    {
        await OpenAsync($"{AttributesRoute}?bit-theme-color-meta=");

        var meta = Page.Locator("meta[name=theme-color]");
        await Expect(meta).ToHaveCountAsync(1);
        await Expect(meta).ToHaveAttributeAsync("content", new Regex("^#FFFFFF$", RegexOptions.IgnoreCase));

        await Page.Locator("#btn-toggle").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(meta).ToHaveAttributeAsync("content", new Regex("^#0F1318$", RegexOptions.IgnoreCase));

        // An accent re-derives the surfaces as an overlay on <body>, which the tag follows too.
        await Page.Locator("#btn-accent").ClickAsync();
        await Expect(meta).Not.ToHaveAttributeAsync("content", new Regex("^#0F1318$", RegexOptions.IgnoreCase));
        await Page.WaitForFunctionAsync("() => document.querySelector('meta[name=theme-color]').getAttribute('content') === getComputedStyle(document.body).getPropertyValue('--bit-clr-bg-pri').trim()");
    }

    [TestMethod]
    public async Task Attributes_ColorMeta_ReadsTheCustomPropertyItNames()
    {
        await OpenAsync($"{AttributesRoute}?bit-theme-color-meta=--bit-clr-bg-sec");

        var expected = await Page.EvaluateAsync<string>("() => getComputedStyle(document.body).getPropertyValue('--bit-clr-bg-sec').trim()");
        Assert.IsFalse(string.IsNullOrEmpty(expected));
        await Expect(Page.Locator("meta[name=theme-color]")).ToHaveAttributeAsync("content", expected);
    }

    [TestMethod]
    public async Task Attributes_ViewTransition_AnimatesASwap_UnlessReducedMotionIsAsked()
    {
        await OpenAsync($"{AttributesRoute}?bit-theme-view-transition=");

        if (await Page.EvaluateAsync<bool>("() => typeof document.startViewTransition === 'function'") is false)
        {
            Assert.Inconclusive("This engine has no View Transitions API; there the swap is instant by design.");
        }

        await Page.EvaluateAsync("() => { const original = document.startViewTransition.bind(document); window.__transitions = 0; document.startViewTransition = cb => { window.__transitions++; return original(cb); }; }");

        await Page.Locator("#btn-toggle").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");
        await Expect(Page.Locator("#result")).ToHaveTextAsync("dark");
        await Expect(Page.Locator("#notified-new")).ToHaveTextAsync("dark");
        Assert.AreEqual(1, await Page.EvaluateAsync<int>("() => window.__transitions"));

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.Locator("#btn-toggle").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        Assert.AreEqual(1, await Page.EvaluateAsync<int>("() => window.__transitions"));
    }



    private static string ToRgb(string hex)
    {
        var value = hex.TrimStart('#');
        var r = int.Parse(value[..2], NumberStyles.HexNumber);
        var g = int.Parse(value[2..4], NumberStyles.HexNumber);
        var b = int.Parse(value[4..6], NumberStyles.HexNumber);

        return $"rgb({r}, {g}, {b})";
    }

    private static double Luminance(string rgb)
    {
        var parts = Regex.Matches(rgb, @"\d+(\.\d+)?").Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture) / 255).ToArray();

        return 0.2126 * parts[0] + 0.7152 * parts[1] + 0.0722 * parts[2];
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", string.Empty), CultureInfo.InvariantCulture);
}
