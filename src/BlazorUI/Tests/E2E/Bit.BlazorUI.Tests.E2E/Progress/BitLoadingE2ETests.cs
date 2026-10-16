using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Progress;

/// <summary>
/// Drives the BitLoading family (/e2e/loading) in a real browser - every one of the eighteen loaders (Bars,
/// BouncingDots, Circle, DotsRing, DualRing, Ellipsis, Grid, Heart, Hourglass, OrbitingDots, Ring, Ripple, Roller,
/// RollingDashes, RollingSquare, SlickBars, Spinner, Xbox): its animation actually running, its live-region status
/// with the hidden drawing, every Size and CustomSize, Color and CustomColor painting the drawing, Label, Speed,
/// Paused, Thickness on the stroked ones, reduced motion and ForceAnimation. The shared BitLoading shell is driven
/// through Ring and Spinner: every LabelPlacement (left to right and right to left), LabelTemplate, Delay, Inline,
/// Role / AriaLive / AriaLabel and the splatted attributes, the BitParams cascade, Style / Class / Styles / Classes,
/// the public --bit-Loading-* variables and the parameters winning over them, Visibility, Disabled, the prerender,
/// runtime re-renders and unmount.
/// </summary>
[TestClass]
public class BitLoadingE2ETests : E2ETestBase
{
    private const string Path = "/e2e/loading";

