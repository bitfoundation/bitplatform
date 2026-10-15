using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitAccentColorSwitcher on /e2e/accent-color-switcher and its persistence pages: picking a swatch
/// repaints real components through the theme overlay (and picking the neutral one takes it back off), every switcher
/// shares the one accent, OnChange and AccentChanged, custom accents (name, aria label and bare-token fallbacks), an
/// accent applied from code, the re-derivation on a dark/light switch, the neutral swatch under Cupertino, the
/// aria-pressed / ring marking, keyboard use and the focus ring, hover, Style / Class / Styles / Classes, RTL, the
/// disabled contract, visibility, reduced motion and ForceAnimation, unmounting, SeedFromPrerender marking the
/// prerendered markup, the nonce of BitAccentColorHead, and the accent surviving a reload - from localStorage and the
/// cookie, and painted before the circuit connects by the StoredCss and StaticCss first-paint strategies.
/// </summary>
[TestClass]
public class BitAccentColorSwitcherE2ETests : E2ETestBase
{
    private const string Route = "/e2e/accent-color-switcher";

    private const string DefaultPrimary = "rgb(18, 118, 198)";
    private const string PurpleSwatch = "rgb(103, 80, 164)";

    private static readonly string[] DefaultNames = ["Default", "Purple", "Green", "Orange", "Teal", "Rose"];

