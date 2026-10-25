namespace Bit.BlazorUI.Tests.E2E.Layouts;

/// <summary>
/// BitHeader in a real browser: the banner landmark, Height (and its runtime change), Disabled (colors, and the content
/// and the page's handlers staying live in a layout host), every Variant and every Color with every variant (read
/// against the theme tokens), Bordered / Elevated / Translucent, every Alignment and VerticalAlign value off the real
/// layout, Gap / Wrap / NoGutter / MaxWidth, ExtensionContent, Sticky / Fixed / Absolute and which one wins, the scroll
/// driven behaviours scrolled with the real mouse wheel - Reveal with RevealOffset, OnRevealChanged and IsRevealed, the
/// focus bringing it back and the keyboard focus keeping it, ElevateOnScroll with ElevateOffset, OnScrolledChanged and
/// IsScrolled, ScrollTarget (and a selector matching nothing), ScrollPadding, on a scrolling box and on the window -
/// Hidden and inert, the skip link, the BitParams cascade, Size, the --bit-Header-* variables and their variant tiers,
/// Style / Class / Styles / Classes / Visibility, RTL, prerendering, a runtime Reveal toggle and unmounting.
/// </summary>
[TestClass]
public class BitHeaderE2ETests : E2ETestBase
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
    public async Task Prerender_TheHeaderIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/header");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<header");
        StringAssert.Contains(html, "id=\"basic-header\"");
        StringAssert.Contains(html, "bit-hdr-gut");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_AHeaderElement_SizedByItsContentAndPaddings()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#basic-header");
        Assert.AreEqual("HEADER", await header.EvaluateAsync<string>("el => el.tagName"));
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr"));
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-fil"));
        await Expect(header.Locator("> .bit-hdr-gut")).ToHaveCSSAsync("padding-top", "8px");
        await Expect(header.Locator("> .bit-hdr-gut")).ToHaveCSSAsync("padding-left", "24px");
        AssertClose(20 + 16, (await BoxAsync("#basic-header")).H, "the content plus the default paddings");
        AssertClose(24, (await BoxAsync("#basic-child")).X - (await BoxAsync("#basic-header")).X, "inset by the gutter");

        // The one header outside every section is the page's banner landmark.
        await Expect(Page.GetByRole(AriaRole.Banner)).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Banner)).ToHaveAttributeAsync("id", "banner");
    }

    [TestMethod]
    public async Task Height_FixesTheHeaderInPixels_AndReRendersAtRuntime()
    {
        await OpenAsync("/e2e/header");

        AssertClose(80, (await BoxAsync("#height-header")).H, "Height 80");

        await Page.ClickAsync("#btn-height");

        await Expect(Page.Locator("#height-header")).ToHaveCSSAsync("height", "100px");
    }

    [TestMethod]
    public async Task Disabled_TheHeaderDims_AndItsContentStaysLive()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#disabled-header");
        await Expect(header).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(header).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));
        await Expect(header).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-pri)"));

        // A header is a layout host: what bubbles up to it is the enabled content's own event, so it keeps the page's
        // handlers and its controls keep working.
        await Page.ClickAsync("#disabled-inner");
        await Expect(Page.Locator("#inner-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(header).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(header).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-pri)"));
    }

    [TestMethod]
    public async Task Variant_FillOutlineAndText_PaintTheSurfaceDifferently()
    {
        await OpenAsync("/e2e/header");

        var fill = Page.Locator("#variant-fill");
        await Expect(fill).ToHaveClassAsync(HasClass("bit-hdr-fil"));
        await Expect(fill).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-pri)"));
        await Expect(fill).ToHaveCSSAsync("border-top-style", "none");

        var outline = Page.Locator("#variant-outline");
        await Expect(outline).ToHaveClassAsync(HasClass("bit-hdr-otl"));
        await Expect(outline).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(outline).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(outline).ToHaveCSSAsync("border-top-color", await ResolveAsync("border-top-color", "var(--bit-clr-brd-pri)"));

        var text = Page.Locator("#variant-text");
        await Expect(text).ToHaveClassAsync(HasClass("bit-hdr-txt"));
        await Expect(text).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(text).ToHaveCSSAsync("border-top-style", "none");
        await Expect(text).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-pri)"));
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsEveryVariantWithItsRole()
    {
        await OpenAsync("/e2e/header");

        var transparent = "rgba(0, 0, 0, 0)";
        foreach (var (name, suffix, main, on) in Colors)
        {
            var mainColor = await ResolveAsync("color", $"var({main})");
            Assert.AreNotEqual(transparent, mainColor, $"{main} resolves to a color");

            var fill = Page.Locator($"#color-fill-{name}");
            await Expect(fill).ToHaveClassAsync(HasClass($"bit-hdr-{suffix}"));
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
    public async Task BorderedAndElevated_DrawADividerAndAShadow()
    {
        await OpenAsync("/e2e/header");

        var bordered = Page.Locator("#bordered");
        await Expect(bordered).ToHaveClassAsync(HasClass("bit-hdr-brd"));
        await Expect(bordered).ToHaveCSSAsync("border-bottom-style", "solid");
        await Expect(bordered).ToHaveCSSAsync("border-bottom-width", "1px");
        await Expect(bordered).ToHaveCSSAsync("border-bottom-color", await ResolveAsync("color", "var(--bit-clr-brd-pri)"));
        await Expect(bordered).ToHaveCSSAsync("border-top-style", "none");
        await Expect(Page.Locator("#bordered-text")).ToHaveCSSAsync("border-bottom-style", "solid");

        await Expect(Page.Locator("#elevated")).ToHaveClassAsync(HasClass("bit-hdr-elv"));
        Assert.AreNotEqual("none", await ComputedStyleAsync("#elevated", "box-shadow"));
        Assert.AreEqual("none", await ComputedStyleAsync("#plain", "box-shadow"));
        await Expect(Page.Locator("#plain")).ToHaveCSSAsync("border-bottom-style", "none");
    }

    [TestMethod]
    [DataRow("", "", "normal", 0)]
    [DataRow("Start", "bit-hdr-srt", "flex-start", 0)]
    [DataRow("End", "bit-hdr-end", "flex-end", 420)]
    [DataRow("Center", "bit-hdr-cnt", "center", 210)]
    [DataRow("SpaceBetween", "bit-hdr-sbt", "space-between", 0)]
    [DataRow("SpaceAround", "bit-hdr-sar", "space-around", 70)]
    [DataRow("SpaceEvenly", "bit-hdr-sev", "space-evenly", 105)]
    [DataRow("Baseline", "bit-hdr-bsl", "normal", 0)]
    [DataRow("Stretch", "bit-hdr-str", "normal", 0)]
    public async Task Alignment_EveryValue_DistributesTheContentAlongTheLine(string value, string cls, string justify, double firstX)
    {
        await OpenAsync("/e2e/header");

        await Page.SelectOptionAsync("#sel-al", value);
        await Expect(Page.Locator("#al-state")).ToContainTextAsync($"al={value};");

        if (cls.Length > 0) await Expect(Page.Locator("#al-header")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator("#al-header > .bit-hdr-gut")).ToHaveCSSAsync("justify-content", justify);
        var header = await BoxAsync("#al-header");
        AssertClose(firstX, (await BoxAsync("#al-1")).X - header.X, $"{value}: first child x");
        if (value == "SpaceBetween") AssertClose(header.Right, (await BoxAsync("#al-3")).Right, "the last at the end");

        // Baseline and Stretch distribute nothing, so they reach the cross axis instead.
        if (value is "Baseline" or "Stretch")
        {
            await Expect(Page.Locator("#al-header > .bit-hdr-gut")).ToHaveCSSAsync("align-items", value.ToLowerInvariant());
        }
    }

    [TestMethod]
    [DataRow("", "", "center", 30)]
    [DataRow("Start", "bit-hdr-vst", "flex-start", 0)]
    [DataRow("End", "bit-hdr-ved", "flex-end", 60)]
    [DataRow("Center", "bit-hdr-vcn", "center", 30)]
    [DataRow("Baseline", "bit-hdr-bsl", "baseline", -1)]
    [DataRow("Stretch", "bit-hdr-str", "stretch", 0)]
    public async Task VerticalAlign_EveryValue_PlacesTheContentInTheHeight(string value, string cls, string alignItems, double y)
    {
        await OpenAsync("/e2e/header");

        await Page.SelectOptionAsync("#sel-va", value);
        await Expect(Page.Locator("#al-state")).ToContainTextAsync($"va={value};");

        if (cls.Length > 0) await Expect(Page.Locator("#al-header")).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator("#al-header > .bit-hdr-gut")).ToHaveCSSAsync("align-items", alignItems);
        if (y >= 0) AssertClose(y, (await BoxAsync("#al-1")).Y - (await BoxAsync("#al-header")).Y, $"{value}: y");
    }

    [TestMethod]
    public async Task Layout_GapWrapNoGutterAndMaxWidth()
    {
        await OpenAsync("/e2e/header");

        AssertClose(8, (await BoxAsync("#gap-2")).X - (await BoxAsync("#gap-1")).Right, "Gap 0.5rem");

        await Expect(Page.Locator("#wrap-header")).ToHaveClassAsync(HasClass("bit-hdr-wrp"));
        await Expect(Page.Locator("#wrap-header > .bit-hdr-gut")).ToHaveCSSAsync("flex-wrap", "wrap");
        AssertClose(10, (await BoxAsync("#wrap-2")).X - (await BoxAsync("#wrap-1")).Right, "the column gap");
        AssertClose((await BoxAsync("#wrap-1")).X, (await BoxAsync("#wrap-4")).X, "three per line, the fourth wraps");
        AssertClose(4, (await BoxAsync("#wrap-4")).Y - (await BoxAsync("#wrap-1")).Bottom, "the row gap");

        await Expect(Page.Locator("#nowrap-header > .bit-hdr-gut")).ToHaveCSSAsync("flex-wrap", "nowrap");
        AssertClose((await BoxAsync("#nowrap-1")).Y, (await BoxAsync("#nowrap-6")).Y, "one line without Wrap");

        await Expect(Page.Locator("#nogutter-header > .bit-hdr-gut")).ToHaveCSSAsync("padding-top", "0px");
        AssertClose((await BoxAsync("#nogutter-header")).X, (await BoxAsync("#nogutter-child")).X, "no inline padding");
        AssertClose(20, (await BoxAsync("#nogutter-header")).H, "no block padding");

        // MaxWidth caps and centers the content of both rows while the surface stays full width.
        var header = await BoxAsync("#maxwidth-header");
        AssertClose(600, header.W, "the surface is full width");
        var gutter = await BoxAsync("#maxwidth-header > .bit-hdr-gut");
        AssertClose(300, gutter.W, "the content is capped");
        AssertClose(header.X + 150, gutter.X, "and centered");
        AssertClose(header.X + 450, (await BoxAsync("#mw-2")).Right, "SpaceBetween within the cap");
        AssertClose(300, (await BoxAsync("#maxwidth-header > .bit-hdr-ext")).W, "the extension row is capped too");
        AssertClose(header.X + 150, (await BoxAsync("#mw-ext")).X, "and lines up");
    }

    [TestMethod]
    public async Task ExtensionContent_AddsASecondRowSharingTheSurface()
    {
        await OpenAsync("/e2e/header");

        var ext = Page.Locator("#ext-header > .bit-hdr-ext");
        await Expect(ext).ToHaveCountAsync(1);
        await Expect(ext).ToHaveClassAsync(HasClass("hdr-extension-class"));
        await Expect(ext).ToHaveCSSAsync("min-height", "30px");
        await Expect(ext).ToHaveCSSAsync("padding-left", "24px");

        var main = await BoxAsync("#ext-main");
        var child = await BoxAsync("#ext-child");
        Assert.IsTrue(child.Y >= main.Bottom, "the second row is below the main one");
        AssertClose(main.X, child.X, "lined up with the main row's gutter");

        // With a second row the Height is a floor rather than the height.
        await Expect(Page.Locator("#ext-header")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("min-height:60px"));
        var header = await BoxAsync("#ext-header");
        Assert.IsTrue(header.H >= 60 - Tolerance, "the Height is the floor");
        AssertClose((await BoxAsync("#ext-header > .bit-hdr-gut")).H + (await BoxAsync("#ext-header > .bit-hdr-ext")).H, header.H, "the two rows decide the height");

        await Expect(Page.Locator("#basic-header > .bit-hdr-ext")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Position_StickyFixedAbsolute_AndTheWidestReachWins()
    {
        await OpenAsync("/e2e/header");

        var sticky = Page.Locator("#sticky-header");
        await Expect(sticky).ToHaveClassAsync(HasClass("bit-hdr-stk"));
        await Expect(sticky).ToHaveCSSAsync("position", "sticky");
        await ScrollBoxAsync("#sticky-box", 300);
        await Expect(Page.Locator("#sticky-box")).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        AssertClose((await BoxAsync("#sticky-box")).Y + 1, (await BoxAsync("#sticky-header")).Y, "pinned at the top of the scrolled box");

        await Expect(Page.Locator("#absolute-header")).ToHaveCSSAsync("position", "absolute");
        var host = await BoxAsync("#abs-host");
        var abs = await BoxAsync("#absolute-header");
        AssertClose(host.Y, abs.Y, "absolute at the top of its container");
        AssertClose(host.W, abs.W, "and as wide");

        await Expect(Page.Locator("#fixed-header")).ToHaveCSSAsync("position", "fixed");
        AssertClose((await BoxAsync("#fixed-host")).Y, (await BoxAsync("#fixed-header")).Y, "fixed to its containing block");

        await Expect(Page.Locator("#all-positions")).ToHaveClassAsync(HasClass("bit-hdr-fix"));
        await Expect(Page.Locator("#all-positions")).Not.ToHaveClassAsync(HasClass("bit-hdr-abs"));
        await Expect(Page.Locator("#all-positions")).Not.ToHaveClassAsync(HasClass("bit-hdr-stk"));
        await Expect(Page.Locator("#abs-sticky")).ToHaveClassAsync(HasClass("bit-hdr-abs"));
        await Expect(Page.Locator("#abs-sticky")).Not.ToHaveClassAsync(HasClass("bit-hdr-stk"));

        // A pinned header adds the safe area inset to its Height, which resolves to nothing here.
        await Expect(Page.Locator("#sticky-height")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("env\\(safe-area-inset-top"));
        AssertClose(50, (await BoxAsync("#sticky-height")).H, "Height 50 plus no inset");
    }

    [TestMethod]
    public async Task Reveal_ScrollingDownHidesTheHeader_ScrollingUpBringsItBack()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#reveal-header");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-anm"));

        await ScrollBoxAsync("#reveal-box", 400);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("False");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-hdn"));
        var height = await header.EvaluateAsync<double>("el => el.offsetHeight");
        await Expect(header).ToHaveCSSAsync("transform", $"matrix(1, 0, 0, 1, 0, -{height})");
        await Expect(header).ToHaveCSSAsync("box-shadow", "none");

        await Page.ClickAsync("#btn-read-revealed");
        await Expect(Page.Locator("#is-revealed")).ToHaveTextAsync("False");

        await ScrollBoxAsync("#reveal-box", -100);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("True");
        await Expect(header).Not.ToHaveClassAsync(HasClass("bit-hdr-hdn"));
        await Expect(header).ToHaveCSSAsync("transform", "none");

        await Page.ClickAsync("#btn-read-revealed");
        await Expect(Page.Locator("#is-revealed")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#reveal-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Reveal_AFocusInsideTheHiddenHeader_BringsItBack()
    {
        await OpenAsync("/e2e/header");

        await ScrollBoxAsync("#reveal-box", 400);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("False");

        await Page.Locator("#reveal-inner").FocusAsync();

        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#reveal-header")).Not.ToHaveClassAsync(HasClass("bit-hdr-hdn"));
    }

    [TestMethod]
    public async Task Reveal_WhileItHoldsTheKeyboardFocus_TheHeaderStays()
    {
        await OpenAsync("/e2e/header");

        await Page.Locator("#btn-read-revealed").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#reveal-inner")).ToBeFocusedAsync();

        // Scrolled down with the keyboard focus inside: nothing changes. Then up, which would reveal it had it hidden.
        await ScrollBoxAsync("#reveal-box", 400);
        await NextFramesAsync();
        await ScrollBoxAsync("#reveal-box", -100);
        await NextFramesAsync();

        // Once the focus has left, the next scroll down hides it: the only change of the whole run.
        await Page.Locator("#btn-before-reveal").FocusAsync();
        await ScrollBoxAsync("#reveal-box", 200);
        await Expect(Page.Locator("#reveal-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#reveal-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task RevealOffset_KeepsTheHeaderUntilTheScrollHasPassedIt()
    {
        await OpenAsync("/e2e/header");

        await ScrollBoxAsync("#reveal-offset-box", 100);
        await Expect(Page.Locator("#reveal-offset-box")).ToHaveJSPropertyAsync("scrollTop", 100);
        await SyncAsync();
        await Expect(Page.Locator("#reveal-offset-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#reveal-offset-header")).Not.ToHaveClassAsync(HasClass("bit-hdr-hdn"));

        await ScrollBoxAsync("#reveal-offset-box", 300);
        await Expect(Page.Locator("#reveal-offset-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#reveal-offset-header")).ToHaveClassAsync(HasClass("bit-hdr-hdn"));
    }

    [TestMethod]
    public async Task Reveal_AHeaderThatIsNotPinned_NeverHides()
    {
        await OpenAsync("/e2e/header");

        await ScrollBoxAsync("#reveal-unpinned-box", 400);
        await Expect(Page.Locator("#reveal-unpinned-box")).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        await SyncAsync();

        await Expect(Page.Locator("#reveal-unpinned-header")).Not.ToHaveClassAsync(HasClass("bit-hdr-hdn"));
        await Expect(Page.Locator("#reveal-unpinned-header")).ToHaveClassAsync(HasClass("bit-hdr-anm"));
    }

    [TestMethod]
    public async Task ElevateOnScroll_TheShadowFadesInOnceContentScrollsUnderIt()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#elevate-header");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-esc"));
        await Expect(header).Not.ToHaveClassAsync(HasClass("bit-hdr-elv"));
        Assert.AreEqual("none", await ComputedStyleAsync("#elevate-header", "box-shadow"));
        AssertClose(70, (await BoxAsync("#elevate-header")).H, "tall at the top");

        await ScrollBoxAsync("#elevate-box", 100);
        await Expect(Page.Locator("#scrolled-state")).ToHaveTextAsync("True");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-elv"));
        await Expect(header).Not.ToHaveCSSAsync("box-shadow", "none");
        // OnScrolledChanged drives the page, which shrinks the header.
        await Expect(header).ToHaveCSSAsync("height", "40px");

        await Page.ClickAsync("#btn-read-scrolled");
        await Expect(Page.Locator("#is-scrolled")).ToHaveTextAsync("True");

        await ScrollBoxAsync("#elevate-box", -500);
        await Expect(Page.Locator("#scrolled-state")).ToHaveTextAsync("False");
        await Expect(header).Not.ToHaveClassAsync(HasClass("bit-hdr-elv"));
        await Expect(Page.Locator("#scrolled-count")).ToHaveTextAsync("2");
        await Page.ClickAsync("#btn-read-scrolled");
        await Expect(Page.Locator("#is-scrolled")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task ElevateOffset_KeepsTheHeaderFlatUntilTheScrollHasPassedIt()
    {
        await OpenAsync("/e2e/header");

        await ScrollBoxAsync("#elevate-offset-box", 60);
        await Expect(Page.Locator("#elevate-offset-box")).ToHaveJSPropertyAsync("scrollTop", 60);
        await SyncAsync();
        await Expect(Page.Locator("#elevate-offset-state")).ToHaveTextAsync("False");

        await ScrollBoxAsync("#elevate-offset-box", 200);
        await Expect(Page.Locator("#elevate-offset-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#elevate-offset-header")).ToHaveClassAsync(HasClass("bit-hdr-elv"));
    }

    [TestMethod]
    public async Task ScrollTarget_TheHeaderFollowsTheNamedPane_OrFallsBackToItsScrollParent()
    {
        await OpenAsync("/e2e/header");

        await ScrollBoxAsync("#hdr-scroll-pane", 200);
        await Expect(Page.Locator("#target-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#target-header")).ToHaveClassAsync(HasClass("bit-hdr-elv"));

        await ScrollBoxAsync("#target-miss-box", 200);
        await Expect(Page.Locator("#target-miss-state")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task Hidden_SlidesTheHeaderAwayAndMakesItInert()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#hidden-header");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-anm"));
        Assert.IsNull(await header.GetAttributeAsync("inert"));
        await Page.Locator("#hidden-inner").FocusAsync();
        await Expect(Page.Locator("#hidden-inner")).ToBeFocusedAsync();

        await Page.ClickAsync("#btn-toggle-hidden");
        await Expect(Page.Locator("#hidden-state")).ToHaveTextAsync("True");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-hdn"));
        await Expect(header).ToHaveAttributeAsync("inert", "");
        var height = await header.EvaluateAsync<double>("el => el.offsetHeight");
        await Expect(header).ToHaveCSSAsync("transform", $"matrix(1, 0, 0, 1, 0, -{height})");
        AssertClose(height, (await header.EvaluateAsync<double>("el => el.offsetHeight")), "it keeps its room");

        await Page.Locator("#hidden-inner").EvaluateAsync("el => el.focus()");
        await Expect(Page.Locator("#hidden-inner")).Not.ToBeFocusedAsync();

        await Page.ClickAsync("#btn-toggle-hidden");
        await Expect(header).Not.ToHaveClassAsync(HasClass("bit-hdr-hdn"));
        Assert.IsNull(await header.GetAttributeAsync("inert"));
        await Expect(header).ToHaveCSSAsync("transform", "none");

        // An inert the page wrote by hand is kept while Hidden is off.
        await Expect(Page.Locator("#inert-header")).ToHaveAttributeAsync("inert", "");
    }

    [TestMethod]
    public async Task Translucent_BlursWhatPassesBehindAFillHeader()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#translucent-header");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-trs"));
        await Expect(header).ToHaveCSSAsync("backdrop-filter", "blur(12px)");
        await Expect(header).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "color-mix(in srgb, var(--bit-clr-bg-pri) 72%, transparent)"));

        await Expect(Page.Locator("#translucent-outline")).ToHaveCSSAsync("backdrop-filter", "none");
    }

    [TestMethod]
    public async Task Accessibility_AriaLabelFromTheParameterAndFromTheAttributes()
    {
        await OpenAsync("/e2e/header");

        await Expect(Page.Locator("#labelled-header")).ToHaveAttributeAsync("aria-label", "Editor actions");
        await Expect(Page.Locator("#labelled-attr-header")).ToHaveAttributeAsync("aria-label", "Toolbar from attributes");
        Assert.IsNull(await Page.Locator("#basic-header").GetAttributeAsync("aria-label"));
    }

    [TestMethod]
    public async Task SkipLink_IsTheFirstStop_VisibleOnlyOnFocus_AndLeadsToItsTarget()
    {
        await OpenAsync("/e2e/header");

        var skip = Page.Locator("#skip-header .bit-hdr-skp");
        // A bare fragment names this very page: resolved against the app's base href it would lead to another one.
        await Expect(skip).ToHaveAttributeAsync("href", $"{BaseUrl}/e2e/header#hdr-skip-target");
        await Expect(skip).ToHaveTextAsync("Skip to main content");
        AssertClose(1, (await BoxAsync("#skip-header .bit-hdr-skp")).W, "visually hidden until focused");

        await Page.Locator("#btn-before-skip").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(skip).ToBeFocusedAsync();
        Assert.IsTrue((await BoxAsync("#skip-header .bit-hdr-skp")).W > 50, "visible once focused");
        await Expect(skip).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-pri)"));

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#skip-next")).ToBeFocusedAsync();

        // SkipLinkText rewords it; Styles and Classes reach it.
        var custom = Page.Locator("#skip-text-header .bit-hdr-skp");
        await Expect(custom).ToHaveTextAsync("Jump to the content");
        await Expect(custom).ToHaveClassAsync(HasClass("hdr-skip-class"));
        await Expect(custom).ToHaveCSSAsync("font-style", "italic");

        // Following it stays on the page, scrolls the target into view and moves the focus onto it.
        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await skip.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#hdr-skip-target")).ToBeFocusedAsync();
        await Expect(Page.Locator("#hdr-skip-target")).ToBeInViewportAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/e2e/header$"));
        await Expect(Page.Locator("#basic-header")).ToBeVisibleAsync();

        await Expect(Page.Locator("#basic-header .bit-hdr-skp")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ScrollPadding_ReservesTheHeightOfThePinnedHeaderInItsScrollingArea()
    {
        await OpenAsync("/e2e/header");

        await Expect(Page.Locator("#padding-box")).ToHaveCSSAsync("scroll-padding-top", "50px");
        await Expect(Page.Locator("#no-padding-box")).ToHaveCSSAsync("scroll-padding-top", "auto");

        // Tabbing backwards from the bottom scrolls each control in from above: it stops below the header, never under it.
        await ScrollBoxAsync("#padding-box", 2000);
        await Expect(Page.Locator("#padding-box")).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        await Page.Locator("#pad-btn-12").FocusAsync();
        for (var i = 0; i < 8; i++) await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.Locator("#pad-btn-4")).ToBeFocusedAsync();
        await NextFramesAsync();
        var header = await BoxAsync("#padding-header");
        var focused = await BoxAsync("#pad-btn-4");
        Assert.IsTrue(focused.Y >= header.Bottom - Tolerance, $"the focused control ({focused.Y}) is below the header ({header.Bottom})");
    }

    [TestMethod]
    public async Task CascadingParameters_BitHeaderParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/header");

        var cascaded = Page.Locator("#casc-header");
        foreach (var cls in new[] { "bit-hdr-err", "bit-hdr-otl", "bit-hdr-sm", "bit-hdr-brd" })
        {
            await Expect(cascaded).ToHaveClassAsync(HasClass(cls));
        }
        await Expect(cascaded).ToHaveCSSAsync("border-top-color", await ResolveAsync("color", "var(--bit-clr-err)"));

        var own = Page.Locator("#casc-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-hdr-ter"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-hdr-err"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-hdr-otl"));

        var outside = Page.Locator("#casc-out");
        await Expect(outside).ToHaveClassAsync(HasClass("bit-hdr-fil"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-hdr-err"));
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-hdr-sm"));
    }

    [TestMethod]
    [DataRow("#size-sm", "bit-hdr-sm", "4px", "16px")]
    [DataRow("#size-md", "bit-hdr-md", "8px", "24px")]
    [DataRow("#size-lg", "bit-hdr-lg", "12px", "32px")]
    public async Task Size_EveryValue_SetsThePaddingsAroundTheContent(string id, string cls, string block, string inline)
    {
        await OpenAsync("/e2e/header");

        await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass(cls));
        await Expect(Page.Locator($"{id} > .bit-hdr-gut")).ToHaveCSSAsync("padding-top", block);
        await Expect(Page.Locator($"{id} > .bit-hdr-gut")).ToHaveCSSAsync("padding-bottom", block);
        await Expect(Page.Locator($"{id} > .bit-hdr-gut")).ToHaveCSSAsync("padding-left", inline);
        await Expect(Page.Locator($"{id} > .bit-hdr-gut")).ToHaveCSSAsync("padding-right", inline);
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheDefault_AndExplicitParametersWin()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#var-header");
        await Expect(header).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(header).ToHaveCSSAsync("color", "rgb(200, 210, 220)");
        await Expect(header).ToHaveCSSAsync("border-bottom-color", "rgb(0, 255, 0)");
        await Expect(header).ToHaveCSSAsync("border-bottom-width", "3px");
        await Expect(header).ToHaveCSSAsync("border-top-left-radius", "12px");
        await Expect(header).ToHaveCSSAsync("box-shadow", "rgb(255, 0, 0) 0px 2px 4px 0px");
        await Expect(header).ToHaveCSSAsync("z-index", "7");
        var gut = Page.Locator("#var-header > .bit-hdr-gut");
        await Expect(gut).ToHaveCSSAsync("padding-top", "5px");
        await Expect(gut).ToHaveCSSAsync("padding-left", "7px");
        await Expect(gut).ToHaveCSSAsync("min-height", "64px");
        await Expect(gut).ToHaveCSSAsync("max-width", "400px");
        AssertClose(9, (await BoxAsync("#var-2")).X - (await BoxAsync("#var-1")).Right, "--bit-Header-gap");

        // The variables restyle the default, never a choice.
        await Expect(Page.Locator("#var-color")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-err)"));
        await Expect(Page.Locator("#var-size > .bit-hdr-gut")).ToHaveCSSAsync("padding-top", "12px");
        AssertClose(2, (await BoxAsync("#vg-2")).X - (await BoxAsync("#vg-1")).Right, "the Gap parameter outranks --bit-Header-gap");

        var outline = Page.Locator("#var-outline");
        await Expect(outline).ToHaveCSSAsync("border-top-color", "rgb(0, 255, 0)");
        await Expect(outline).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(outline).ToHaveCSSAsync("color", "rgb(200, 210, 220)");
    }

    [TestMethod]
    public async Task CssVariables_EachVariantReadsItsOwnTierAheadOfTheSharedOne()
    {
        await OpenAsync("/e2e/header");

        await Expect(Page.Locator("#tier-fill")).ToHaveCSSAsync("background-color", "rgb(40, 50, 60)");
        await Expect(Page.Locator("#tier-fill")).ToHaveCSSAsync("color", "rgb(3, 3, 3)");

        await Expect(Page.Locator("#tier-outline")).ToHaveCSSAsync("border-top-color", "rgb(70, 80, 90)");
        await Expect(Page.Locator("#tier-outline")).ToHaveCSSAsync("border-top-width", "4px");
        await Expect(Page.Locator("#tier-outline")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");

        await Expect(Page.Locator("#tier-text")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(Page.Locator("#tier-text")).ToHaveCSSAsync("border-bottom-color", "rgb(5, 5, 5)");
        await Expect(Page.Locator("#tier-text")).ToHaveCSSAsync("border-bottom-width", "6px");
    }

    [TestMethod]
    public async Task CssVariables_DisabledTranslucentAndSkipLinkVariables()
    {
        await OpenAsync("/e2e/header");

        await Expect(Page.Locator("#var-disabled")).ToHaveCSSAsync("color", "rgb(11, 12, 13)");
        await Expect(Page.Locator("#var-disabled")).ToHaveCSSAsync("background-color", "rgb(14, 15, 16)");

        await Expect(Page.Locator("#var-translucent")).ToHaveCSSAsync("backdrop-filter", "blur(3px)");
        await Expect(Page.Locator("#var-translucent")).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "color-mix(in srgb, var(--bit-clr-bg-pri) 50%, transparent)"));

        var skip = Page.Locator("#var-skip .bit-hdr-skp");
        await skip.FocusAsync();
        await Expect(skip).ToHaveCSSAsync("color", "rgb(21, 22, 23)");
        await Expect(skip).ToHaveCSSAsync("background-color", "rgb(24, 25, 26)");
    }

    [TestMethod]
    public async Task StyleClassStylesClasses_ReachTheRootAndEachPart()
    {
        await OpenAsync("/e2e/header");

        await Expect(Page.Locator("#styled-header")).ToHaveCSSAsync("background-color", "rgb(126, 87, 194)");
        await Expect(Page.Locator("#styled-header")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");

        await Expect(Page.Locator("#classed-header")).ToHaveClassAsync(HasClass("hdr-custom"));
        await Expect(Page.Locator("#classed-header")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");

        await Expect(Page.Locator("#styles-header")).ToHaveCSSAsync("border-bottom-style", "dashed");
        await Expect(Page.Locator("#styles-header")).ToHaveCSSAsync("border-bottom-color", "rgb(255, 99, 71)");
        await Expect(Page.Locator("#styles-header > .bit-hdr-gut")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        var header = await BoxAsync("#styles-header");
        var child = await BoxAsync("#styles-child");
        AssertClose(header.X + (header.W - child.W) / 2, child.X, "Styles.Container centers the content");

        await Expect(Page.Locator("#classes-header")).ToHaveClassAsync(HasClass("hdr-root-class"));
        await Expect(Page.Locator("#classes-header")).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(Page.Locator("#classes-header > .bit-hdr-gut")).ToHaveClassAsync(HasClass("hdr-container-class"));
        await Expect(Page.Locator("#classes-header > .bit-hdr-gut")).ToHaveCSSAsync("letter-spacing", "3px");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed()
    {
        await OpenAsync("/e2e/header");

        await Page.ClickAsync("#btn-hidden-vis");
        await Expect(Page.Locator("#vis-header")).ToHaveCSSAsync("visibility", "hidden");
        await Page.ClickAsync("#btn-collapsed-vis");
        await Expect(Page.Locator("#vis-header")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_MirrorTheContent()
    {
        await OpenAsync("/e2e/header");

        var header = Page.Locator("#rtl-header");
        await Expect(header).ToHaveAttributeAsync("dir", "rtl");
        await Expect(header).ToHaveClassAsync(HasClass("bit-rtl"));
        var box = await BoxAsync("#rtl-header");
        AssertClose(box.Right - 24, (await BoxAsync("#rtl-1")).Right, "the first child at the right, inside the gutter");
        Assert.IsTrue((await BoxAsync("#rtl-2")).X < (await BoxAsync("#rtl-1")).X, "the second to its left");

        AssertClose((await BoxAsync("#rtl-inherited")).Right - 24, (await BoxAsync("#rtli-1")).Right, "inherited");
    }

    [TestMethod]
    public async Task Runtime_TurningRevealOnAndOff_AttachesAndDetachesTheScrollListener()
    {
        await OpenAsync("/e2e/header");

        await ScrollBoxAsync("#runtime-box", 300);
        await Expect(Page.Locator("#runtime-box")).Not.ToHaveJSPropertyAsync("scrollTop", 0);
        await SyncAsync();
        await Expect(Page.Locator("#runtime-header")).Not.ToHaveClassAsync(HasClass("bit-hdr-hdn"));

        await Page.ClickAsync("#btn-runtime");
        await Expect(Page.Locator("#runtime-state")).ToContainTextAsync("reveal=True");
        await ScrollBoxAsync("#runtime-box", 300);
        await Expect(Page.Locator("#runtime-state")).ToHaveTextAsync("reveal=True;revealed=False");
        await Expect(Page.Locator("#runtime-header")).ToHaveClassAsync(HasClass("bit-hdr-hdn"));

        // Turned off again, the header is no longer driven by the scroll and is not left hidden.
        await Page.ClickAsync("#btn-runtime");
        await Expect(Page.Locator("#runtime-state")).ToContainTextAsync("reveal=False");
        await Expect(Page.Locator("#runtime-header")).Not.ToHaveClassAsync(HasClass("bit-hdr-hdn"));
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAHeaderWithScrollListeners_RaisesNoError()
    {
        await OpenAsync("/e2e/header");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-header")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#mount-header")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mount-header")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Window_AFixedHeader_RevealsElevatesAndPadsTheDocument()
    {
        await OpenAsync("/e2e/header/window");

        var header = Page.Locator("#window-header");
        await Expect(header).ToHaveCSSAsync("position", "fixed");
        await Expect(Page.Locator("html")).ToHaveCSSAsync("scroll-padding-top", "56px");
        await Expect(Page.Locator("#scrolled")).ToHaveTextAsync("False");

        await Page.Mouse.MoveAsync(300, 400);
        await WheelWindowAsync(600);
        await Expect(Page.Locator("#revealed")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#scrolled")).ToHaveTextAsync("True");
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-hdn"));
        await Expect(header).ToHaveClassAsync(HasClass("bit-hdr-elv"));

        await WheelWindowAsync(-200);
        await Expect(Page.Locator("#revealed")).ToHaveTextAsync("True");
        await Expect(header).ToHaveCSSAsync("transform", "none");
        AssertClose(0, (await BoxAsync("#window-header")).Y, "fixed at the top of the window");

        await WheelWindowAsync(-5000);
        await Expect(Page.Locator("#scrolled")).ToHaveTextAsync("False");
    }

    private async Task ScrollBoxAsync(string selector, int deltaY)
    {
        var box = Page.Locator(selector);
        await box.ScrollIntoViewIfNeededAsync();
        var rect = await box.BoundingBoxAsync();
        var before = await box.EvaluateAsync<double>("el => el.scrollTop");
        await Page.Mouse.MoveAsync(rect!.X + rect.Width / 2, rect.Y + rect.Height - 20);
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