    // The loaders and the class prefix each one's parts and keyframes are named by.
    private static readonly (string Name, string Code)[] Loaders =
    [
        ("Bars", "bar"), ("BouncingDots", "bnd"), ("Circle", "cir"), ("DotsRing", "dor"), ("DualRing", "dur"),
        ("Ellipsis", "elp"), ("Grid", "grd"), ("Heart", "hrt"), ("Hourglass", "hgl"), ("OrbitingDots", "ord"),
        ("Ring", "rng"), ("Ripple", "rpl"), ("Roller", "rol"), ("RollingDashes", "rld"), ("RollingSquare", "rsq"),
        ("SlickBars", "sbr"), ("Spinner", "spn"), ("Xbox", "xbx")
    ];

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsEveryLoaderAsAStatusBeforeTheCircuitConnects()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        foreach (var (name, code) in Loaders)
        {
            StringAssert.Contains(html, $"<span id=\"ldn-{name}-default\" class=\"bit-ldn bit-ldn-ltp bit-ldn-{code}\" role=\"status\" aria-live=\"polite\">");
        }
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#ldn-Ring-default")).ToHaveAttributeAsync("role", "status");
    }

    [TestMethod]
    [DynamicData(nameof(LoaderRows))]
    public async Task EveryLoader_AnimatesInTheBrowser(string name, string code)
    {
        await OpenAsync(Path);

        var root = $"#ldn-{name}-default";
        var names = await Page.Locator(root).EvaluateAsync<string[]>("el => el.getAnimations({ subtree: true }).map(a => a.animationName)");

        Assert.IsTrue(names.Length > 0, $"{name} runs an animation.");
        Assert.IsTrue(names.All(n => n.StartsWith($"bit-ldn-{code}-anm")), $"{name} runs its own keyframes: {string.Join(", ", names)}.");
        Assert.IsTrue(await Page.Locator(root).EvaluateAsync<bool>("el => el.getAnimations({ subtree: true }).every(a => a.playState === 'running')"));

        // The clock of the animation moves on.
        var start = await Page.Locator(root).EvaluateAsync<double>("el => el.getAnimations({ subtree: true })[0].currentTime");
        await Page.WaitForFunctionAsync("([sel, start]) => document.querySelector(sel).getAnimations({ subtree: true })[0].currentTime > start + 100", new object[] { root, start });
    }

    [TestMethod]
    [DynamicData(nameof(LoaderRows))]
    public async Task EveryLoader_IsAPoliteStatus_AnnouncingLoading_WithItsDrawingHidden(string name, string code)
    {
        await OpenAsync(Path);

        var root = Page.Locator($"#ldn-{name}-default");

        await Expect(root).ToHaveClassAsync(HasClass($"bit-ldn-{code}"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-ldn-ltp"));
        await Expect(root).ToHaveAttributeAsync("role", "status");
        await Expect(root).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(root.Locator("> .bit-ldn-ccn")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(root.Locator("> .bit-ldn-ccn")).ToHaveClassAsync(HasClass($"bit-ldn-{code}-ccn"));

        var text = root.Locator("> .bit-ldn-srt");
        await Expect(text).ToHaveTextAsync("Loading");
        var box = await text.BoundingBoxAsync();
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1, "The fallback text is for screen readers only.");
    }

    [TestMethod]
    [DynamicData(nameof(LoaderRows))]
    public async Task EveryLoader_EverySizeAndCustomSize_SizeTheDrawing(string name, string code)
    {
        await OpenAsync(Path);

        var expected = new (string Variant, int Size, string? Class)[]
        {
            ("default", 64, null), ("sm", 40, "bit-ldn-sm"), ("md", 64, "bit-ldn-md"), ("lg", 88, "bit-ldn-lg"),
            ("custom", 100, "bit-ldn-csz"), ("custom-loses", 40, "bit-ldn-sm")
        };

        foreach (var (variant, size, className) in expected)
        {
            var root = Page.Locator($"#ldn-{name}-{variant}");
            if (className is not null) await Expect(root).ToHaveClassAsync(HasClass(className));

            var drawn = await root.Locator("> .bit-ldn-ccn").EvaluateAsync<double[]>("el => [el.offsetWidth, el.offsetHeight]");
            Assert.AreEqual(size, drawn[0], 0.5, $"{name} {variant} width");
            Assert.AreEqual(size, drawn[1], 0.5, $"{name} {variant} height");
        }

        await Expect(Page.Locator($"#ldn-{name}-custom")).ToHaveAttributeAsync("style", new Regex("--bit-ldn-sz:100px"));
        await Expect(Page.Locator($"#ldn-{name}-custom-loses")).Not.ToHaveAttributeAsync("style", new Regex("--bit-ldn-sz"));
    }

    [TestMethod]
    [DynamicData(nameof(LoaderRows))]
    public async Task EveryLoader_ColorAndCustomColor_PaintTheDrawing(string name, string code)
    {
        await OpenAsync(Path);

        var primary = await ResolveAsync("color", "var(--bit-clr-pri)");
        var error = await ResolveAsync("color", "var(--bit-clr-err)");
        var success = await ResolveAsync("color", "var(--bit-clr-suc)");

        Assert.IsTrue(await PaintsAsync($"#ldn-{name}-default", primary), $"{name} draws in the primary color by default.");
        Assert.IsTrue(await PaintsAsync($"#ldn-{name}-error", error), $"{name} draws in the Error role.");
        Assert.IsFalse(await PaintsAsync($"#ldn-{name}-error", primary));
        Assert.IsTrue(await PaintsAsync($"#ldn-{name}-customcolor", "rgb(1, 128, 2)"), $"{name} draws in its CustomColor.");

        // Color wins over CustomColor.
        Assert.IsTrue(await PaintsAsync($"#ldn-{name}-color-wins", success));
        Assert.IsFalse(await PaintsAsync($"#ldn-{name}-color-wins", "rgb(1, 128, 2)"));
    }

    [TestMethod]
    [DynamicData(nameof(LoaderRows))]
    public async Task EveryLoader_ALabel_IsShownInPlaceOfTheHiddenText_WhereItsPlacementSays(string name, string code)
    {
        await OpenAsync(Path);

        var root = Page.Locator($"#ldn-{name}-label");

        await Expect(root.Locator("> .bit-ldn-lbl")).ToHaveTextAsync($"Loading {name}");
        await Expect(root.Locator("> .bit-ldn-srt")).ToHaveCountAsync(0);
        await Expect(root).ToHaveClassAsync(HasClass("bit-ldn-led"));
        await Expect(root).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));

        Assert.AreEqual("right", await SideOfLabelAsync($"#ldn-{name}-label"), $"{name}: End puts the label after the drawing.");
    }

    [TestMethod]
    [DynamicData(nameof(LoaderRows))]
    public async Task EveryLoader_SpeedQuickensIt_AndPausedFreezesIt(string name, string code)
    {
        await OpenAsync(Path);

        var normal = await DurationsAsync($"#ldn-{name}-default");
        var fast = await DurationsAsync($"#ldn-{name}-speed");

        await Expect(Page.Locator($"#ldn-{name}-speed")).ToHaveAttributeAsync("style", new Regex("--bit-ldn-spd:2"));
        CollectionAssert.AreEqual(normal.Select(d => Math.Round(d / 2)).ToArray(), fast.Select(d => Math.Round(d)).ToArray(), $"{name} runs twice as fast.");

        var paused = Page.Locator($"#ldn-{name}-paused");
        await Expect(paused).ToHaveClassAsync(HasClass("bit-ldn-pau"));
        Assert.IsTrue(await paused.EvaluateAsync<bool>("el => { const a = el.getAnimations({ subtree: true }); return a.length > 0 && a.every(x => x.playState === 'paused'); }"),
                      $"{name} is frozen on its frame.");
    }

    [TestMethod]
    [DataRow("DualRing")]
    [DataRow("Ring")]
    [DataRow("Ripple")]
    [DataRow("Spinner")]
    [DataRow("Xbox")]
    public async Task Thickness_SetsTheStrokeOfTheLoadersDrawnWithALine(string name)
    {
        await OpenAsync(Path);

        var root = Page.Locator($"#ldn-{name}-thick");
        await Expect(root).ToHaveAttributeAsync("style", new Regex("--bit-ldn-stroke:10px"));
        Assert.AreEqual("10px", await root.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-ldn-thickness').trim()"));

        // A stroke of the drawing - a border or a bar - is 10px wide.
        Assert.IsTrue(await root.EvaluateAsync<bool>(
            @"el => [...el.querySelectorAll('.bit-ldn-ccn, .bit-ldn-ccn *')].some(e => [null, '::before', '::after'].some(p => {
                const s = getComputedStyle(e, p);
                return s.borderTopWidth === '10px' || s.borderLeftWidth === '10px' || s.width === '10px';
            }))"), $"{name} draws its stroke 10px wide.");
    }

    [TestMethod]
    public async Task ReducedMotion_SlowsEveryLoader_AndForceAnimationRestoresItsPace()
    {
        await OpenAsync(Path);

        var normal = new Dictionary<string, double[]>();
        foreach (var (name, _) in Loaders) normal[name] = await DurationsAsync($"#ldn-{name}-default");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        foreach (var (name, _) in Loaders)
        {
            var slow = normal[name].Select(d => Math.Round(d * 3)).ToArray();
            await Page.WaitForFunctionAsync("sel => document.querySelector(sel).getAnimations({ subtree: true }).length > 0", $"#ldn-{name}-default");
            CollectionAssert.AreEqual(slow, (await DurationsAsync($"#ldn-{name}-default")).Select(d => Math.Round(d)).ToArray(), $"{name} is three times slower.");

            await Expect(Page.Locator($"#ldn-{name}-force")).ToHaveClassAsync(HasClass("bit-fam"));
            CollectionAssert.AreEqual(normal[name].Select(d => Math.Round(d)).ToArray(), (await DurationsAsync($"#ldn-{name}-force")).Select(d => Math.Round(d)).ToArray(), $"{name} keeps its pace with ForceAnimation.");
        }
    }

    [TestMethod]
    [DataRow("default", "bit-ldn-ltp", "above")]
    [DataRow("Top", "bit-ldn-ltp", "above")]
    [DataRow("Bottom", "bit-ldn-lbm", "below")]
    [DataRow("Start", "bit-ldn-lst", "left")]
    [DataRow("End", "bit-ldn-led", "right")]
    [DataRow("other", "bit-ldn-ltp", "above")]
    public async Task LabelPlacement_EveryValue_PutsTheLabelOnThatSide(string placement, string className, string side)
    {
        await OpenAsync(Path);

        var root = Page.Locator($"#ldn-place-{placement}");
        await Expect(root).ToHaveClassAsync(HasClass(className));

        Assert.AreEqual(side, await SideOfLabelAsync($"#ldn-place-{placement}"));
    }

    [TestMethod]
    public async Task LabelPlacement_StartAndEnd_FollowTheDirection_AndATemplateReplacesTheLabel()
    {
        await OpenAsync(Path);

        Assert.AreEqual("right", await SideOfLabelAsync("#ldn-rtl-Start"));
        Assert.AreEqual("left", await SideOfLabelAsync("#ldn-rtl-End"));
        Assert.AreEqual("right", await SideOfLabelAsync("#ldn-rtl-inherited"));
        await Expect(Page.Locator("#ldn-rtl-Start")).ToHaveAttributeAsync("dir", "rtl");

        await Expect(Page.Locator("#ldn-template > #label-template")).ToHaveTextAsync("Custom label");
        await Expect(Page.Locator("#ldn-template .bit-ldn-srt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ldn-template .bit-ldn-lbl")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Delay_HoldsTheContentBack_WhileTheLiveRegionIsAlreadyThere()
    {
        await OpenAsync(Path);

        var watch = Stopwatch.StartNew();
        await Page.Locator("#btn-show-delayed").ClickAsync();

        var root = Page.Locator("#ldn-delay");
        await Expect(root).ToHaveAttributeAsync("role", "status");
        await Expect(root).ToHaveAttributeAsync("aria-live", "polite");
        Assert.AreEqual(0, await root.EvaluateAsync<int>("el => el.children.length"), "Nothing inside yet.");

        await Expect(root.Locator(".bit-ldn-lbl")).ToHaveTextAsync("Delayed");
        Assert.IsTrue(watch.ElapsedMilliseconds >= 1500, $"Shown after {watch.ElapsedMilliseconds}ms of its 2000.");
        await Expect(root.Locator(".bit-ldn-ccn")).ToHaveCountAsync(1);

        // A delay set back to zero shows the loader at once.
        await Expect(Page.Locator("#ldn-delay-long .bit-ldn-lbl")).ToHaveCountAsync(0);
        await Page.Locator("#btn-cancel-delay").ClickAsync();
        await Expect(Page.Locator("#ldn-delay-long .bit-ldn-lbl")).ToHaveTextAsync("Long delay");
    }

    [TestMethod]
    public async Task Inline_SitsOnTheLineAtTheSizeOfItsText_WithTheLabelAfterIt()
    {
        await OpenAsync(Path);

        var inline = Page.Locator("#inline-paragraph > #ldn-inline");
        await Expect(inline).ToHaveClassAsync(HasClass("bit-ldn-inl"));
        await Expect(inline).ToHaveClassAsync(HasClass("bit-ldn-em"));
        await Expect(inline).ToHaveCSSAsync("display", "inline-flex");
        Assert.AreEqual(24, await inline.Locator(".bit-ldn-ccn").EvaluateAsync<double>("el => el.offsetWidth"), 0.5, "1em of the paragraph.");

        var labelled = Page.Locator("#ldn-inline-label");
        await Expect(labelled).ToHaveClassAsync(HasClass("bit-ldn-led"));
        await Expect(labelled.Locator(".bit-ldn-lbl")).ToHaveCSSAsync("font-size", "24px");
        Assert.AreEqual("right", await SideOfLabelAsync("#ldn-inline-label"));

        // currentColor takes the color of the text around it.
        var buttonColor = await ComputedStyleAsync("#inline-button", "color");
        Assert.IsTrue(await PaintsAsync("#ldn-inline-button", buttonColor));
        Assert.AreEqual(20, await Page.Locator("#ldn-inline-button .bit-ldn-ccn").EvaluateAsync<double>("el => el.offsetWidth"), 0.5);
    }

    [TestMethod]
    public async Task Accessibility_RoleAriaLiveAndAriaLabel_DecideWhatIsAnnounced()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ldn-a11y-aria-label > .bit-ldn-srt")).ToHaveTextAsync("Fetching your orders");

        var none = Page.Locator("#ldn-a11y-none");
        await Expect(none).ToHaveAttributeAsync("role", "none");
        await Expect(none).Not.ToHaveAttributeAsync("aria-live", new Regex(".*"));
        await Expect(none.Locator(".bit-ldn-srt")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#ldn-a11y-assertive")).ToHaveAttributeAsync("aria-live", "assertive");

        var progressbar = Page.Locator("#ldn-a11y-progressbar");
        await Expect(progressbar).ToHaveAttributeAsync("role", "progressbar");
        await Expect(progressbar).ToHaveAttributeAsync("aria-label", "Exporting");
        await Expect(progressbar).Not.ToHaveAttributeAsync("aria-live", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Progressbar, new() { Name = "Exporting" })).ToHaveCountAsync(1);

        await Expect(Page.Locator("#ldn-a11y-progressbar-unnamed")).ToHaveAttributeAsync("aria-label", "Loading");
        await Expect(Page.Locator("#ldn-a11y-progressbar-unnamed .bit-ldn-srt")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#ldn-a11y-label-aria")).ToHaveAttributeAsync("aria-label", "Spoken instead");
        await Expect(Page.Locator("#ldn-a11y-label-aria .bit-ldn-lbl")).ToHaveTextAsync("Visible");

        // Attributes passed through are honoured over the defaults.
        var splat = Page.Locator("#ldn-a11y-splat");
        await Expect(splat).ToHaveAttributeAsync("role", "alert");
        await Expect(splat).ToHaveAttributeAsync("aria-live", "off");
        await Expect(splat).ToHaveAttributeAsync("aria-label", "Splatted name");
        await Expect(splat).ToHaveAttributeAsync("dir", "rtl");
        await Expect(splat.Locator(".bit-ldn-srt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_ToEveryKindOfLoader_AndALoadersOwnValueWins()
    {
        await OpenAsync(Path);

        var warning = await ResolveAsync("color", "var(--bit-clr-wrn)");

        foreach (var id in new[] { "#ldn-params-ring", "#ldn-params-dual", "#ldn-params-spinner" })
        {
            Assert.AreEqual("3px", await Page.Locator(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-ldn-thickness').trim()"));
            await Expect(Page.Locator(id)).ToHaveAttributeAsync("style", new Regex("--bit-ldn-spd:1.5"));
            Assert.IsTrue(await PaintsAsync(id, warning), $"{id} takes the cascaded color.");
        }

        CollectionAssert.AreEqual(new[] { 800.0 }, (await DurationsAsync("#ldn-params-ring")).Select(d => Math.Round(d)).Distinct().ToArray());
        await Expect(Page.Locator("#ldn-params-ring")).ToHaveClassAsync(HasClass("bit-ldn-led"));
        await Expect(Page.Locator("#ldn-params-spinner")).ToHaveClassAsync(HasClass("bit-ldn-lbm"));
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachEveryPart()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ldn-style")).ToHaveCSSAsync("padding-top", "16px");
        await Expect(Page.Locator("#ldn-style")).ToHaveCSSAsync("border-top-color", "rgb(128, 128, 128)");
        await Expect(Page.Locator("#ldn-style")).ToHaveClassAsync(HasClass("e2e-loading-class"));

        await Expect(Page.Locator("#ldn-styles")).ToHaveCSSAsync("padding-top", "8px");
        await Expect(Page.Locator("#ldn-styles")).ToHaveClassAsync(HasClass("e2e-root"));
        await Expect(Page.Locator("#ldn-styles > .bit-ldn-ccn")).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#ldn-styles > .bit-ldn-ccn")).ToHaveClassAsync(HasClass("e2e-container"));
        await Expect(Page.Locator("#ldn-styles .bit-ldn-chl")).ToHaveCountAsync(9);
        await Expect(Page.Locator("#ldn-styles .bit-ldn-chl").First).ToHaveCSSAsync("outline-color", "rgb(2, 3, 4)");
        await Expect(Page.Locator("#ldn-styles .bit-ldn-chl").First).ToHaveClassAsync(HasClass("e2e-child"));
        await Expect(Page.Locator("#ldn-styles > .bit-ldn-lbl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(Page.Locator("#ldn-styles > .bit-ldn-lbl")).ToHaveClassAsync(HasClass("e2e-label"));
        await Expect(Page.Locator("#ldn-styles-srt > .bit-ldn-srt")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Page.Locator("#ldn-styles-srt > .bit-ldn-srt")).ToHaveClassAsync(HasClass("e2e-srt"));
    }

    [TestMethod]
    public async Task VisibilityAndDisabled_HideOrDimAndFreezeTheLoader()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ldn-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#ldn-collapsed")).ToHaveCSSAsync("display", "none");

        var disabled = Page.Locator("#ldn-disabled");
        await Expect(disabled).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(disabled).ToHaveCSSAsync("opacity", await ResolveAsync("opacity", "var(--bit-opa-dis)"));
        Assert.IsTrue(await disabled.EvaluateAsync<bool>("el => el.getAnimations({ subtree: true }).every(a => a.playState === 'paused')"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleTheLoaders_AndTheParametersWin()
    {
        await OpenAsync(Path);

        var css = Page.Locator("#ldn-css");
        Assert.IsTrue(await PaintsAsync("#ldn-css", "rgb(10, 20, 30)"), "--bit-Loading-color");
        Assert.IsTrue(await PaintsAsync("#ldn-css", "rgb(40, 50, 60)"), "--bit-Loading-track-color");
        Assert.AreEqual(50, await css.Locator(".bit-ldn-ccn").EvaluateAsync<double>("el => el.offsetWidth"), 0.5);
        Assert.AreEqual("5px", await css.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-ldn-thickness').trim()"));
        CollectionAssert.AreEqual(new[] { 2400.0 }, (await DurationsAsync("#ldn-css")).Select(d => Math.Round(d)).Distinct().ToArray(), "--bit-Loading-speed 0.5 halves the pace.");
        await Expect(css).ToHaveCSSAsync("row-gap", "13px");
        await Expect(css.Locator(".bit-ldn-lbl")).ToHaveCSSAsync("color", "rgb(70, 80, 90)");
        await Expect(css.Locator(".bit-ldn-lbl")).ToHaveCSSAsync("font-size", "17px");
        await Expect(css.Locator(".bit-ldn-lbl")).ToHaveCSSAsync("font-weight", "700");

        var parameters = Page.Locator("#ldn-css-params");
        Assert.IsTrue(await PaintsAsync("#ldn-css-params", await ResolveAsync("color", "var(--bit-clr-err)")));
        Assert.IsFalse(await PaintsAsync("#ldn-css-params", "rgb(10, 20, 30)"));
        Assert.AreEqual(40, await parameters.Locator(".bit-ldn-ccn").EvaluateAsync<double>("el => el.offsetWidth"), 0.5);
        Assert.AreEqual("2px", await parameters.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-ldn-thickness').trim()"));
        CollectionAssert.AreEqual(new[] { 600.0 }, (await DurationsAsync("#ldn-css-params")).Select(d => Math.Round(d)).Distinct().ToArray());
        await Expect(parameters.Locator(".bit-ldn-lbl")).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-xs)"));

        Assert.AreEqual(30, await Page.Locator("#ldn-css-custom .bit-ldn-ccn").EvaluateAsync<double>("el => el.offsetWidth"), 0.5);
        Assert.IsTrue(await PaintsAsync("#ldn-css-custom", "rgb(1, 128, 2)"));
    }

    [TestMethod]
    public async Task Runtime_ChangingTheParameters_ReRendersTheLoader()
    {
        await OpenAsync(Path);

        var root = Page.Locator("#ldn-runtime");

        await Page.Locator("#btn-set-size").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-ldn-lg"));
        Assert.AreEqual(88, await root.Locator(".bit-ldn-ccn").EvaluateAsync<double>("el => el.offsetWidth"), 0.5);

        await Page.Locator("#btn-set-color").ClickAsync();
        await Expect(root).ToHaveAttributeAsync("style", new Regex("--bit-ldn-clr:var\\(--bit-clr-suc\\)"));
        Assert.IsTrue(await PaintsAsync("#ldn-runtime", await ResolveAsync("color", "var(--bit-clr-suc)")));

        await Page.Locator("#btn-toggle-paused").ClickAsync();
        await Expect(root).ToHaveClassAsync(HasClass("bit-ldn-pau"));
        await Page.Locator("#btn-toggle-paused").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-ldn-pau"));
        Assert.IsTrue(await root.EvaluateAsync<bool>("el => el.getAnimations({ subtree: true }).every(a => a.playState === 'running')"));

        await Page.Locator("#btn-set-label").ClickAsync();
        await Expect(root.Locator(".bit-ldn-lbl")).ToHaveTextAsync("Almost there");
    }

    [TestMethod]
    public async Task Unmount_WhileADelayIsPending_LeavesNoError()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ldn-mounted")).ToHaveAttributeAsync("role", "status");

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#ldn-mounted")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    public static IEnumerable<object[]> LoaderRows => Loaders.Select(l => new object[] { l.Name, l.Code });

    // Whether any part of a loader's drawing - an element or a pseudo element of it - is painted in the color.
    private Task<bool> PaintsAsync(string root, string color)
    {
        return Page.Locator(root).EvaluateAsync<bool>(
            @"(el, color) => [el, ...el.querySelectorAll('*')].some(e => [null, '::before', '::after'].some(p => {
                const s = getComputedStyle(e, p);
                return ['background-color', 'border-top-color', 'border-right-color', 'border-bottom-color', 'border-left-color', 'box-shadow']
                    .some(prop => s.getPropertyValue(prop).includes(color));
            }))", color);
    }

    // The durations of every animation a loader runs, in milliseconds.
    private Task<double[]> DurationsAsync(string root)
    {
        return Page.Locator(root).EvaluateAsync<double[]>("el => el.getAnimations({ subtree: true }).map(a => a.effect.getComputedTiming().duration)");
    }

    // Which side of the drawing the label of a loader is on, read off their layout boxes: a drawing turned by a
    // transform (the Heart, the OrbitingDots) has a painted box larger than the room it takes.
    private Task<string> SideOfLabelAsync(string root)
    {
        return Page.Locator(root).EvaluateAsync<string>(
            @"el => {
                const box = e => ({ left: e.offsetLeft, top: e.offsetTop, right: e.offsetLeft + e.offsetWidth, bottom: e.offsetTop + e.offsetHeight });
                const label = box(el.querySelector('.bit-ldn-lbl'));
                const drawing = box(el.querySelector('.bit-ldn-ccn'));
                if (label.bottom <= drawing.top + 0.5) return 'above';
                if (label.top >= drawing.bottom - 0.5) return 'below';
                if (label.right <= drawing.left + 0.5) return 'left';
                if (label.left >= drawing.right - 0.5) return 'right';
                return 'overlapping';
            }");
    }

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