    [TestMethod]
    public async Task Prerender_TheServerHtmlCarriesEverySwatch_BeforeItBecomesInteractive()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Route}")).TextAsync();

        var root = html.IndexOf("id=\"acs-basic\"", StringComparison.Ordinal);
        Assert.IsTrue(root > 0, "The switcher is not in the prerendered HTML.");
        var markup = html[root..html.IndexOf("</div>", root, StringComparison.Ordinal)];
        Assert.AreEqual(6, Regex.Matches(markup, "class=\"bit-acs-swt").Count);
        StringAssert.Contains(markup, "aria-pressed=\"true\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Route);
        await Expect(Swatches("acs-basic")).ToHaveCountAsync(6);
    }

    [TestMethod]
    public async Task Basic_RendersTheSixDefaultAccents_NamedAndPaintedInTheirOwnColor()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#acs-basic");
        await Expect(root).ToHaveAttributeAsync("role", "group");
        await Expect(root).ToHaveAttributeAsync("aria-label", "Accent color");
        await Expect(root).ToHaveClassAsync(HasClass("bit-acs"));

        var swatches = Swatches("acs-basic");
        await Expect(swatches).ToHaveCountAsync(DefaultNames.Length);

        for (var i = 0; i < DefaultNames.Length; i++)
        {
            await Expect(swatches.Nth(i)).ToHaveAttributeAsync("title", DefaultNames[i]);
            await Expect(swatches.Nth(i)).ToHaveAttributeAsync("aria-label", $"Apply the {DefaultNames[i]} accent color");
            await Expect(swatches.Nth(i)).ToHaveAttributeAsync("type", "button");
        }

        // The neutral swatch paints the packaged primary; the others their own hue.
        await Expect(swatches.Nth(0)).ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(swatches.Nth(1)).ToHaveCSSAsync("background-color", PurpleSwatch);
        await Expect(swatches.Nth(2)).ToHaveCSSAsync("background-color", "rgb(16, 124, 16)");
        await Expect(swatches.Nth(5)).ToHaveCSSAsync("background-color", "rgb(194, 57, 179)");
        await Expect(swatches.Nth(0)).ToHaveCSSAsync("width", "24px");
        await Expect(swatches.Nth(0)).ToHaveCSSAsync("border-top-left-radius", "50%");

        // Nothing is picked yet, so the neutral accent is the active one.
        await Expect(swatches.Nth(0)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(swatches.Nth(0)).ToHaveClassAsync(HasClass("bit-acs-act"));
        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#1276C6");
    }

    [TestMethod]
    public async Task Pick_ASwatch_RepaintsComponents_MarksIt_AndTheNeutralOneTakesItBack()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", DefaultPrimary);

        var swatches = Swatches("acs-basic");
        await swatches.Nth(1).ClickAsync();

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#6750A4");
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-last-color")).ToHaveTextAsync("#6750A4");
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(Page.Locator("body")).ToHaveAttributeAsync("style", new Regex("--bit-clr-pri"));

        // In the light scheme the seed is the primary itself.
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", PurpleSwatch);

        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(swatches.Nth(1)).ToHaveClassAsync(HasClass("bit-acs-act"));
        await Expect(swatches.Nth(1)).ToHaveCSSAsync("outline-style", "solid");
        await Expect(swatches.Nth(1)).ToHaveCSSAsync("outline-color", PurpleSwatch);
        await Expect(swatches.Nth(0)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(swatches.Nth(0)).Not.ToHaveClassAsync(HasClass("bit-acs-act"));

        // The swatch keeps painting its own color whatever the accent is.
        await Expect(swatches.Nth(0)).ToHaveCSSAsync("background-color", DefaultPrimary);

        await swatches.Nth(0).ClickAsync();

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#1276C6");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#basic-last-color")).ToHaveTextAsync("#1276C6");
        await Expect(Page.Locator("body")).Not.ToHaveAttributeAsync("style", new Regex("--bit-clr-pri"));
    }

    [TestMethod]
    public async Task EverySwitcher_ShowsTheOneSharedAccent_AndAccentChangedIsRaised()
    {
        await OpenAsync(Route);

        await Swatches("acs-basic").Nth(2).ClickAsync();

        await Expect(Swatches("acs-second").Nth(2)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Swatches("acs-second").Nth(0)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Swatches("acs-aria").Nth(2)).ToHaveClassAsync(HasClass("bit-acs-act"));
        await Expect(Page.Locator("#accent-changed-count")).ToHaveTextAsync("1");

        // A pick in the second one moves the first.
        await Swatches("acs-second").Nth(3).ClickAsync();

        await Expect(Swatches("acs-basic").Nth(3)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Swatches("acs-basic").Nth(2)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Page.Locator("#accent-changed-count")).ToHaveTextAsync("2");

        // Its own OnChange is the only one raised.
        await Expect(Page.Locator("#basic-change-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Custom_AccentsOfTheConfig_AreOffered_WithTheirFallbacks()
    {
        await OpenAsync(Route);

        var swatches = Swatches("acs-custom");
        await Expect(swatches).ToHaveCountAsync(3);

        await Expect(swatches.Nth(0)).ToHaveAttributeAsync("title", "Crimson");
        await Expect(swatches.Nth(0)).ToHaveAttributeAsync("aria-label", "Apply the Crimson accent color");
        await Expect(swatches.Nth(0)).ToHaveCSSAsync("background-color", "rgb(220, 20, 60)");

        // A bare token is given its # and painted; an aria label of its own wins over the composed one.
        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("aria-label", "Use indigo");
        await Expect(swatches.Nth(1)).ToHaveCSSAsync("background-color", "rgb(135, 100, 184)");

        // An accent with no name has no tooltip and a generic label.
        await Expect(swatches.Nth(2)).Not.ToHaveAttributeAsync("title", new Regex(".+"));
        await Expect(swatches.Nth(2)).ToHaveAttributeAsync("aria-label", "Apply this accent color");

        await swatches.Nth(0).ClickAsync();

        await Expect(Page.Locator("#custom-last-color")).ToHaveTextAsync("#DC143C");
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#dc143c");
        await Expect(swatches.Nth(0)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", DefaultPrimary);

        // None of the default swatches is it.
        await Expect(Page.Locator("#acs-basic .bit-acs-swt[aria-pressed=true]")).ToHaveCountAsync(0);

        await swatches.Nth(1).ClickAsync();
        await Expect(Page.Locator("#custom-last-color")).ToHaveTextAsync("8764B8");
        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task Code_AnAccentAppliedThroughTheService_RepaintsAndInvalidOnesAreIgnored()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-apply-code").ClickAsync();

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#0f9d58");
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", DefaultPrimary);
        await Expect(Page.Locator("#acs-basic .bit-acs-swt[aria-pressed=true]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#accent-changed-count")).ToHaveTextAsync("1");

        var applied = await SettledBackgroundAsync("#probe-button");

        await Page.Locator("#btn-apply-invalid").ClickAsync();
        await NextFramesAsync();

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#0f9d58");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", applied);
        await Expect(Page.Locator("#accent-changed-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task SchemeSwitch_ReDerivesThePickedAccentForTheDarkPalette()
    {
        await OpenAsync(Route);

        await Swatches("acs-basic").Nth(1).ClickAsync();
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", DefaultPrimary);
        var light = await SettledBackgroundAsync("#probe-button");

        await Page.Locator("#btn-toggle-scheme").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "dark");

        // A light overlay left on the dark theme would keep the light surfaces it carries.
        await Page.WaitForFunctionAsync("() => { const c = getComputedStyle(document.getElementById('probe-surface')).backgroundColor.match(/[0-9]+/g).map(Number); return (0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]) / 255 < 0.2; }");
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", light);
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", "rgb(79, 163, 244)");
        Assert.IsTrue(IsPurplish(await SettledBackgroundAsync("#probe-button")));

        await Page.Locator("#btn-toggle-scheme").ClickAsync();
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-theme", "light");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", light);
    }

    [TestMethod]
    public async Task Cupertino_TheNeutralSwatchPaintsThatPresetsSeed()
    {
        await OpenAsync(Route);

        await Expect(Swatches("acs-cupertino").Nth(0)).ToHaveCSSAsync("background-color", "rgb(0, 113, 227)");
        await Expect(Swatches("acs-basic").Nth(0)).ToHaveCSSAsync("background-color", DefaultPrimary);
    }

    [TestMethod]
    public async Task Keyboard_SwatchesAreTabStops_AppliedWithEnterOrSpace_AndShowTheFocusRing()
    {
        await OpenAsync(Route);

        var swatches = Swatches("acs-basic");
        await swatches.Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(swatches.Nth(1)).ToBeFocusedAsync();
        await Expect(swatches.Nth(1)).Not.ToHaveCSSAsync("box-shadow", "none");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#6750A4");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(swatches.Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#107C10");
        await Expect(swatches.Nth(2)).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(swatches.Nth(1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Hover_ScalesTheSwatchUp()
    {
        await OpenAsync(Route);

        var swatch = Swatches("acs-basic").Nth(3);
        await Expect(swatch).ToHaveCSSAsync("transform", "none");

        await swatch.HoverAsync();

        await Expect(swatch).ToHaveCSSAsync("transform", "matrix(1.15, 0, 0, 1.15, 0, 0)");
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRoot_AndStylesAndClassesReachTheSwatches()
    {
        await OpenAsync(Route);

        var styled = Page.Locator("#acs-style");
        await Expect(styled).ToHaveClassAsync(HasClass("custom-root-class"));
        await Expect(styled).ToHaveCSSAsync("padding-top", "7px");
        await Expect(styled).ToHaveCSSAsync("outline-style", "dashed");

        await Expect(Page.Locator("#acs-styles")).ToHaveCSSAsync("column-gap", "21px");
        await Expect(Swatches("acs-styles").Nth(3)).ToHaveCSSAsync("border-top-left-radius", "3px");
        await Expect(Swatches("acs-styles").Nth(0)).ToHaveCSSAsync("outline-color", "rgb(0, 128, 0)");

        await Expect(Page.Locator("#acs-classes")).ToHaveClassAsync(HasClass("c-root"));
        await Expect(Swatches("acs-classes").Nth(4)).ToHaveClassAsync(HasClass("c-swatch"));
        await Expect(Swatches("acs-classes").Nth(0)).ToHaveClassAsync(HasClass("c-active"));
        await Expect(Swatches("acs-classes").Nth(4)).Not.ToHaveClassAsync(HasClass("c-active"));

        // The active class and style follow the accent.
        await Swatches("acs-basic").Nth(4).ClickAsync();
        await Expect(Swatches("acs-classes").Nth(4)).ToHaveClassAsync(HasClass("c-active"));
        await Expect(Swatches("acs-classes").Nth(0)).Not.ToHaveClassAsync(HasClass("c-active"));
        await Expect(Swatches("acs-styles").Nth(4)).ToHaveCSSAsync("outline-color", "rgb(0, 128, 0)");
    }

    [TestMethod]
    public async Task Params_BitParamsHandsEverySwitcherItsConfigAndLook_AndAnOwnParameterWins()
    {
        await OpenAsync(Route);

        // The cascaded Config offers its accents, and the cascaded Dir and Classes reach the root and the swatches.
        var swatches = Swatches("acs-params");
        await Expect(swatches).ToHaveCountAsync(3);
        await Expect(swatches.Nth(0)).ToHaveAttributeAsync("title", "Crimson");
        await Expect(swatches.Nth(0)).ToHaveCSSAsync("background-color", "rgb(220, 20, 60)");
        await Expect(swatches.Nth(0)).ToHaveClassAsync(HasClass("cascaded-swatch"));
        await Expect(Page.Locator("#acs-params")).ToHaveClassAsync(HasClass("cascaded-root"));
        await Expect(Page.Locator("#acs-params")).ToHaveAttributeAsync("dir", "rtl");

        var first = await swatches.First.BoundingBoxAsync();
        var last = await swatches.Last.BoundingBoxAsync();
        Assert.IsTrue(first!.X > last!.X, $"first at {first.X}, last at {last.X}");

        // What the switcher sets itself wins over what is cascaded; what it does not set - the Config - is still filled in.
        var own = Swatches("acs-params-own");
        await Expect(own).ToHaveCountAsync(3);
        await Expect(own.Nth(0)).ToHaveClassAsync(HasClass("own-swatch"));
        await Expect(own.Nth(0)).Not.ToHaveClassAsync(HasClass("cascaded-swatch"));
        await Expect(Page.Locator("#acs-params-own")).ToHaveAttributeAsync("dir", "ltr");

        // A cascaded switcher picks like any other.
        await swatches.Nth(1).ClickAsync();
        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(own.Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_LayTheSwatchesOutRightToLeft()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acs-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#acs-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var (id, rtl) in new[] { ("acs-rtl", true), ("acs-rtl-inherited", true), ("acs-basic", false) })
        {
            var first = await Swatches(id).First.BoundingBoxAsync();
            var last = await Swatches(id).Last.BoundingBoxAsync();

            Assert.IsNotNull(first);
            Assert.IsNotNull(last);
            Assert.AreEqual(rtl, first.X > last.X, $"{id}: first at {first.X}, last at {last.X}");
        }
    }

    [TestMethod]
    public async Task Disabled_MarksEverySwatch_AndNothingActivatesIt()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#acs-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));

        var swatches = Swatches("acs-disabled");
        for (var i = 0; i < DefaultNames.Length; i++)
        {
            await Expect(swatches.Nth(i)).ToBeDisabledAsync();
            await Expect(swatches.Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(swatches.Nth(1)).ToHaveCSSAsync("cursor", "default");
        Assert.IsTrue(double.Parse(await ComputedStyleAsync("#acs-disabled .bit-acs-swt", "opacity"), CultureInfo.InvariantCulture) < 1);

        await swatches.Nth(1).ClickAsync(new() { Force = true });
        await root.DispatchEventAsync("click");

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#1276C6");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-change-count")).ToHaveTextAsync("0");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(swatches.Nth(1)).ToBeEnabledAsync();
        await Expect(swatches.Nth(1)).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await swatches.Nth(1).ClickAsync();

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#6750A4");
        await Expect(Page.Locator("#disabled-root-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-change-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task AriaHtmlAttributesAndVisibility_ReachTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acs-aria")).ToHaveAttributeAsync("aria-label", "Brand color");
        await Expect(Page.Locator("#acs-aria")).ToHaveAttributeAsync("data-testid", "accent-attrs");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Brand color" })).ToHaveCountAsync(1);

        await Expect(Page.Locator("#acs-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#acs-collapsed")).ToHaveCSSAsync("display", "none");
        Assert.IsNull(await Page.Locator("#acs-collapsed").BoundingBoxAsync());
    }

    [TestMethod]
    public async Task Runtime_ASwitcherMountedLater_MarksTheAccentInUse_AndUnmountsCleanly()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-runtime-mount").ClickAsync();
        await Expect(Page.Locator("#acs-runtime")).ToHaveCountAsync(0);

        await Swatches("acs-basic").Nth(5).ClickAsync();
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#C239B3");

        await Page.Locator("#btn-runtime-mount").ClickAsync();
        await Expect(Swatches("acs-runtime").Nth(5)).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Locator("#btn-runtime-mount").ClickAsync();
        await Swatches("acs-basic").Nth(0).ClickAsync();
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#1276C6");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Persistence_APickedAccent_IsRestoredAfterAReload_FromTheStores()
    {
        await OpenAsync("/e2e/accent-color-switcher/persist");

        await Swatches("acs-persist").Nth(1).ClickAsync();
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#6750A4");
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", DefaultPrimary);
        var accented = await SettledBackgroundAsync("#probe-button");

        var stored = await Page.EvaluateAsync<string?>("() => localStorage.getItem('bit-accent-color')");
        StringAssert.Contains(stored?.ToLowerInvariant() ?? string.Empty, "6750a4");
        Assert.IsTrue((await Page.Context.CookiesAsync()).Any(c => c.Name == "bit-accent-color"), "The accent cookie was not written.");

        // The None strategy leaves the document untouched before first paint.
        await Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("bit-accent", new Regex(".*"));

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#6750A4");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", accented);
        await Expect(Swatches("acs-persist").Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");

        // The neutral accent is stored as no preference at all.
        await Swatches("acs-persist").Nth(0).ClickAsync();
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
        Assert.IsNull(await Page.EvaluateAsync<string?>("() => localStorage.getItem('bit-accent-color')"));
        Assert.IsFalse((await Page.Context.CookiesAsync()).Any(c => c.Name == "bit-accent-color"));

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");
        await Expect(Swatches("acs-persist").Nth(0)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", DefaultPrimary);
    }

    [TestMethod]
    [DataRow("stored-css")]
    [DataRow("static-css")]
    public async Task FirstPaint_TheCssStrategies_PaintAndRingTheAccentBeforeTheCircuitConnects(string mode)
    {
        await OpenAsync($"/e2e/accent-color-switcher/{mode}");

        await Swatches("acs-persist").Nth(1).ClickAsync();
        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#6750A4");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-accent", new Regex("6750a4", RegexOptions.IgnoreCase));
        await Expect(Page.Locator("#probe-button")).Not.ToHaveCSSAsync("background-color", DefaultPrimary);
        var accented = await SettledBackgroundAsync("#probe-button");

        var snapshot = await Page.EvaluateAsync<string?>("() => localStorage.getItem('bit-accent-css')");
        Assert.AreEqual(mode == "stored-css", snapshot is not null, $"The palette snapshot of {mode}: {snapshot?[..Math.Min(40, snapshot.Length)]}");

        // Blazor's own script is served empty: what is painted now is the head script's and the stylesheet's alone.
        await Page.RouteAsync("**/_framework/blazor.web.js", route => route.FulfillAsync(new() { Status = 200, ContentType = "text/javascript", Body = "" }));
        await Page.ReloadAsync();

        await Expect(Page.Locator("#status")).ToHaveTextAsync("Loading");
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("bit-accent", new Regex("6750a4", RegexOptions.IgnoreCase));
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", accented);

        if (mode == "stored-css")
        {
            await Expect(Page.Locator("style#bit-accent-css")).ToHaveCountAsync(1);
        }

        // The marker CSS rings the visitor's swatch in the prerendered markup, and only that one.
        var swatches = Swatches("acs-persist");
        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("bit-accent-swatch", new Regex("6750a4", RegexOptions.IgnoreCase));
        await Expect(swatches.Nth(1)).ToHaveCSSAsync("outline-style", "solid");
        await Expect(swatches.Nth(0)).ToHaveCSSAsync("outline-style", "none");
    }



    [TestMethod]
    public async Task SeedFromPrerender_ThePrerenderedMarkupAlreadyMarksTheSeededAccent_AndTheClientAppliesIt()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/accent-color-switcher/seed")).TextAsync();

        var swatches = Regex.Matches(html, "<button[^>]*class=\"bit-acs-swt[^>]*>").Select(m => m.Value).ToArray();
        Assert.AreEqual(6, swatches.Length);
        StringAssert.Contains(swatches[2], "aria-pressed=\"true\"");
        StringAssert.Contains(swatches[0], "aria-pressed=\"false\"");

        await OpenAsync("/e2e/accent-color-switcher/seed");

        await Expect(Page.Locator("#active-accent")).ToHaveTextAsync("#107C10");
        await Expect(Swatches("acs-persist").Nth(2)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#probe-button")).ToHaveCSSAsync("background-color", "rgb(16, 124, 16)");
    }

    [TestMethod]
    public async Task Head_TheFirstPaintScriptCarriesTheNonceItIsGiven()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/accent-color-switcher/stored-css")).TextAsync();
        var head = html[..html.IndexOf("</head>", StringComparison.Ordinal)];

        StringAssert.Contains(head, "<script nonce=\"e2e-nonce\">");
        StringAssert.Contains(head, "<style nonce=\"e2e-nonce\">");

        // The None strategy emits nothing at all.
        var none = await (await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/accent-color-switcher/persist")).TextAsync();
        Assert.IsFalse(none[..none.IndexOf("</head>", StringComparison.Ordinal)].Contains("e2e-nonce", StringComparison.Ordinal));
    }



    [TestMethod]
    public async Task ReducedMotion_CollapsesTheHoverTransition_UnlessForceAnimationIsSet()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acs-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        var full = await TransitionSecondsAsync("#acs-basic .bit-acs-swt");
        Assert.IsTrue(full > 0.05, $"The swatch has no hover transition to collapse ({full}s).");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Page.WaitForFunctionAsync("() => parseFloat(getComputedStyle(document.querySelector('#acs-basic .bit-acs-swt')).transitionDuration) < 0.01");
        Assert.AreEqual(full, await TransitionSecondsAsync("#acs-forced .bit-acs-swt"), 0.001);
    }



    private ILocator Swatches(string switcherId) => Page.Locator($"#{switcherId} .bit-acs-swt");

    /// <summary>
    /// The background of an element once its color transition has run: a button eases into a new primary, so a read
    /// taken right after an accent lands would catch a color halfway between the two.
    /// </summary>
    private async Task<string> SettledBackgroundAsync(string selector)
    {
        await Page.Locator(selector).EvaluateAsync("el => Promise.all(el.getAnimations().map(a => a.finished.catch(() => null)))");
        await Page.WaitForFunctionAsync($"() => document.querySelector('{selector}').getAnimations().length === 0");

        return await ComputedStyleAsync(selector, "background-color");
    }

    private async Task<double> TransitionSecondsAsync(string selector)
    {
        var value = await ComputedStyleAsync(selector, "transition-duration");
        var first = value.Split(',')[0].Trim();

        return first.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(first[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(first.TrimEnd('s'), CultureInfo.InvariantCulture);
    }

    private static double[] Channels(string rgb) => Regex.Matches(rgb, @"\d+(\.\d+)?").Take(3).Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).ToArray();

    private static bool IsPurplish(string rgb)
    {
        var c = Channels(rgb);

        // Red and blue both clearly above green.
        return c[0] > c[1] && c[2] > c[1];
    }
}
