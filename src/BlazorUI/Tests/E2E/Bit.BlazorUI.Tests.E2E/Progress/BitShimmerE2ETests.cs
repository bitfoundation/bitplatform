using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Progress;

/// <summary>
/// Drives BitShimmer (/e2e/shimmer) in a real browser: the placeholder's box (Height, Width, every Shape, Circle,
/// Radius), Lines with Gap, LastLineWidth and LineWidths, every Animation actually running with its Duration, Delay
/// and Stagger, Inline inside a paragraph, the Loaded swap to ChildContent / Content with its reveal, ShowDelay and
/// MinShowTime timing, Overlay covering content already on the page (and keeping it out of the tab order), Template,
/// the live region (Label, LoadedLabel, every Politeness) and AriaLabel's progressbar, the BitParams cascade, every
/// Color and Background, every Size, Style / Class / Styles / Classes, the public --bit-Shimmer-* variables and the
/// parameters winning over them, reduced motion and ForceAnimation, RTL, the prerender and unmount.
/// </summary>
[TestClass]
public class BitShimmerE2ETests : E2ETestBase
{
    private const string Path = "/e2e/shimmer";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheBusyPlaceholderBeforeTheCircuitConnects()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        StringAssert.Contains(html, "<div id=\"smr-basic\" class=\"bit-smr-lin bit-smr\" aria-busy=\"true\"><div class=\"bit-smr-wrp  \" aria-hidden=\"true\">");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#smr-basic")).ToHaveAttributeAsync("aria-busy", "true");
    }

    [TestMethod]
    public async Task Basic_AFullWidthRoundedBar_WithARunningWave_HiddenFromAssistiveTechnologies()
    {
        await OpenAsync(Path);

        var root = Page.Locator("#smr-basic");
        await Expect(root).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveAttributeAsync("aria-hidden", "true");

        var box = await root.BoundingBoxAsync();
        Assert.AreEqual(374, Math.Round(box!.Width), "The full width of its container.");
        Assert.IsTrue(box.Height > 0);

        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-bg-sec)"));
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCSSAsync("border-top-left-radius", await ResolveAsync("border-top-left-radius", "var(--bit-shp-radius-surface)"));

        var band = root.Locator(".bit-smr-anm");
        await Expect(band).ToHaveCSSAsync("animation-name", "bit-smr-anim-wave");
        await Expect(band).ToHaveCSSAsync("animation-duration", "1.6s");
        var first = await band.EvaluateAsync<string>("el => getComputedStyle(el).transform");
        await Page.WaitForFunctionAsync("first => getComputedStyle(document.querySelector('#smr-basic .bit-smr-anm')).transform !== first", first);
    }

    [TestMethod]
    public async Task HeightAndWidth_SizeThePlaceholder()
    {
        await OpenAsync(Path);

        Assert.AreEqual(80, (await SizeAsync("#smr-height")).Height);
        await Expect(Page.Locator("#smr-height")).ToHaveAttributeAsync("style", "--bit-smr-hgt:80px");
        Assert.AreEqual(160, (await SizeAsync("#smr-width")).Width);
    }

    [TestMethod]
    [DataRow("shape-default", "bit-smr-lin", "var(--bit-shp-radius-surface)")]
    [DataRow("shape-Rounded", "bit-smr-lin", "var(--bit-shp-radius-surface)")]
    [DataRow("shape-Square", "bit-smr-sqr", "0px")]
    [DataRow("shape-Pill", "bit-smr-pil", "9999px")]
    [DataRow("radius", "bit-smr-lin", "16px")]
    [DataRow("radius-wins", "bit-smr-pil", "0px")]
    public async Task Shape_EveryValue_AndRadius_SetTheCorners(string id, string className, string radius)
    {
        await OpenAsync(Path);

        await Expect(Page.Locator($"#smr-{id}")).ToHaveClassAsync(HasClass(className));
        await Expect(Page.Locator($"#smr-{id} .bit-smr-wrp")).ToHaveCSSAsync("border-top-left-radius", await ResolveAsync("border-top-left-radius", radius));
    }

    [TestMethod]
    public async Task Shape_Circle_IsARoundDiscSizedByItsHeightOrWidth_WithOneLineOnly()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#smr-shape-Circle")).ToHaveClassAsync(HasClass("bit-smr-crl"));
        Assert.AreEqual((48, 48), await SizeAsync("#smr-shape-Circle"));
        await Expect(Page.Locator("#smr-shape-Circle .bit-smr-wrp")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#smr-shape-Circle .bit-smr-wrp")).ToHaveCSSAsync("border-top-left-radius", "50%");

        Assert.AreEqual((64, 64), await SizeAsync("#smr-circle-width"));
    }

    [TestMethod]
    public async Task Lines_StackIntoAParagraph_WithAGap_AndAShortLastLine()
    {
        await OpenAsync(Path);

        var lines = await LineBoxesAsync("#smr-lines");
        Assert.AreEqual(3, lines.Count);
        await Expect(Page.Locator("#smr-lines")).ToHaveClassAsync(HasClass("bit-smr-mln"));
        Assert.IsTrue(lines.All(l => Math.Abs(l.Height - 12) < 0.5), "Each line is Height tall.");
        Assert.AreEqual(374, lines[0].Width, 0.5);
        Assert.AreEqual(374 * 0.6, lines[2].Width, 0.5, "The last line is 60% wide by default.");
        Assert.AreEqual(8, lines[1].Y - (lines[0].Y + lines[0].Height), 0.5, "spacing(1) apart.");

        var gapped = await LineBoxesAsync("#smr-lines-gap");
        Assert.AreEqual(4, gapped.Count);
        Assert.AreEqual(16, gapped[1].Y - (gapped[0].Y + gapped[0].Height), 0.5);
        Assert.AreEqual(374 * 0.35, gapped[3].Width, 0.5);

        var measured = await LineBoxesAsync("#smr-lines-widths");
        Assert.AreEqual(374 * 0.88, measured[1].Width, 0.5);
        Assert.AreEqual(374 * 0.94, measured[2].Width, 0.5);
        Assert.AreEqual(374 * 0.6, measured[3].Width, 0.5, "A line LineWidths says nothing about keeps the last-line width.");
    }

    [TestMethod]
    [DataRow("default", "bit-smr-wav", "bit-smr-anim-wave")]
    [DataRow("Wave", "bit-smr-wav", "bit-smr-anim-wave")]
    [DataRow("Pulse", "bit-smr-pul", "bit-smr-anim-pulse")]
    [DataRow("pulse-flag", "bit-smr-pul", "bit-smr-anim-pulse")]
    [DataRow("Fade", "bit-smr-fad", "bit-smr-anim-fade")]
    public async Task Animation_EveryLoopingValue_RunsInTheBrowser(string id, string className, string keyframes)
    {
        await OpenAsync(Path);

        var band = Page.Locator($"#smr-anim-{id} .bit-smr-anm");

        await Expect(band).ToHaveClassAsync(HasClass(className));
        await Expect(band).ToHaveCSSAsync("animation-name", keyframes);
        await Expect(band).ToHaveCSSAsync("animation-iteration-count", "infinite");
        await Page.WaitForFunctionAsync($"() => document.querySelector('#smr-anim-{id} .bit-smr-anm').getAnimations().some(a => a.playState === 'running')");
    }

    [TestMethod]
    public async Task Animation_None_IsAStaticBlock_AndDurationDelayAndStaggerTimeTheLoop()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#smr-anim-None .bit-smr-anm")).ToHaveClassAsync(HasClass("bit-smr-non"));
        await Expect(Page.Locator("#smr-anim-None .bit-smr-anm")).ToHaveCSSAsync("display", "none");
        await Expect(Page.Locator("#smr-anim-None .bit-smr-wrp")).ToBeVisibleAsync();

        await Expect(Page.Locator("#smr-anim-timing .bit-smr-anm")).ToHaveCSSAsync("animation-duration", "5s");
        await Expect(Page.Locator("#smr-anim-timing .bit-smr-anm")).ToHaveCSSAsync("animation-delay", "1s");

        var delays = await Page.Locator("#smr-anim-stagger .bit-smr-anm").EvaluateAllAsync<string[]>("els => els.map(e => getComputedStyle(e).animationDelay)");
        CollectionAssert.AreEqual(new[] { "0s", "0.2s", "0.4s", "0.6s" }, delays);
    }

    [TestMethod]
    public async Task Inline_StandsInALineOfText_AsPhrasingContent()
    {
        await OpenAsync(Path);

        // The paragraph stays one paragraph, the shimmer a span inside it.
        await Expect(Page.Locator("#inline-paragraph > span#smr-inline.bit-smr-inl")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#inline-paragraph")).ToContainTextAsync("per month and renews on");
        Assert.AreEqual(0, await Page.Locator("#inline-paragraph div").CountAsync());

        await Expect(Page.Locator("#smr-inline")).ToHaveCSSAsync("display", "inline-block");
        var fontSize = Px(await ComputedStyleAsync("#inline-paragraph", "font-size"));
        Assert.AreEqual((64, (int)Math.Round(fontSize)), await SizeAsync("#smr-inline"), "Width, and 1em tall.");
    }

    [TestMethod]
    [Ignore("KNOWN BUG: an Inline shimmer without a Width is 0px wide under the Fluent theme - it falls back to "
          + "--bit-siz-ctrl-min-width, whose Fluent value is 'auto' (BitShimmer.scss .bit-smr-lin.bit-smr-inl), so the "
          + "placeholder the docs say takes the theme's minimum control width draws nothing.")]
    public async Task Inline_WithoutAWidth_FallsBackToAVisibleWidth()
    {
        await OpenAsync(Path);

        Assert.IsTrue((await Page.Locator("#smr-inline-default").BoundingBoxAsync())!.Width > 0);
    }

    [TestMethod]
    public async Task Loaded_SwapsThePlaceholderForTheContent_AndBackAgain()
    {
        await OpenAsync(Path);

        var root = Page.Locator("#smr-loaded");
        await Expect(Page.Locator("#loaded-content")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-loaded").ClickAsync();

        await Expect(Page.Locator("#loaded-content")).ToBeVisibleAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-smr-ldd"));
        await Expect(root).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCountAsync(0);
        await Expect(root.Locator("> .bit-smr-cnt")).ToHaveClassAsync(HasClass("bit-smr-rvl"));
        await Expect(root.Locator("> .bit-smr-cnt")).ToHaveCSSAsync("animation-name", "bit-smr-anim-opacity");
        await Expect(root).Not.ToHaveAttributeAsync("style", new Regex("--bit-smr-hgt"));

        // The Content parameter is the other way to pass it.
        await Expect(Page.Locator("#smr-loaded-content #content-parameter")).ToHaveTextAsync("From the Content parameter");

        await Page.Locator("#loaded-button").FocusAsync();
        await Expect(Page.Locator("#loaded-button")).ToBeFocusedAsync();

        await Page.Locator("#btn-toggle-loaded").ClickAsync();
        await Expect(Page.Locator("#loaded-content")).ToHaveCountAsync(0);
        await Expect(root).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task ShowDelay_KeepsAFastResponseFromFlashingAPlaceholder()
    {
        await OpenAsync(Path);
        await Page.Locator("#btn-start-timing").ClickAsync();

        var root = Page.Locator("#smr-show-delay");
        await Expect(root).ToHaveAttributeAsync("style", new Regex("--bit-smr-dly:2000ms"));
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCSSAsync("animation-delay", "2s");
        Assert.AreEqual("0", await root.Locator(".bit-smr-wrp").EvaluateAsync<string>("el => getComputedStyle(el).opacity"), "Not shown yet.");

        // Loaded before the delay is up: the content comes straight in, with no reveal of a placeholder never seen.
        await Page.Locator("#btn-finish-timing").ClickAsync();
        await Expect(Page.Locator("#show-delay-content")).ToBeVisibleAsync();
        await Expect(root.Locator("> .bit-smr-cnt")).Not.ToHaveClassAsync(HasClass("bit-smr-rvl"));

        // One with no delay was seen, so its content is revealed.
        await Expect(Page.Locator("#smr-plain > .bit-smr-cnt")).ToHaveClassAsync(HasClass("bit-smr-rvl"));
    }

    [TestMethod]
    public async Task ShowDelay_ThePlaceholderAppearsOnceTheDelayIsUp()
    {
        await OpenAsync(Path);
        await Page.Locator("#btn-start-timing").ClickAsync();

        var watch = Stopwatch.StartNew();
        // The placeholder is not in the page at all until the delay is up.
        await Page.WaitForFunctionAsync("() => { const el = document.querySelector('#smr-show-delay .bit-smr-wrp'); return !!el && getComputedStyle(el).opacity === '1'; }");
        Assert.IsTrue(watch.ElapsedMilliseconds >= 1500, $"Shown after {watch.ElapsedMilliseconds}ms of its 2000.");
    }

    [TestMethod]
    public async Task MinShowTime_HoldsAPlaceholderThatAppeared_FromVanishingInABlink()
    {
        await OpenAsync(Path);
        await Page.Locator("#btn-start-timing").ClickAsync();
        await Expect(Page.Locator("#smr-min-show")).ToHaveAttributeAsync("aria-busy", "true");

        var watch = Stopwatch.StartNew();
        await Page.Locator("#btn-finish-timing").ClickAsync();

        // The plain one is in at once; the held one keeps its placeholder, covered content included.
        await Expect(Page.Locator("#plain-content")).ToBeVisibleAsync();
        await Expect(Page.Locator("#min-show-content")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#smr-overlay-min")).ToHaveClassAsync(HasClass("bit-smr-ovl"));
        await Expect(Page.Locator("#overlay-min-content")).ToBeHiddenAsync();

        await Expect(Page.Locator("#min-show-content")).ToBeVisibleAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 2000, $"Held for {watch.ElapsedMilliseconds}ms of its 3000.");
        await Expect(Page.Locator("#overlay-min-content")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Overlay_CoversContentAlreadyOnThePage_KeepingItsSize_AndOutOfTheTabOrder()
    {
        await OpenAsync(Path);

        var root = Page.Locator("#smr-overlay");
        await Expect(root).ToHaveClassAsync(HasClass("bit-smr-ovl"));
        await Expect(Page.Locator("#overlay-content")).ToBeHiddenAsync();
        await Expect(root.Locator("> .bit-smr-cvd")).ToHaveCSSAsync("visibility", "hidden");

        // The cover is the size of the content under it, a single block whatever Lines says.
        Assert.AreEqual(90, (await SizeAsync("#smr-overlay")).Height);
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCountAsync(1);
        Assert.AreEqual(await SizeAsync("#smr-overlay"), await SizeAsync("#smr-overlay .bit-smr-wrp"));
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCSSAsync("border-top-left-radius", "8px");

        await Page.Locator("#before-overlay").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-overlay")).ToBeFocusedAsync();

        await Page.Locator("#btn-toggle-overlay").ClickAsync();
        await Expect(Page.Locator("#overlay-content")).ToBeVisibleAsync();
        await Expect(root.Locator(".bit-smr-wrp")).ToHaveCountAsync(0);

        await Page.Locator("#before-overlay").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#overlay-button")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Template_DrawsASkeletonOfItsOwn_UntilTheContentArrives()
    {
        await OpenAsync(Path);

        var root = Page.Locator("#smr-template");
        await Expect(root).ToHaveClassAsync(HasClass("bit-smr-tpl"));
        await Expect(root).ToHaveCSSAsync("animation-name", "bit-smr-anim-opacity");
        await Expect(root).ToHaveCSSAsync("animation-delay", "0.3s");
        await Expect(Page.Locator("#skeleton #smr-skeleton-circle")).ToHaveClassAsync(HasClass("bit-smr-crl"));
        await Expect(Page.Locator("#skeleton #smr-skeleton-line")).ToHaveClassAsync(HasClass("bit-smr-lin"));
        await Expect(root.Locator("> .bit-smr-wrp")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-template").ClickAsync();
        await Expect(Page.Locator("#template-content")).ToHaveTextAsync("Loaded card");
        await Expect(Page.Locator("#skeleton")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Label_IsALiveRegion_ThatSwapsToTheLoadedLabel()
    {
        await OpenAsync(Path);

        var region = Page.Locator("#smr-label + span.bit-smr-vhd");
        await Expect(region).ToHaveAttributeAsync("role", "status");
        await Expect(region).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(region).ToHaveAttributeAsync("aria-atomic", "true");
        await Expect(region).ToHaveTextAsync("Loading the profile");
        var box = await region.BoundingBoxAsync();
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1, "Heard, not seen.");

        await Expect(Page.Locator("#smr-assertive + span")).ToHaveAttributeAsync("role", "alert");
        await Expect(Page.Locator("#smr-assertive + span")).ToHaveAttributeAsync("aria-live", "assertive");
        await Expect(Page.Locator("#smr-off + span")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Page.Locator("#smr-off + span")).ToHaveAttributeAsync("aria-live", "off");

        await Page.Locator("#btn-toggle-a11y").ClickAsync();
        await Expect(region).ToHaveTextAsync("Profile loaded");
        await Expect(Page.Locator("#smr-label")).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
    }

    [TestMethod]
    public async Task Label_TheLiveRegionFollowsTheRootsVisibilityLanguageAndDirection()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#smr-label-hidden + span")).ToHaveAttributeAsync("hidden", "");
        await Expect(Page.Locator("#smr-label-lang + span")).ToHaveAttributeAsync("lang", "fr");
        await Expect(Page.Locator("#smr-label-lang + span")).ToHaveAttributeAsync("dir", "rtl");
    }

    [TestMethod]
    public async Task AriaLabel_NamesThePlaceholderAsAProgressbar_UntilItIsLoaded()
    {
        await OpenAsync(Path);

        await Expect(Page.GetByRole(AriaRole.Progressbar, new() { Name = "Loading the chart" })).ToHaveIdAsync("smr-aria-label");

        await Page.Locator("#btn-toggle-a11y").ClickAsync();
        await Expect(Page.Locator("#smr-aria-label")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Progressbar, new() { Name = "Loading the chart" })).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndAShimmersOwnValueWins()
    {
        await OpenAsync(Path);

        var lines = await LineBoxesAsync("#smr-params-1");
        Assert.IsTrue(lines.All(l => Math.Abs(l.Height - 12) < 0.5));
        Assert.AreEqual(374 * 0.4, lines[2].Width, 0.5);
        await Expect(Page.Locator("#smr-params-1 .bit-smr-anm").First).ToHaveClassAsync(HasClass("bit-smr-pul"));
        var delays = await Page.Locator("#smr-params-1 .bit-smr-anm").EvaluateAllAsync<string[]>("els => els.map(e => getComputedStyle(e).animationDelay)");
        CollectionAssert.AreEqual(new[] { "0s", "0.15s", "0.3s" }, delays);

        await Expect(Page.Locator("#smr-params-2 .bit-smr-anm").First).ToHaveClassAsync(HasClass("bit-smr-wav"));
        Assert.IsTrue((await LineBoxesAsync("#smr-params-2")).All(l => Math.Abs(l.Height - 20) < 0.5));
    }

    [TestMethod]
    [DataRow("Primary", "pri", "--bit-clr-pri")]
    [DataRow("Secondary", "sec", "--bit-clr-sec")]
    [DataRow("Tertiary", "ter", "--bit-clr-ter")]
    [DataRow("Info", "inf", "--bit-clr-inf")]
    [DataRow("Success", "suc", "--bit-clr-suc")]
    [DataRow("Warning", "wrn", "--bit-clr-wrn")]
    [DataRow("SevereWarning", "swr", "--bit-clr-swr")]
    [DataRow("Error", "err", "--bit-clr-err")]
    [DataRow("PrimaryBackground", "pbg", "--bit-clr-bg-pri")]
    [DataRow("SecondaryBackground", "sbg", "--bit-clr-bg-sec")]
    [DataRow("TertiaryBackground", "tbg", "--bit-clr-bg-ter")]
    [DataRow("PrimaryForeground", "pfg", "--bit-clr-fg-pri")]
    [DataRow("SecondaryForeground", "sfg", "--bit-clr-fg-sec")]
    [DataRow("TertiaryForeground", "tfg", "--bit-clr-fg-ter")]
    [DataRow("PrimaryBorder", "pbr", "--bit-clr-brd-pri")]
    [DataRow("SecondaryBorder", "sbr", "--bit-clr-brd-sec")]
    [DataRow("TertiaryBorder", "tbr", "--bit-clr-brd-ter")]
    public async Task ColorAndBackground_EveryValue_PaintTheAnimatedPartAndTheBoxBeneath(string color, string suffix, string token)
    {
        await OpenAsync(Path);

        var expected = await ResolveAsync("color", $"var({token})");

        await Expect(Page.Locator($"#smr-color-{color}")).ToHaveClassAsync(HasClass($"bit-smr-{suffix}"));
        await Expect(Page.Locator($"#smr-color-{color} .bit-smr-anm")).ToHaveCSSAsync("background-color", expected);

        await Expect(Page.Locator($"#smr-bg-{color} .bit-smr-wrp")).ToHaveClassAsync(HasClass($"bit-smr-b{suffix}"));
        await Expect(Page.Locator($"#smr-bg-{color} .bit-smr-wrp")).ToHaveCSSAsync("background-color", expected);
    }

    [TestMethod]
    public async Task Color_PaintsTheWaveBand()
    {
        await OpenAsync(Path);

        StringAssert.Contains(await ComputedStyleAsync("#smr-color-wave .bit-smr-anm", "background-image"), await ResolveAsync("color", "var(--bit-clr-err)"));
    }

    [TestMethod]
    [DataRow("Small", "sm", 16, "--bit-siz-ctrl-sm")]
    [DataRow("Medium", "md", 32, "--bit-siz-ctrl-md")]
    [DataRow("Large", "lg", 48, "--bit-siz-ctrl-lg")]
    public async Task Size_EveryValue_SetsTheLineHeightAndTheCircleDiameter(string size, string suffix, int lineHeight, string circleToken)
    {
        await OpenAsync(Path);

        await Expect(Page.Locator($"#smr-size-{size}")).ToHaveClassAsync(HasClass($"bit-smr-{suffix}"));
        Assert.AreEqual(lineHeight, (await SizeAsync($"#smr-size-{size}")).Height);

        // The control size is density-aware: its alias is computed where it is read.
        var token = $"var({circleToken}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var({circleToken}-steps)))";
        var diameter = (int)Math.Round(Px(await ResolveAsync("width", token)));
        Assert.AreEqual((diameter, diameter), await SizeAsync($"#smr-cir-size-{size}"));

        // An explicit Height wins over it.
        Assert.AreEqual(10, (await SizeAsync("#smr-size-height-wins")).Height);
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachEveryPart()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#smr-style")).ToHaveCSSAsync("border-top-color", "rgb(128, 128, 128)");
        await Expect(Page.Locator("#smr-style")).ToHaveClassAsync(HasClass("e2e-shimmer-class"));

        await Expect(Page.Locator("#smr-styles")).ToHaveCSSAsync("padding-top", "3px");
        await Expect(Page.Locator("#smr-styles")).ToHaveClassAsync(HasClass("e2e-root"));
        await Expect(Page.Locator("#smr-styles .bit-smr-wrp").First).ToHaveCSSAsync("background-color", "rgb(139, 69, 19)");
        await Expect(Page.Locator("#smr-styles .bit-smr-wrp").First).ToHaveClassAsync(HasClass("e2e-wrapper"));
        await Expect(Page.Locator("#smr-styles .bit-smr-anm").First).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#smr-styles .bit-smr-anm").First).ToHaveClassAsync(HasClass("e2e-shimmer"));
        await Expect(Page.Locator("#smr-styles + span.bit-smr-vhd")).ToHaveClassAsync(HasClass("e2e-label"));
        await Expect(Page.Locator("#smr-styles + span.bit-smr-vhd")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        await Expect(Page.Locator("#smr-styles-content > .bit-smr-cnt")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Page.Locator("#smr-styles-content > .bit-smr-cnt")).ToHaveClassAsync(HasClass("e2e-content"));

        await Expect(Page.Locator("#smr-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#smr-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleEveryShimmer_AndTheParametersWin()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#smr-css .bit-smr-wrp")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#smr-css .bit-smr-anm")).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
        Assert.AreEqual(21, (await SizeAsync("#smr-css")).Height);
        await Expect(Page.Locator("#smr-css .bit-smr-wrp")).ToHaveCSSAsync("border-top-left-radius", "7px");
        await Expect(Page.Locator("#smr-css .bit-smr-anm")).ToHaveCSSAsync("animation-duration", "3s");
        await Expect(Page.Locator("#smr-css .bit-smr-anm")).ToHaveCSSAsync("animation-delay", "0.2s");

        var lines = await LineBoxesAsync("#smr-css-lines");
        Assert.AreEqual(13, lines[1].Y - (lines[0].Y + lines[0].Height), 0.5);
        Assert.AreEqual(374 * 0.25, lines[1].Width, 0.5);
        Assert.AreEqual((44, 44), await SizeAsync("#smr-css-circle"));

        var parameters = Page.Locator("#smr-css-params");
        Assert.AreEqual(9, (await SizeAsync("#smr-css-params")).Height);
        await Expect(parameters.Locator(".bit-smr-wrp")).ToHaveCSSAsync("border-top-left-radius", "2px");
        await Expect(parameters.Locator(".bit-smr-wrp")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-suc)"));
        await Expect(parameters.Locator(".bit-smr-anm")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-err)"));
        await Expect(parameters.Locator(".bit-smr-anm")).ToHaveCSSAsync("animation-duration", "0.7s");
        await Expect(parameters.Locator(".bit-smr-anm")).ToHaveCSSAsync("animation-delay", "0.3s");
    }

    [TestMethod]
    public async Task ReducedMotion_StopsTheLoops_AndForceAnimationKeepsThem()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await OpenAsync(Path);

        foreach (var id in new[] { "default", "Pulse", "Fade" })
        {
            await Expect(Page.Locator($"#smr-anim-{id} .bit-smr-anm")).ToHaveCSSAsync("animation-name", "none");
        }

        await Expect(Page.Locator("#smr-anim-force")).ToHaveClassAsync(HasClass("bit-fam"));
        await Expect(Page.Locator("#smr-anim-force .bit-smr-anm")).ToHaveCSSAsync("animation-name", "bit-smr-anim-wave");
    }

    [TestMethod]
    public async Task Rtl_TheWaveSweepsInFromTheRight_AndTheLastLineStopsShortOfTheLeftEdge()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#smr-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#smr-rtl .bit-smr-anm").First).ToHaveCSSAsync("animation-direction", "reverse");
        await Expect(Page.Locator("#smr-rtl-inherited .bit-smr-anm")).ToHaveCSSAsync("animation-direction", "reverse");
        await Expect(Page.Locator("#smr-basic .bit-smr-anm")).ToHaveCSSAsync("animation-direction", "normal");

        var root = await Page.Locator("#smr-rtl").BoundingBoxAsync();
        var last = (await LineBoxesAsync("#smr-rtl"))[2];
        Assert.AreEqual(root!.X + root.Width, last.X + last.Width, 0.5, "The short line hugs the right edge.");
    }

    [TestMethod]
    public async Task Unmount_WhileAMinShowTimeHoldIsPending_LeavesNoError()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-load-mounted").ClickAsync();
        await Expect(Page.Locator("#smr-mounted")).ToHaveAttributeAsync("aria-busy", "true");

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#smr-mounted")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task<List<(double X, double Y, double Width, double Height)>> LineBoxesAsync(string root)
    {
        var boxes = await Page.Locator($"{root} > .bit-smr-wrp").EvaluateAllAsync<double[][]>(
            "els => els.map(e => { const b = e.getBoundingClientRect(); return [b.x, b.y, b.width, b.height]; })");

        return boxes.Select(b => (b[0], b[1], b[2], b[3])).ToList();
    }

    private async Task<(int Width, int Height)> SizeAsync(string selector)
    {
        var box = await Page.Locator(selector).First.BoundingBoxAsync();

        return ((int)Math.Round(box!.Width), (int)Math.Round(box.Height));
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}
