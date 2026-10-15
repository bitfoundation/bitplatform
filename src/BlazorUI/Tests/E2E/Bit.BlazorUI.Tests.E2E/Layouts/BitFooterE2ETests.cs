namespace Bit.BlazorUI.Tests.E2E.Layouts;

/// <summary>
/// BitFooter in a real browser: the contentinfo landmark, Height (and its runtime change), Disabled (colors, and the
/// content and the page's handlers staying live in a layout host), every Variant and every Color with every variant
/// (read against the theme tokens), Bordered / Elevated / Translucent, every Alignment and VerticalAlign value off the
/// real layout, Gap / Wrap / NoGutter / MaxWidth, Sticky / Fixed / Absolute and which one wins, the scroll driven
/// behaviours scrolled with the real mouse wheel - Reveal (shown at both ends) with RevealOffset, OnRevealChanged and
/// IsRevealed and the focus bringing it back, ElevateOnScroll with OnOverlapChanged and IsOverlapping, ScrollTarget (and
/// a selector matching nothing), ScrollPadding, on a scrolling box and on the window - Hidden and inert, the BitParams
/// cascade, Size, the --bit-Footer-* variables and their variant tiers, Style / Class / Styles / Classes / Visibility,
/// RTL, prerendering, a runtime Reveal toggle and unmounting.
/// </summary>
[TestClass]
public class BitFooterE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    // Every BitColor, in the enum's order, with the class suffix of BitCssClasses.Color and the theme tokens its role reads.
    private static readonly (string Name, string Suffix, string Main, string On)[] Colors =
    [
        ("Primary", "pri", "--bit-clr-pri", "--bit-clr-pri-text"),
        ("Secondary", "sec", "--bit-clr-sec", "--bit-clr-sec-text"),
        ("Tertiary", "ter", "--bit-clr-ter", "--bit-clr-ter-text"),
        ("Info", "inf", "--bit-clr-inf", "--bit-clr-inf-text"),
        ("Success", "suc", "--bit-clr-suc", "--bit-clr-suc-text"),
        ("Warning", "wrn", "--bit-clr-wrn", "--bit-clr-wrn-text"),
        ("SevereWarning", "swr", "--bit-clr-swr", "--bit-clr-swr-text"),
        ("Error", "err", "--bit-clr-err", "--bit-clr-err-text"),
        ("PrimaryBackground", "pbg", "--bit-clr-bg-pri", "--bit-clr-fg-pri"),
        ("SecondaryBackground", "sbg", "--bit-clr-bg-sec", "--bit-clr-fg-pri"),
        ("TertiaryBackground", "tbg", "--bit-clr-bg-ter", "--bit-clr-fg-pri"),
        ("PrimaryForeground", "pfg", "--bit-clr-fg-pri", "--bit-clr-bg-pri"),
        ("SecondaryForeground", "sfg", "--bit-clr-fg-sec", "--bit-clr-bg-pri"),
        ("TertiaryForeground", "tfg", "--bit-clr-fg-ter", "--bit-clr-bg-pri"),
        ("PrimaryBorder", "pbr", "--bit-clr-brd-pri", "--bit-clr-fg-pri"),
        ("SecondaryBorder", "sbr", "--bit-clr-brd-sec", "--bit-clr-fg-pri"),
        ("TertiaryBorder", "tbr", "--bit-clr-brd-ter", "--bit-clr-fg-pri"),
    ];

    [TestMethod]
    public async Task Prerender_TheFooterIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/footer");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<footer");
        StringAssert.Contains(html, "id=\"basic-footer\"");
        StringAssert.Contains(html, "bit-ftr-gut");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_AFooterElement_SizedByItsContentAndPaddings()
    {
        await OpenAsync("/e2e/footer");

        var footer = Page.Locator("#basic-footer");
        Assert.AreEqual("FOOTER", await footer.EvaluateAsync<string>("el => el.tagName"));
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr"));
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-fil"));
        await Expect(footer.Locator("> .bit-ftr-gut")).ToHaveCSSAsync("padding-top", "8px");
        await Expect(footer.Locator("> .bit-ftr-gut")).ToHaveCSSAsync("padding-left", "24px");
        AssertClose(20 + 16, (await BoxAsync("#basic-footer")).H, "the content plus the default paddings");
        AssertClose(24, (await BoxAsync("#basic-child")).X - (await BoxAsync("#basic-footer")).X, "inset by the gutter");

        // The one footer outside every section is the page's contentinfo landmark.
        await Expect(Page.GetByRole(AriaRole.Contentinfo)).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Contentinfo)).ToHaveAttributeAsync("id", "contentinfo");
    }

    [TestMethod]
    public async Task Height_FixesTheFooterInPixels_AndReRendersAtRuntime()
    {
        await OpenAsync("/e2e/footer");

        AssertClose(80, (await BoxAsync("#height-footer")).H, "Height 80");

        await Page.ClickAsync("#btn-height");

        await Expect(Page.Locator("#height-footer")).ToHaveCSSAsync("height", "100px");
    }

    [TestMethod]
    public async Task Disabled_TheFooterDims_AndItsContentStaysLive()
    {
        await OpenAsync("/e2e/footer");

        var footer = Page.Locator("#disabled-footer");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(footer).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));
        await Expect(footer).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-pri)"));

        // A footer is a layout host: what bubbles up to it is the enabled content's own event, so it keeps the page's
        // handlers and its controls keep working.
        await Page.ClickAsync("#disabled-inner");
        await Expect(Page.Locator("#inner-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(footer).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(footer).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-pri)"));
    }

    [TestMethod]
    public async Task Variant_FillOutlineAndText_PaintTheSurfaceDifferently()
    {
        await OpenAsync("/e2e/footer");

        var fill = Page.Locator("#variant-fill");
        await Expect(fill).ToHaveClassAsync(HasClass("bit-ftr-fil"));
        await Expect(fill).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-pri)"));
        await Expect(fill).ToHaveCSSAsync("border-top-style", "none");

        var outline = Page.Locator("#variant-outline");
        await Expect(outline).ToHaveClassAsync(HasClass("bit-ftr-otl"));
        await Expect(outline).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(outline).ToHaveCSSAsync("border-bottom-style", "solid");
        await Expect(outline).ToHaveCSSAsync("border-bottom-color", await ResolveAsync("color", "var(--bit-clr-brd-pri)"));

        var text = Page.Locator("#variant-text");
        await Expect(text).ToHaveClassAsync(HasClass("bit-ftr-txt"));
        await Expect(text).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(text).ToHaveCSSAsync("border-top-style", "none");
        await Expect(text).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-pri)"));
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsEveryVariantWithItsRole()
    {
        await OpenAsync("/e2e/footer");

        var transparent = "rgba(0, 0, 0, 0)";
        foreach (var (name, suffix, main, on) in Colors)
        {
            var mainColor = await ResolveAsync("color", $"var({main})");
            Assert.AreNotEqual(transparent, mainColor, $"{main} resolves to a color");

            var fill = Page.Locator($"#color-fill-{name}");
            await Expect(fill).ToHaveClassAsync(HasClass($"bit-ftr-{suffix}"));
            await Expect(fill).ToHaveCSSAsync("background-color", mainColor);
            await Expect(fill).ToHaveCSSAsync("color", await ResolveAsync("color", $"var({on})"));

            var outline = Page.Locator($"#color-outline-{name}");
            await Expect(outline).ToHaveCSSAsync("border-top-color", mainColor);
            await Expect(outline).ToHaveCSSAsync("color", mainColor);

            var text = Page.Locator($"#color-text-{name}");
            await Expect(text).ToHaveCSSAsync("color", mainColor);
            await Expect(text).ToHaveCSSAsync("background-color", transparent);

            var disabled = Page.Locator($"#color-dis-{name}");
            await Expect(disabled).ToHaveCSSAsync("background-color", await ResolveAsync("color", $"var({main}-dis)"));
            await Expect(disabled).ToHaveCSSAsync("color", await ResolveAsync("color", $"var({main}-dis-text)"));
        }
    }

    [TestMethod]
    public async Task BorderedAndElevated_DrawADividerAboveAndAShadow()
    {
        await OpenAsync("/e2e/footer");

        var bordered = Page.Locator("#bordered");
        await Expect(bordered).ToHaveClassAsync(HasClass("bit-ftr-brd"));
        await Expect(bordered).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(bordered).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(bordered).ToHaveCSSAsync("border-top-color", await ResolveAsync("color", "var(--bit-clr-brd-pri)"));
        await Expect(bordered).ToHaveCSSAsync("border-bottom-style", "none");
        await Expect(Page.Locator("#bordered-text")).ToHaveCSSAsync("border-top-style", "solid");

        await Expect(Page.Locator("#elevated")).ToHaveClassAsync(HasClass("bit-ftr-elv"));
        Assert.AreNotEqual("none", await ComputedStyleAsync("#elevated", "box-shadow"));
        Assert.AreEqual("none", await ComputedStyleAsync("#plain", "box-shadow"));
        await Expect(Page.Locator("#plain")).ToHaveCSSAsync("border-top-style", "none");
    }

    [TestMethod]
    [DataRow("", "", "normal", 0)]
    [DataRow("Start", "bit-ftr-srt", "flex-start", 0)]
    [DataRow("End", "bit-ftr-end", "flex-end", 420)]
    [DataRow("Center", "bit-ftr-cnt", "center", 210)]
    [DataRow("SpaceBetween", "bit-ftr-sbt", "space-between", 0)]
    [DataRow("SpaceAround", "bit-ftr-sar", "space-around", 70)]
    [DataRow("SpaceEvenly", "bit-ftr-sev", "space-evenly", 105)]
    [DataRow("Baseline", "bit-ftr-bsl", "normal", 0)]
    [DataRow("Stretch", "bit-ftr-str", "normal", 0)]
    public async Task Alignment_EveryValue_DistributesTheContentAlongTheLine(string value, string cls, string justify, double firstX)
    {
        await OpenAsync("/e2e/footer");

        await Page.SelectOptionAsync("#sel-al", value);
        await Expect(Page.Locator("#al-state")).ToContainTextAsync($"al={value};");

        if (cls.Length > 0) await Expect(Page.Locator("#al-footer")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator("#al-footer > .bit-ftr-gut")).ToHaveCSSAsync("justify-content", justify);
        var footer = await BoxAsync("#al-footer");
        AssertClose(firstX, (await BoxAsync("#al-1")).X - footer.X, $"{value}: first child x");
        if (value == "SpaceBetween") AssertClose(footer.Right, (await BoxAsync("#al-3")).Right, "the last at the end");

        if (value is "Baseline" or "Stretch")
        {
            await Expect(Page.Locator("#al-footer > .bit-ftr-gut")).ToHaveCSSAsync("align-items", value.ToLowerInvariant());
        }
    }

    [TestMethod]
    [DataRow("", "", "center", 30)]
    [DataRow("Start", "bit-ftr-vst", "flex-start", 0)]
    [DataRow("End", "bit-ftr-ved", "flex-end", 60)]
    [DataRow("Center", "bit-ftr-vcn", "center", 30)]
    [DataRow("Baseline", "bit-ftr-bsl", "baseline", -1)]
    [DataRow("Stretch", "bit-ftr-str", "stretch", 0)]
    public async Task VerticalAlign_EveryValue_PlacesTheContentInTheHeight(string value, string cls, string alignItems, double y)
    {
        await OpenAsync("/e2e/footer");

        await Page.SelectOptionAsync("#sel-va", value);
        await Expect(Page.Locator("#al-state")).ToContainTextAsync($"va={value};");

        if (cls.Length > 0) await Expect(Page.Locator("#al-footer")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator("#al-footer > .bit-ftr-gut")).ToHaveCSSAsync("align-items", alignItems);
        if (y >= 0) AssertClose(y, (await BoxAsync("#al-1")).Y - (await BoxAsync("#al-footer")).Y, $"{value}: y");
    }

    [TestMethod]
    public async Task Layout_GapWrapNoGutterAndMaxWidth()
    {
        await OpenAsync("/e2e/footer");

        AssertClose(16, (await BoxAsync("#gap-2")).X - (await BoxAsync("#gap-1")).Right, "Gap 1rem");

        await Expect(Page.Locator("#wrap-footer")).ToHaveClassAsync(HasClass("bit-ftr-wrp"));
        await Expect(Page.Locator("#wrap-footer > .bit-ftr-gut")).ToHaveCSSAsync("flex-wrap", "wrap");
        AssertClose(10, (await BoxAsync("#wrap-2")).X - (await BoxAsync("#wrap-1")).Right, "the column gap");
        AssertClose((await BoxAsync("#wrap-1")).X, (await BoxAsync("#wrap-4")).X, "three per line, the fourth wraps");
        AssertClose(4, (await BoxAsync("#wrap-4")).Y - (await BoxAsync("#wrap-1")).Bottom, "the row gap");

        await Expect(Page.Locator("#nowrap-footer > .bit-ftr-gut")).ToHaveCSSAsync("flex-wrap", "nowrap");
        AssertClose((await BoxAsync("#nowrap-1")).Y, (await BoxAsync("#nowrap-6")).Y, "one line without Wrap");

        await Expect(Page.Locator("#nogutter-footer")).ToHaveClassAsync(HasClass("bit-ftr-ngt"));
        await Expect(Page.Locator("#nogutter-footer > .bit-ftr-gut")).ToHaveCSSAsync("padding-top", "0px");
        AssertClose((await BoxAsync("#nogutter-footer")).X, (await BoxAsync("#nogutter-child")).X, "no inline padding");
        AssertClose(20, (await BoxAsync("#nogutter-footer")).H, "no block padding");

        var footer = await BoxAsync("#maxwidth-footer");
        AssertClose(600, footer.W, "the bar is full width");
        var gutter = await BoxAsync("#maxwidth-footer > .bit-ftr-gut");
        AssertClose(300, gutter.W, "the content is capped");
        AssertClose(footer.X + 150, gutter.X, "and centered");
        AssertClose(footer.X + 450, (await BoxAsync("#mw-2")).Right, "SpaceBetween within the cap");
    }

    [TestMethod]
    public async Task Position_StickyFixedAbsolute_AndTheWidestReachWins()
    {
        await OpenAsync("/e2e/footer");

        var sticky = Page.Locator("#sticky-footer");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-ftr-stk"));
        await Expect(sticky).ToHaveCSSAsync("position", "sticky");
        await Page.Locator("#sticky-box").ScrollIntoViewIfNeededAsync();
        AssertClose((await BoxAsync("#sticky-box")).Bottom - 1, (await BoxAsync("#sticky-footer")).Bottom, "stuck to the bottom of the box");

        await Expect(Page.Locator("#absolute-footer")).ToHaveCSSAsync("position", "absolute");
        var host = await BoxAsync("#abs-host");
        var abs = await BoxAsync("#absolute-footer");
        AssertClose(host.Bottom, abs.Bottom, "absolute at the bottom of its container");
        AssertClose(host.W, abs.W, "and as wide");

        await Expect(Page.Locator("#fixed-footer")).ToHaveCSSAsync("position", "fixed");
        AssertClose((await BoxAsync("#fixed-host")).Bottom, (await BoxAsync("#fixed-footer")).Bottom, "fixed to the bottom of its containing block");

        await Expect(Page.Locator("#all-positions")).ToHaveClassAsync(HasClass("bit-ftr-fix"));
        await Expect(Page.Locator("#all-positions")).Not.ToHaveClassAsync(HasClass("bit-ftr-abs"));
        await Expect(Page.Locator("#all-positions")).Not.ToHaveClassAsync(HasClass("bit-ftr-stk"));
        await Expect(Page.Locator("#abs-sticky")).ToHaveClassAsync(HasClass("bit-ftr-abs"));
        await Expect(Page.Locator("#abs-sticky")).Not.ToHaveClassAsync(HasClass("bit-ftr-stk"));

        await Expect(Page.Locator("#sticky-height")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("env\\(safe-area-inset-bottom"));
        AssertClose(50, (await BoxAsync("#sticky-height")).H, "Height 50 plus no inset");
    }

    [TestMethod]
    public async Task Reveal_ScrollingDownHidesTheFooter_ScrollingUpOrReachingTheEndBringsItBack()
    {
        await OpenAsync("/e2e/footer");

        var footer = Page.Locator("#reveal-footer");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-anm"));

        await ScrollBoxAsync("#reveal-box", 300);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("False");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-hdn"));
        var height = await footer.EvaluateAsync<double>("el => el.offsetHeight");
        await Expect(footer).ToHaveCSSAsync("transform", $"matrix(1, 0, 0, 1, 0, {height})");
        await Expect(footer).ToHaveCSSAsync("box-shadow", "none");

        await Page.ClickAsync("#btn-read-revealed");
        await Expect(Page.Locator("#is-revealed")).ToHaveTextAsync("False");

        await ScrollBoxAsync("#reveal-box", -100);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("True");
        await Expect(footer).ToHaveCSSAsync("transform", "none");
        await Page.ClickAsync("#btn-read-revealed");
        await Expect(Page.Locator("#is-revealed")).ToHaveTextAsync("True");

        // Hidden again on the way down, and shown at the end of the scroll however it got there.
        await ScrollBoxAsync("#reveal-box", 200);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("False");
        await Expect(footer).ToHaveCSSAsync("transform", $"matrix(1, 0, 0, 1, 0, {height})");
        await ScrollBoxAsync("#reveal-box", 5000);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#reveal-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Reveal_ReachingTheEndWhileTheFooterIsStillSlidingAway_ShowsItAtTheEnd()
    {
        await OpenAsync("/e2e/footer");

        // Scrolled down, and then straight on to the end in the middle of the slide that follows: the footer is part
        // way out, so its translation is already lengthening the box, and goes on doing so after the end was reached
        // with no scroll event to say so.
        await Page.EvaluateAsync(@"() => new Promise(resolve => {
            const box = document.getElementById('reveal-box'), footer = document.getElementById('reveal-footer');
            box.scrollTop = 300;
            const tick = () => {
                const shift = new DOMMatrixReadOnly(getComputedStyle(footer).transform).m42;
                // A quarter of the way out, where the slide is fast enough to lengthen the box by more than the
                // pixel of slack the end is read with between two frames.
                if (shift >= footer.offsetHeight / 4) {
                    box.scrollTop = box.scrollHeight;
                    resolve();
                } else {
                    requestAnimationFrame(tick);
                }
            };
            tick();
        })");

        // Hidden once, and shown again at the end.
        await Expect(Page.Locator("#reveal-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#reveal-footer")).Not.ToHaveClassAsync(HasClass("bit-ftr-hdn"));
    }

    [TestMethod]
    public async Task Reveal_AFocusInsideTheHiddenFooter_BringsItBack()
    {
        await OpenAsync("/e2e/footer");

        await ScrollBoxAsync("#reveal-box", 300);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("False");

        await Page.Locator("#reveal-inner").FocusAsync();

        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#reveal-footer")).Not.ToHaveClassAsync(HasClass("bit-ftr-hdn"));
    }

    [TestMethod]
    public async Task RevealOffset_KeepsTheFooterUntilTheScrollHasPassedIt()
    {
        await OpenAsync("/e2e/footer");

        await ScrollBoxAsync("#reveal-offset-box", 100);
        await Expect(Page.Locator("#reveal-offset-box")).ToHaveJSPropertyAsync("scrollTop", 100);
        await SyncAsync();
        await Expect(Page.Locator("#reveal-offset-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#reveal-offset-footer")).Not.ToHaveClassAsync(HasClass("bit-ftr-hdn"));

        await ScrollBoxAsync("#reveal-offset-box", 300);
        await Expect(Page.Locator("#reveal-offset-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#reveal-offset-footer")).ToHaveClassAsync(HasClass("bit-ftr-hdn"));
    }

    [TestMethod]
    public async Task Reveal_AFooterThatIsNotPinned_NeverHides()
    {
        await OpenAsync("/e2e/footer");

        await ScrollBoxAsync("#reveal-unpinned-box", 300);
        await Expect(Page.Locator("#reveal-unpinned-box")).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        await SyncAsync();

        await Expect(Page.Locator("#reveal-unpinned-footer")).Not.ToHaveClassAsync(HasClass("bit-ftr-hdn"));
        await Expect(Page.Locator("#reveal-unpinned-footer")).ToHaveClassAsync(HasClass("bit-ftr-anm"));
    }

    [TestMethod]
    public async Task ElevateOnScroll_TheShadowStaysWhileContentIsLeftUnderneath()
    {
        await OpenAsync("/e2e/footer");

        // With content below it from the start, the footer is lifted right away.
        var footer = Page.Locator("#elevate-footer");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-esc"));
        await Expect(Page.Locator("#overlap-state")).ToHaveTextAsync("True");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-elv"));
        await Expect(footer).Not.ToHaveCSSAsync("box-shadow", "none");
        await Page.ClickAsync("#btn-read-overlapping");
        await Expect(Page.Locator("#is-overlapping")).ToHaveTextAsync("True");

        await ScrollBoxAsync("#elevate-box", 5000);
        await Expect(Page.Locator("#overlap-state")).ToHaveTextAsync("False");
        await Expect(footer).Not.ToHaveClassAsync(HasClass("bit-ftr-elv"));
        await Expect(footer).ToHaveCSSAsync("box-shadow", "none");
        await Page.ClickAsync("#btn-read-overlapping");
        await Expect(Page.Locator("#is-overlapping")).ToHaveTextAsync("False");

        await ScrollBoxAsync("#elevate-box", -200);
        await Expect(Page.Locator("#overlap-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#overlap-count")).ToHaveTextAsync("3");

        // Elevated takes precedence.
        await Expect(Page.Locator("#elevate-elevated")).ToHaveClassAsync(HasClass("bit-ftr-elv"));
        await Expect(Page.Locator("#elevate-elevated")).Not.ToHaveClassAsync(HasClass("bit-ftr-esc"));
    }

    [TestMethod]
    public async Task ScrollTarget_TheFooterFollowsTheNamedPane_OrFallsBackToItsScrollParent()
    {
        await OpenAsync("/e2e/footer");

        await ScrollBoxAsync("#ftr-scroll-pane", 300);
        await Expect(Page.Locator("#target-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#target-footer")).ToHaveClassAsync(HasClass("bit-ftr-hdn"));

        await Expect(Page.Locator("#target-miss-state")).ToHaveTextAsync("True");
        await Page.Locator("#target-miss-box").EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Page.WaitForFunctionAsync("() => { const b = document.getElementById('target-miss-box'); return b.scrollTop + b.clientHeight >= b.scrollHeight - 1; }");
        await Expect(Page.Locator("#target-miss-state")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Hidden_SlidesTheFooterAwayAndMakesItInert()
    {
        await OpenAsync("/e2e/footer");

        var footer = Page.Locator("#hidden-footer");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-anm"));
        Assert.IsNull(await footer.GetAttributeAsync("inert"));
        await Page.Locator("#hidden-inner").FocusAsync();
        await Expect(Page.Locator("#hidden-inner")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-toggle-hidden");
        await Expect(Page.Locator("#hidden-state")).ToHaveTextAsync("True");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-hdn"));
        await Expect(footer).ToHaveAttributeAsync("inert", "");
        var height = await footer.EvaluateAsync<double>("el => el.offsetHeight");
        await Expect(footer).ToHaveCSSAsync("transform", $"matrix(1, 0, 0, 1, 0, {height})");

        await Page.Locator("#hidden-inner").EvaluateAsync("el => el.focus()");
        await Expect(Page.Locator("#hidden-inner")).Not.ToBeFocusedAsync();

        await Page.ClickAsync("#btn-toggle-hidden");
        await Expect(footer).Not.ToHaveClassAsync(HasClass("bit-ftr-hdn"));
        Assert.IsNull(await footer.GetAttributeAsync("inert"));
        await Expect(footer).ToHaveCSSAsync("transform", "none");

        await Expect(Page.Locator("#inert-footer")).ToHaveAttributeAsync("inert", "");
    }

    [TestMethod]
    public async Task Translucent_BlursWhatPassesBehindAFillFooter()
    {
        await OpenAsync("/e2e/footer");

        var footer = Page.Locator("#translucent-footer");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-trs"));
        await Expect(footer).ToHaveCSSAsync("backdrop-filter", "blur(12px)");
        await Expect(footer).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "color-mix(in srgb, var(--bit-clr-bg-pri) 72%, transparent)"));

        await Expect(Page.Locator("#translucent-outline")).ToHaveCSSAsync("backdrop-filter", "none");
    }

    [TestMethod]
    public async Task Accessibility_AriaLabelFromTheParameterAndFromTheAttributes()
    {
        await OpenAsync("/e2e/footer");

        await Expect(Page.Locator("#labelled-footer")).ToHaveAttributeAsync("aria-label", "Editor actions");
        await Expect(Page.Locator("#labelled-attr-footer")).ToHaveAttributeAsync("aria-label", "Toolbar from attributes");
        await Expect(Page.Locator("#hidden-footer")).ToHaveAttributeAsync("aria-label", "Selection actions");
        Assert.IsNull(await Page.Locator("#basic-footer").GetAttributeAsync("aria-label"));
    }

    [TestMethod]
    public async Task ScrollPadding_ReservesTheHeightOfThePinnedFooterInItsScrollingArea()
    {
        await OpenAsync("/e2e/footer");

        await Expect(Page.Locator("#padding-box")).ToHaveCSSAsync("scroll-padding-bottom", "50px");
        await Expect(Page.Locator("#no-padding-box")).ToHaveCSSAsync("scroll-padding-bottom", "auto");

        // Tabbing forwards scrolls each control in from below: it stops above the footer, never under it.
        await Page.Locator("#pad-btn-1").FocusAsync();
        for (var i = 0; i < 8; i++) await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#pad-btn-9")).ToBeFocusedAsync();
        await NextFramesAsync();
        var footer = await BoxAsync("#padding-footer");
        var focused = await BoxAsync("#pad-btn-9");
        Assert.IsTrue(focused.Bottom <= footer.Y + Tolerance, $"the focused control ({focused.Bottom}) is above the footer ({footer.Y})");
    }

    [TestMethod]
    public async Task CascadingParameters_BitFooterParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/footer");

        var cascaded = Page.Locator("#casc-footer");
        foreach (var cls in new[] { "bit-ftr-err", "bit-ftr-otl", "bit-ftr-sm", "bit-ftr-brd" })
        {
            await Expect(cascaded).ToHaveClassAsync(HasClass(cls));
        }
        await Expect(cascaded).ToHaveCSSAsync("border-top-color", await ResolveAsync("color", "var(--bit-clr-err)"));

        var own = Page.Locator("#casc-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-ftr-ter"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-ftr-err"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-ftr-otl"));

        var outside = Page.Locator("#casc-out");
        await Expect(outside).ToHaveClassAsync(HasClass("bit-ftr-fil"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-ftr-err"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-ftr-sm"));
    }

    [TestMethod]
    [DataRow("#size-sm", "bit-ftr-sm", "4px", "16px")]
    [DataRow("#size-md", "bit-ftr-md", "8px", "24px")]
    [DataRow("#size-lg", "bit-ftr-lg", "12px", "32px")]
    public async Task Size_EveryValue_SetsThePaddingsAroundTheContent(string id, string cls, string block, string inline)
    {
        await OpenAsync("/e2e/footer");

        await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator($"{id} > .bit-ftr-gut")).ToHaveCSSAsync("padding-top", block);
        await Expect(Page.Locator($"{id} > .bit-ftr-gut")).ToHaveCSSAsync("padding-bottom", block);
        await Expect(Page.Locator($"{id} > .bit-ftr-gut")).ToHaveCSSAsync("padding-left", inline);
        await Expect(Page.Locator($"{id} > .bit-ftr-gut")).ToHaveCSSAsync("padding-right", inline);
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheDefault_AndExplicitParametersWin()
    {
        await OpenAsync("/e2e/footer");

        var footer = Page.Locator("#var-footer");
        await Expect(footer).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(footer).ToHaveCSSAsync("color", "rgb(200, 210, 220)");
        await Expect(footer).ToHaveCSSAsync("border-top-color", "rgb(0, 255, 0)");
        await Expect(footer).ToHaveCSSAsync("border-top-left-radius", "12px");
        await Expect(footer).ToHaveCSSAsync("box-shadow", "rgb(255, 0, 0) 0px -2px 4px 0px");
        await Expect(footer).ToHaveCSSAsync("z-index", "7");
        await Expect(footer).ToHaveCSSAsync("min-height", "64px");
        await Expect(footer).ToHaveCSSAsync("font-size", "19px");
        var gut = Page.Locator("#var-footer > .bit-ftr-gut");
        await Expect(gut).ToHaveCSSAsync("padding-top", "5px");
        await Expect(gut).ToHaveCSSAsync("padding-left", "7px");
        await Expect(gut).ToHaveCSSAsync("max-width", "400px");
        AssertClose(9, (await BoxAsync("#var-2")).X - (await BoxAsync("#var-1")).Right, "--bit-Footer-gap");

        // The variables restyle the default, never a choice.
        await Expect(Page.Locator("#var-color")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-err)"));
        await Expect(Page.Locator("#var-size > .bit-ftr-gut")).ToHaveCSSAsync("padding-top", "12px");
        AssertClose(2, (await BoxAsync("#vg-2")).X - (await BoxAsync("#vg-1")).Right, "the Gap parameter outranks --bit-Footer-gap");
        await Expect(Page.Locator("#var-gap > .bit-ftr-gut")).ToHaveCSSAsync("max-width", "500px");

        var outline = Page.Locator("#var-outline");
        await Expect(outline).ToHaveCSSAsync("border-top-color", "rgb(0, 255, 0)");
        await Expect(outline).ToHaveCSSAsync("color", "rgb(200, 210, 220)");

        await Expect(Page.Locator("#var-translucent")).ToHaveCSSAsync("backdrop-filter", "blur(3px)");
    }

    [TestMethod]
    public async Task CssVariables_EachVariantReadsItsOwnTierAheadOfTheSharedOne()
    {
        await OpenAsync("/e2e/footer");

        await Expect(Page.Locator("#tier-fill")).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
        await Expect(Page.Locator("#tier-fill")).ToHaveCSSAsync("color", "rgb(3, 3, 3)");
        await Expect(Page.Locator("#tier-fill")).ToHaveCSSAsync("border-top-color", "rgb(6, 6, 6)");

        await Expect(Page.Locator("#tier-outline")).ToHaveCSSAsync("border-top-color", "rgb(70, 80, 90)");
        await Expect(Page.Locator("#tier-outline")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");

        await Expect(Page.Locator("#tier-text")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(Page.Locator("#tier-text")).ToHaveCSSAsync("border-top-color", "rgb(5, 5, 5)");

        await Expect(Page.Locator("#tier-translucent")).ToHaveCSSAsync("backdrop-filter", "blur(5px)");
    }

    [TestMethod]
    public async Task StyleClassStylesClasses_ReachTheRootAndTheContainer()
    {
        await OpenAsync("/e2e/footer");

        await Expect(Page.Locator("#styled-footer")).ToHaveCSSAsync("background-color", "rgb(126, 87, 194)");
        await Expect(Page.Locator("#styled-footer")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");

        await Expect(Page.Locator("#classed-footer")).ToHaveClassAsync(HasClass("ftr-custom"));
        await Expect(Page.Locator("#classed-footer")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");

        await Expect(Page.Locator("#styles-footer")).ToHaveCSSAsync("border-top-style", "dashed");
        await Expect(Page.Locator("#styles-footer")).ToHaveCSSAsync("border-top-color", "rgb(255, 99, 71)");
        await Expect(Page.Locator("#styles-footer > .bit-ftr-gut")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        var footer = await BoxAsync("#styles-footer");
        var child = await BoxAsync("#styles-child");
        AssertClose(footer.X + (footer.W - child.W) / 2, child.X, "Styles.Container centers the content");

        await Expect(Page.Locator("#classes-footer")).ToHaveClassAsync(HasClass("ftr-root-class"));
        await Expect(Page.Locator("#classes-footer")).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(Page.Locator("#classes-footer > .bit-ftr-gut")).ToHaveClassAsync(HasClass("ftr-container-class"));
        await Expect(Page.Locator("#classes-footer > .bit-ftr-gut")).ToHaveCSSAsync("letter-spacing", "3px");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed()
    {
        await OpenAsync("/e2e/footer");

        await Page.ClickAsync("#btn-hidden-vis");
        await Expect(Page.Locator("#vis-footer")).ToHaveCSSAsync("visibility", "hidden");
        await Page.ClickAsync("#btn-collapsed-vis");
        await Expect(Page.Locator("#vis-footer")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_MirrorTheContent()
    {
        await OpenAsync("/e2e/footer");

        var footer = Page.Locator("#rtl-footer");
        await Expect(footer).ToHaveAttributeAsync("dir", "rtl");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-rtl"));
        var box = await BoxAsync("#rtl-footer");
        AssertClose(box.Right - 24, (await BoxAsync("#rtl-1")).Right, "the first child at the right, inside the gutter");
        Assert.IsTrue((await BoxAsync("#rtl-2")).X < (await BoxAsync("#rtl-1")).X, "the second to its left");

        AssertClose((await BoxAsync("#rtl-inherited")).Right - 24, (await BoxAsync("#rtli-1")).Right, "inherited");
    }

    [TestMethod]
    public async Task Runtime_TurningRevealOnAndOff_AttachesAndDetachesTheScrollListener()
    {
        await OpenAsync("/e2e/footer");

        await ScrollBoxAsync("#runtime-box", 300);
        await Expect(Page.Locator("#runtime-box")).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        await SyncAsync();
        await Expect(Page.Locator("#runtime-footer")).Not.ToHaveClassAsync(HasClass("bit-ftr-hdn"));

        await Page.ClickAsync("#btn-runtime");
        await Expect(Page.Locator("#runtime-state")).ToContainTextAsync("reveal=True");
        await ScrollBoxAsync("#runtime-box", 300);
        await Expect(Page.Locator("#runtime-state")).ToHaveTextAsync("reveal=True;revealed=False");
        await Expect(Page.Locator("#runtime-footer")).ToHaveClassAsync(HasClass("bit-ftr-hdn"));

        await Page.ClickAsync("#btn-runtime");
        await Expect(Page.Locator("#runtime-state")).ToContainTextAsync("reveal=False");
        await Expect(Page.Locator("#runtime-footer")).Not.ToHaveClassAsync(HasClass("bit-ftr-hdn"));
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAFooterWithScrollListeners_RaisesNoError()
    {
        await OpenAsync("/e2e/footer");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-footer")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#mount-footer")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mount-footer")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Window_AFixedFooter_RevealsElevatesAndPadsTheDocument()
    {
        await OpenAsync("/e2e/footer/window");

        var footer = Page.Locator("#window-footer");
        await Expect(footer).ToHaveCSSAsync("position", "fixed");
        await Expect(Page.Locator("html")).ToHaveCSSAsync("scroll-padding-bottom", "56px");
        await Expect(Page.Locator("#overlapping")).ToHaveTextAsync("True");
        var viewport = Page.ViewportSize!;
        AssertClose(viewport.Height, (await BoxAsync("#window-footer")).Bottom, "fixed at the bottom of the window");

        await Page.Mouse.MoveAsync(300, 300);
        await WheelWindowAsync(600);
        await Expect(Page.Locator("#revealed")).ToHaveTextAsync("False");
        await Expect(footer).ToHaveClassAsync(HasClass("bit-ftr-hdn"));

        await WheelWindowAsync(-200);
        await Expect(Page.Locator("#revealed")).ToHaveTextAsync("True");

        await WheelWindowAsync(10000);
        await Expect(Page.Locator("#overlapping")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#revealed")).ToHaveTextAsync("True");
        await Expect(footer).Not.ToHaveClassAsync(HasClass("bit-ftr-elv"));
    }

    private async Task ScrollBoxAsync(string selector, int deltaY)
    {
        var box = Page.Locator(selector);
        await box.ScrollIntoViewIfNeededAsync();
        var rect = await box.BoundingBoxAsync();
        var before = await box.EvaluateAsync<double>("el => el.scrollTop");
        await Page.Mouse.MoveAsync(rect!.X + rect.Width / 2, rect.Y + 20);
        await Page.Mouse.WheelAsync(0, deltaY);
        await WaitForScrollToSettleAsync(selector, before);
    }

    private async Task WheelWindowAsync(int deltaY)
    {
        var before = await Page.EvaluateAsync<double>("() => document.scrollingElement.scrollTop");
        await Page.Mouse.WheelAsync(0, deltaY);
        await WaitForScrollToSettleAsync("window", before);
    }

    // Waits for a wheel scroll to have moved the element and come to rest: a wheel scroll is animated, and a second
    // wheel sent while the first is still running only moves the target of the same animation.
    private Task WaitForScrollToSettleAsync(string selector, double before)
    {
        return Page.EvaluateAsync(@"([selector, before]) => new Promise(resolve => {
            const el = selector === 'window' ? document.scrollingElement : document.querySelector(selector);
            let last = el.scrollTop, stable = 0, frames = 0;
            const tick = () => {
                frames++;
                const now = el.scrollTop;
                if (now === last) stable++; else { stable = 0; last = now; }
                if ((now !== before && stable >= 5) || frames > 120) return resolve(now);
                requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
        })", new object[] { selector, before });
    }


    private async Task SyncAsync()
    {
        await NextFramesAsync();
        var before = int.Parse(await Page.Locator("#sync").InnerTextAsync());
        await Page.EvaluateAsync("() => document.getElementById('btn-sync').click()");
        await Expect(Page.Locator("#sync")).ToHaveTextAsync((before + 1).ToString());
    }

    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(@"([property, value]) => {
            const probe = document.createElement('div');
            probe.style.setProperty(property, value);
            document.body.appendChild(probe);
            const resolved = getComputedStyle(probe).getPropertyValue(property);
            probe.remove();
            return resolved;
        }", new[] { property, value });
    }

    private async Task<Box> BoxAsync(string selector)
    {
        var box = await Page.Locator(selector).BoundingBoxAsync();
        Assert.IsNotNull(box, $"{selector} has no box");
        return new(box.X, box.Y, box.Width, box.Height);
    }

    private static void AssertClose(double expected, double actual, string what)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= Tolerance, $"{what}: expected {expected}, got {actual}");
    }

    private sealed record Box(double X, double Y, double W, double H)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
    }
}
