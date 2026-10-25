namespace Bit.BlazorUI.Tests.E2E.Layouts;

/// <summary>
/// BitLayout in a real browser, read off the real layout of 800px layouts: the landmarks of its sections, the sections
/// left out of the markup, NavPanel / Aside with their widths and ReverseNavPanel, the four Hide parameters toggled at
/// runtime, HeaderHeight / FooterHeight / Gap / Padding, Bordered (and its dividers following ReverseNavPanel and RTL),
/// FullHeightPanels, the four Sticky parameters and ZIndex in a scrolling box, FullHeight, ScrollableMain, the Hide
/// parameters driven by BitMediaQuery at three viewport widths, Nested and its named region, the skip link (first tab
/// stop, shown on focus, moving the focus to the main section without leaving the page), NavPanelAriaLabel /
/// AsideAriaLabel, the BitParams cascade, Style / Class / Styles / Classes for every part, Visibility, Disabled (a layout
/// host keeping its content live), the --bit-Layout-* variables and the parameters winning over them, RTL, prerendering
/// and unmounting.
/// </summary>
[TestClass]
public class BitLayoutE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    [TestMethod]
    public async Task Prerender_TheLayoutIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/layout");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"landmarks\"");
        StringAssert.Contains(html, "bit-lyt-mcn");
        StringAssert.Contains(html, "<main id=\"landmarks-main\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_TheSectionsAreTheBannerMainAndContentinfoLandmarks()
    {
        await OpenAsync("/e2e/layout");

        await Expect(Page.Locator("#landmarks")).ToHaveClassAsync(HasClass("bit-lyt"));
        await Expect(Page.GetByRole(AriaRole.Banner)).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Banner)).ToHaveClassAsync(HasClass("bit-lyt-hdr"));
        await Expect(Page.GetByRole(AriaRole.Contentinfo)).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Contentinfo)).ToHaveClassAsync(HasClass("bit-lyt-ftr"));
        await Expect(Page.GetByRole(AriaRole.Main).Filter(new() { Has = Page.Locator("#landmarks-main-content") })).ToHaveCountAsync(1);
        Assert.AreEqual("MAIN", await Page.Locator("#landmarks-main").EvaluateAsync<string>("el => el.tagName"));

        // Header, middle row, footer, top to bottom.
        var header = await BoxAsync("#landmarks > .bit-lyt-hdr");
        var middle = await BoxAsync("#landmarks > .bit-lyt-man");
        var footer = await BoxAsync("#landmarks > .bit-lyt-ftr");
        AssertClose(header.Bottom, middle.Y, "the middle row under the header");
        AssertClose(middle.Bottom, footer.Y, "the footer under the middle row");
        AssertClose(800, header.W, "full width");
    }

    [TestMethod]
    public async Task Basic_ASectionWithoutContent_IsLeftOutOfTheMarkup()
    {
        await OpenAsync("/e2e/layout");

        var layout = Page.Locator("#main-only");
        await Expect(layout.Locator("> header")).ToHaveCountAsync(0);
        await Expect(layout.Locator("> footer")).ToHaveCountAsync(0);
        await Expect(layout.Locator(".bit-lyt-nmn")).ToHaveCountAsync(0);
        await Expect(layout.Locator(".bit-lyt-asd")).ToHaveCountAsync(0);
        await Expect(layout.Locator("main.bit-lyt-mcn")).ToHaveCountAsync(1);
        await Expect(layout).ToHaveClassAsync(HasClass("bit-lyt-nhd"));
        await Expect(layout).ToHaveClassAsync(HasClass("bit-lyt-nft"));
    }

    [TestMethod]
    public async Task Panels_SitOnEitherSideOfTheMainContent_AndReverseNavPanelSwapsThem()
    {
        await OpenAsync("/e2e/layout");

        var layout = await BoxAsync("#panels");
        Assert.AreEqual("NAV", await Page.Locator("#panels .bit-lyt-nmn").EvaluateAsync<string>("el => el.tagName"));
        Assert.AreEqual("ASIDE", await Page.Locator("#panels .bit-lyt-asd").EvaluateAsync<string>("el => el.tagName"));

        var nav = await BoxAsync("#panels .bit-lyt-nmn");
        var main = await BoxAsync("#panels-main");
        var aside = await BoxAsync("#panels .bit-lyt-asd");
        AssertClose(layout.X, nav.X, "the nav panel at the start");
        AssertClose(120, nav.W, "the nav panel sized by its content");
        AssertClose(800 - 220, main.W, "the main section leaves the reserved widths");
        AssertClose(nav.Right, main.X, "main after the nav panel");
        AssertClose(layout.Right, aside.Right, "the aside at the end");

        await Page.ClickAsync("#btn-reverse");
        await Expect(Page.Locator("#reverse-state")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#panels")).ToHaveClassAsync(HasClass("bit-lyt-rnv"));
        AssertClose(layout.Right, (await BoxAsync("#panels .bit-lyt-nmn")).Right, "reversed: the nav panel at the end");
        AssertClose(layout.X, (await BoxAsync("#panels .bit-lyt-asd")).X, "reversed: the aside at the start");

        // The reading order is unchanged.
        var order = await Page.Locator("#panels > .bit-lyt-man > *").EvaluateAllAsync<string[]>("els => els.map(e => e.tagName)");
        CollectionAssert.AreEqual(new[] { "NAV", "MAIN", "ASIDE" }, order);
    }

    [TestMethod]
    public async Task Hiding_EverySection_IsTakenOutAndTheRestTakeItsRoom()
    {
        await OpenAsync("/e2e/layout");

        var layout = Page.Locator("#hiding");
        await Expect(layout.Locator("> header")).ToHaveCountAsync(1);
        AssertClose(800 - 220, (await BoxAsync("#hiding-main")).W, "all panels shown");

        await Page.ClickAsync("#btn-hide-header");
        await Expect(Page.Locator("#hide-state")).ToContainTextAsync("h=True");
        await Expect(layout.Locator("> header")).ToHaveCountAsync(0);
        await Expect(layout).ToHaveClassAsync(HasClass("bit-lyt-nhd"));

        await Page.ClickAsync("#btn-hide-nav");
        await Expect(layout.Locator(".bit-lyt-nmn")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hiding-main")).ToHaveCSSAsync("width", "700px");

        await Page.ClickAsync("#btn-hide-aside");
        await Expect(layout.Locator(".bit-lyt-asd")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hiding-main")).ToHaveCSSAsync("width", "800px");

        await Page.ClickAsync("#btn-hide-footer");
        await Expect(layout.Locator("> footer")).ToHaveCountAsync(0);
        await Expect(layout).ToHaveClassAsync(HasClass("bit-lyt-nft"));

        // The main section is always rendered, and everything comes back.
        await Expect(Page.Locator("#hiding-main")).ToHaveCountAsync(1);
        foreach (var id in new[] { "#btn-hide-header", "#btn-hide-nav", "#btn-hide-aside", "#btn-hide-footer" }) await Page.ClickAsync(id);
        await Expect(Page.Locator("#hide-state")).ToHaveTextAsync("h=False;n=False;a=False;f=False");
        await Expect(layout.Locator("> header")).ToHaveCountAsync(1);
        await Expect(layout.Locator("> footer")).ToHaveCountAsync(1);
        await Expect(layout).Not.ToHaveClassAsync(HasClass("bit-lyt-nhd"));
    }

    [TestMethod]
    public async Task Sizing_HeaderHeightFooterHeightGapAndPadding()
    {
        await OpenAsync("/e2e/layout");

        AssertClose(64, (await BoxAsync("#sizing > .bit-lyt-hdr")).H, "HeaderHeight");
        AssertClose(40, (await BoxAsync("#sizing > .bit-lyt-ftr")).H, "FooterHeight");

        var nav = await BoxAsync("#sizing .bit-lyt-nmn");
        var main = await BoxAsync("#sizing-main");
        var aside = await BoxAsync("#sizing .bit-lyt-asd");
        AssertClose(16, main.X - nav.Right, "Gap 1rem after the nav panel");
        AssertClose(16, aside.X - main.Right, "Gap 1rem before the aside");
        AssertClose((await BoxAsync("#sizing")).Right, aside.Right, "the row still fits");

        await Expect(Page.Locator("#sizing-main")).ToHaveCSSAsync("padding-top", "12px");
        AssertClose(12, (await BoxAsync("#sizing-main-content")).X - main.X, "Padding insets the main content");
    }

    [TestMethod]
    public async Task Bordered_DrawsTheDividersBetweenTheSections_FollowingReverseNavPanel()
    {
        await OpenAsync("/e2e/layout");

        var color = await ResolveAsync("color", "var(--bit-clr-brd-pri)");
        await Expect(Page.Locator("#bordered")).ToHaveClassAsync(HasClass("bit-lyt-brd"));
        await Expect(Page.Locator("#bordered > .bit-lyt-hdr")).ToHaveCSSAsync("border-bottom-width", "1px");
        await Expect(Page.Locator("#bordered > .bit-lyt-hdr")).ToHaveCSSAsync("border-bottom-color", color);
        await Expect(Page.Locator("#bordered > .bit-lyt-ftr")).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(Page.Locator("#bordered .bit-lyt-nmn")).ToHaveCSSAsync("border-right-width", "1px");
        await Expect(Page.Locator("#bordered .bit-lyt-nmn")).ToHaveCSSAsync("border-left-width", "0px");
        await Expect(Page.Locator("#bordered .bit-lyt-asd")).ToHaveCSSAsync("border-left-width", "1px");
        await Expect(Page.Locator("#bordered .bit-lyt-asd")).ToHaveCSSAsync("border-right-width", "0px");

        // Reversed, the dividers stay between a panel and the content.
        await Expect(Page.Locator("#bordered-reverse .bit-lyt-nmn")).ToHaveCSSAsync("border-left-width", "1px");
        await Expect(Page.Locator("#bordered-reverse .bit-lyt-nmn")).ToHaveCSSAsync("border-right-width", "0px");
        await Expect(Page.Locator("#bordered-reverse .bit-lyt-asd")).ToHaveCSSAsync("border-right-width", "1px");
        await Expect(Page.Locator("#bordered-reverse .bit-lyt-asd")).ToHaveCSSAsync("border-left-width", "0px");

        // No border on a plain layout.
        await Expect(Page.Locator("#panels .bit-lyt-nmn")).ToHaveCSSAsync("border-right-width", "0px");
    }

    [TestMethod]
    public async Task FullHeightPanels_ThePanelsRunTheFullHeight_AndTheBarsSitBetweenThem()
    {
        await OpenAsync("/e2e/layout");

        var layout = await BoxAsync("#fhp");
        await Expect(Page.Locator("#fhp")).ToHaveClassAsync(HasClass("bit-lyt-fhp"));
        await Expect(Page.Locator("#fhp")).ToHaveCSSAsync("display", "grid");

        var nav = await BoxAsync("#fhp .bit-lyt-nmn");
        var aside = await BoxAsync("#fhp .bit-lyt-asd");
        var header = await BoxAsync("#fhp > .bit-lyt-hdr");
        var main = await BoxAsync("#fhp-main");
        var footer = await BoxAsync("#fhp > .bit-lyt-ftr");

        AssertClose(layout.Y, nav.Y, "the nav panel starts at the top");
        AssertClose(layout.Bottom, nav.Bottom, "and ends at the bottom");
        AssertClose(150, nav.W, "NavPanelWidth is the panel's own width");
        AssertClose(110, aside.W, "AsideWidth is the panel's own width");
        AssertClose(layout.Y, aside.Y, "the aside too");
        AssertClose(nav.Right + 10, header.X, "the header between the panels, a Gap away");
        AssertClose(aside.X - 10, header.Right, "up to the aside");
        AssertClose(header.Bottom + 10, main.Y, "the Gap also separates the header");
        AssertClose(main.Bottom + 10, footer.Y, "and the footer");

        // Off again: a web page, the panels in the middle row.
        await Page.ClickAsync("#btn-fhp");
        await Expect(Page.Locator("#fhp-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#fhp")).ToHaveCSSAsync("display", "flex");
        var navAfter = await BoxAsync("#fhp .bit-lyt-nmn");
        AssertClose((await BoxAsync("#fhp > .bit-lyt-hdr")).Bottom, navAfter.Y, "the nav panel under the header");
        AssertClose(800, (await BoxAsync("#fhp > .bit-lyt-hdr")).W, "the header full width");
    }

    [TestMethod]
    public async Task Sticky_EverySection_PinsWhileItsScrollingAncestorScrolls()
    {
        await OpenAsync("/e2e/layout");

        var box = Page.Locator("#sticky-box");
        await box.ScrollIntoViewIfNeededAsync();

        // Nothing pinned: the header scrolls away.
        await box.EvaluateAsync("el => el.scrollTop = 300");
        Assert.IsTrue((await BoxAsync("#sticky > .bit-lyt-hdr")).Bottom < (await BoxAsync("#sticky-box")).Y, "the header scrolled away");

        foreach (var id in new[] { "#btn-sticky-header", "#btn-sticky-footer", "#btn-sticky-nav", "#btn-sticky-aside" }) await Page.ClickAsync(id);
        await Expect(Page.Locator("#sticky-state")).ToHaveTextAsync("h=True;f=True;n=True;a=True");
        await Expect(Page.Locator("#sticky > .bit-lyt-hdr")).ToHaveCSSAsync("position", "sticky");

        await box.EvaluateAsync("el => el.scrollTop = 400");
        await NextFramesAsync();
        var viewport = await BoxAsync("#sticky-box");
        var header = await BoxAsync("#sticky > .bit-lyt-hdr");
        AssertClose(viewport.Y + 1, header.Y, "the header pinned at the top");
        AssertClose(viewport.Bottom - 1, (await BoxAsync("#sticky > .bit-lyt-ftr")).Bottom, "the footer pinned at the bottom");
        AssertClose(header.Bottom, (await BoxAsync("#sticky .bit-lyt-nmn")).Y, "the nav panel pinned under the header");
        AssertClose(header.Bottom, (await BoxAsync("#sticky .bit-lyt-asd")).Y, "the aside pinned under the header");

        // A pinned bar gets the page background, and ZIndex stacks the pinned sections.
        await Expect(Page.Locator("#sticky > .bit-lyt-hdr")).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-pri)"));
        await Expect(Page.Locator("#sticky > .bit-lyt-hdr")).ToHaveCSSAsync("z-index", "6");
        await Expect(Page.Locator("#sticky .bit-lyt-nmn")).ToHaveCSSAsync("z-index", "5");
    }

    [TestMethod]
    public async Task FullHeight_MakesTheLayoutAtLeastAsTallAsTheViewport()
    {
        await OpenAsync("/e2e/layout");

        var before = await BoxAsync("#full-height");
        Assert.IsTrue(before.H < 200, "as tall as its content by default");

        await Page.ClickAsync("#btn-full-height");
        await Expect(Page.Locator("#full-height")).ToHaveClassAsync(HasClass("bit-lyt-flh"));
        var viewport = Page.ViewportSize!;
        await Expect(Page.Locator("#full-height")).ToHaveCSSAsync("height", $"{viewport.Height}px");
        var layout = await BoxAsync("#full-height");
        AssertClose(layout.Bottom, (await BoxAsync("#full-height > .bit-lyt-ftr")).Bottom, "the footer at the bottom");
    }

    [TestMethod]
    public async Task ScrollableMain_EachSectionOfTheMiddleRowScrollsOnItsOwn()
    {
        await OpenAsync("/e2e/layout");

        var layout = Page.Locator("#scrollable");
        await Expect(layout).ToHaveClassAsync(HasClass("bit-lyt-scm"));
        AssertClose(300, (await BoxAsync("#scrollable")).H, "fills the definite height of its parent");
        await Expect(Page.Locator("#scrollable-main")).ToHaveCSSAsync("overflow-y", "auto");

        var header = await BoxAsync("#scrollable > .bit-lyt-hdr");
        await Page.Locator("#scrollable-main").EvaluateAsync("el => el.scrollTop = 200");
        await Expect(Page.Locator("#scrollable-main")).ToHaveJSPropertyAsync("scrollTop", 200);
        AssertClose(header.Y, (await BoxAsync("#scrollable > .bit-lyt-hdr")).Y, "the header stays put");
        await Expect(Page.Locator("#scrollable .bit-lyt-nmn")).ToHaveJSPropertyAsync("scrollTop", 0);

        await Page.Locator("#scrollable .bit-lyt-nmn").EvaluateAsync("el => el.scrollTop = 100");
        await Expect(Page.Locator("#scrollable .bit-lyt-nmn")).ToHaveJSPropertyAsync("scrollTop", 100);
        await Expect(Page.Locator("#scrollable-main")).ToHaveJSPropertyAsync("scrollTop", 200);

        await Page.ClickAsync("#btn-scrollable");
        await Expect(layout).Not.ToHaveClassAsync(HasClass("bit-lyt-scm"));
        Assert.IsTrue((await BoxAsync("#scrollable")).H > 1000, "without it the layout is as tall as its content");
    }

    [TestMethod]
    public async Task Responsive_TheHideParameters_FollowABitMediaQuery()
    {
        await OpenAsync("/e2e/layout");

        var layout = Page.Locator("#responsive");

        await Page.SetViewportSizeAsync(500, 800);
        await Expect(Page.Locator("#responsive-state")).ToHaveTextAsync("ltMd=True;ltSm=True");
        await Expect(layout.Locator(".bit-lyt-nmn")).ToHaveCountAsync(0);
        await Expect(layout.Locator(".bit-lyt-asd")).ToHaveCountAsync(0);

        await Page.SetViewportSizeAsync(800, 800);
        await Expect(Page.Locator("#responsive-state")).ToHaveTextAsync("ltMd=True;ltSm=False");
        await Expect(layout.Locator(".bit-lyt-nmn")).ToHaveCountAsync(1);
        await Expect(layout.Locator(".bit-lyt-asd")).ToHaveCountAsync(0);

        await Page.SetViewportSizeAsync(1280, 800);
        await Expect(Page.Locator("#responsive-state")).ToHaveTextAsync("ltMd=False;ltSm=False");
        await Expect(layout.Locator(".bit-lyt-asd")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Nested_TheInnerMainIsAPlainDiv_AndALabelMakesARegion()
    {
        await OpenAsync("/e2e/layout");

        Assert.AreEqual("MAIN", await Page.Locator("#outer-main").EvaluateAsync<string>("el => el.tagName"));
        var inner = Page.Locator("#inner-main");
        Assert.AreEqual("DIV", await inner.EvaluateAsync<string>("el => el.tagName"));
        await Expect(inner).ToHaveClassAsync(HasClass("bit-lyt-mcn"));
        await Expect(inner).ToHaveCSSAsync("padding-top", "8px");

        await Expect(Page.Locator("#inner")).ToHaveAttributeAsync("role", "region");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Workspace" })).ToHaveCountAsync(1);
        Assert.IsNull(await Page.Locator("#inner-unnamed").GetAttributeAsync("role"));
        Assert.IsNull(await Page.Locator("#outer").GetAttributeAsync("role"));

        // The inner layout lays itself out inside the outer main section.
        var outerMain = await BoxAsync("#outer-main");
        var innerNav = await BoxAsync("#inner .bit-lyt-nmn");
        AssertClose(outerMain.X, innerNav.X, "the inner nav panel at the start of the outer main");
    }

    [TestMethod]
    public async Task SkipLink_IsTheFirstStop_ShownOnFocus_AndMovesTheFocusToTheMainSection()
    {
        await OpenAsync("/e2e/layout");

        var skip = Page.Locator("#skip .bit-lyt-skp");
        await Expect(skip).ToHaveTextAsync("Skip to main content");
        await Expect(skip).ToHaveAttributeAsync("href", $"{BaseUrl}/e2e/layout#skip-main");
        await Expect(skip).ToHaveCSSAsync("opacity", "0");
        await Expect(skip).ToHaveClassAsync(HasClass("lyt-skip-class"));
        await Expect(skip).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator("#skip-main")).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Locator("#btn-before-skip").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(skip).ToBeFocusedAsync();
        await Expect(skip).ToHaveCSSAsync("opacity", "1");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#skip-header-button")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(skip).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#skip-main")).ToBeFocusedAsync();
        await Expect(Page.Locator("#skip-main")).ToBeInViewportAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/e2e/layout$"));

        // The next Tab carries on from the content.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#skip-main-button")).ToBeFocusedAsync();

        await Expect(Page.Locator("#skip-text .bit-lyt-skp")).ToHaveTextAsync("Skip to the content");
        await Expect(Page.Locator("#panels .bit-lyt-skp")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Accessibility_ThePanelsAreNamedLandmarks()
    {
        await OpenAsync("/e2e/layout");

        await Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = "Sections" })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Complementary, new() { Name = "On this page" })).ToHaveCountAsync(1);
        Assert.IsNull(await Page.Locator("#panels .bit-lyt-nmn").GetAttributeAsync("aria-label"));
    }

    [TestMethod]
    public async Task CascadingParameters_BitLayoutParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/layout");

        await Expect(Page.Locator("#casc")).ToHaveClassAsync(HasClass("bit-lyt-brd"));
        await Expect(Page.Locator("#casc .bit-lyt-nmn")).ToHaveCSSAsync("border-right-width", "1px");
        await Expect(Page.Locator("#casc .bit-lyt-nmn")).ToHaveAttributeAsync("aria-label", "Section");
        AssertClose(16, (await BoxAsync("#casc-main")).X - (await BoxAsync("#casc .bit-lyt-nmn")).Right, "the cascaded Gap");
        AssertClose(8, (await BoxAsync("#casc-main-content")).X - (await BoxAsync("#casc-main")).X, "the cascaded Padding");

        await Expect(Page.Locator("#casc-own")).Not.ToHaveClassAsync(HasClass("bit-lyt-brd"));
        AssertClose(0, (await BoxAsync("#casc-own-main")).X - (await BoxAsync("#casc-own .bit-lyt-nmn")).Right, "its own Gap 0");
        await Expect(Page.Locator("#casc-own .bit-lyt-nmn")).ToHaveAttributeAsync("aria-label", "Section");

        await Expect(Page.Locator("#casc-out")).Not.ToHaveClassAsync(HasClass("bit-lyt-brd"));
        Assert.IsNull(await Page.Locator("#casc-out .bit-lyt-nmn").GetAttributeAsync("aria-label"));
    }

    [TestMethod]
    public async Task StyleClassStylesClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync("/e2e/layout");

        var root = Page.Locator("#styled");
        await Expect(root).ToHaveClassAsync(HasClass("lyt-root-class"));
        await Expect(root).ToHaveClassAsync(HasClass("lyt-extra-root"));
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "8px");
        await Expect(root).ToHaveCSSAsync("padding-top", "2px");

        var parts = new (string Selector, string Class, string Property, string Value)[]
        {
            ("#styled > .bit-lyt-hdr", "lyt-header-class", "padding-top", "3px"),
            ("#styled > .bit-lyt-man", "lyt-main-class", "min-height", "150px"),
            ("#styled .bit-lyt-nmn", "lyt-nav-class", "padding-top", "4px"),
            ("#styled-main", "lyt-content-class", "padding-top", "5px"),
            ("#styled .bit-lyt-asd", "lyt-aside-class", "padding-top", "6px"),
            ("#styled > .bit-lyt-ftr", "lyt-footer-class", "padding-top", "7px"),
            ("#styled > .bit-lyt-skp", "lyt-skip-class", "margin-top", "1px"),
        };

        foreach (var (selector, cls, property, value) in parts)
        {
            await Expect(Page.Locator(selector)).ToHaveClassAsync(HasClass(cls));
            await Expect(Page.Locator(selector)).ToHaveCSSAsync(property, value);
        }

        // Styles.MainContent is appended to the width the panels leave.
        await Expect(Page.Locator("#styled-main")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("width:calc\\(100% - 220px\\);padding: 5px"));
    }

    [TestMethod]
    public async Task VisibilityAndDisabled_TheLayoutHidesAndDims_AndItsContentStaysLive()
    {
        await OpenAsync("/e2e/layout");

        await Page.ClickAsync("#btn-hidden-vis");
        await Expect(Page.Locator("#vis")).ToHaveCSSAsync("visibility", "hidden");
        await Page.ClickAsync("#btn-collapsed-vis");
        await Expect(Page.Locator("#vis")).ToHaveCSSAsync("display", "none");

        var disabled = Page.Locator("#disabled");
        await Expect(disabled).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(disabled).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));

        // A layout hosts content: what bubbles up to it is an enabled control's own event.
        await Page.ClickAsync("#disabled-inner");
        await Expect(Page.Locator("#inner-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#root-clicks")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(disabled).Not.ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task CssVariables_RestyleEverySection_AndTheParametersWinOverThem()
    {
        await OpenAsync("/e2e/layout");

        await Expect(Page.Locator("#vars")).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#vars")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");

        var header = Page.Locator("#vars > .bit-lyt-hdr");
        await Expect(header).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(header).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(header).ToHaveCSSAsync("box-shadow", "rgb(255, 0, 0) 0px 2px 4px 0px");
        await Expect(header).ToHaveCSSAsync("height", "48px");
        await Expect(header).ToHaveCSSAsync("z-index", "21");
        await Expect(header).ToHaveCSSAsync("border-bottom-width", "3px");
        await Expect(header).ToHaveCSSAsync("border-bottom-color", "rgb(0, 255, 0)");

        var footer = Page.Locator("#vars > .bit-lyt-ftr");
        await Expect(footer).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(footer).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(footer).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 255) 0px -2px 4px 0px");
        await Expect(footer).ToHaveCSSAsync("height", "36px");

        await Expect(Page.Locator("#vars .bit-lyt-nmn")).ToHaveCSSAsync("background-color", "rgb(19, 20, 21)");
        await Expect(Page.Locator("#vars .bit-lyt-nmn")).ToHaveCSSAsync("color", "rgb(22, 23, 24)");
        await Expect(Page.Locator("#vars .bit-lyt-asd")).ToHaveCSSAsync("background-color", "rgb(25, 26, 27)");
        await Expect(Page.Locator("#vars .bit-lyt-asd")).ToHaveCSSAsync("color", "rgb(28, 29, 30)");
        await Expect(Page.Locator("#vars-main")).ToHaveCSSAsync("background-color", "rgb(31, 32, 33)");
        await Expect(Page.Locator("#vars-main")).ToHaveCSSAsync("color", "rgb(34, 35, 36)");
        await Expect(Page.Locator("#vars-main")).ToHaveCSSAsync("padding-top", "11px");
        AssertClose(9, (await BoxAsync("#vars-main")).X - (await BoxAsync("#vars .bit-lyt-nmn")).Right, "--bit-Layout-gap");

        // A parameter wins over its variable.
        await Expect(Page.Locator("#vars-params > .bit-lyt-hdr")).ToHaveCSSAsync("height", "60px");
        await Expect(Page.Locator("#vars-params > .bit-lyt-hdr")).ToHaveCSSAsync("z-index", "2");
        await Expect(Page.Locator("#vars-params-main")).ToHaveCSSAsync("padding-top", "3px");
        AssertClose(2, (await BoxAsync("#vars-params-main")).X - (await BoxAsync("#vars-params .bit-lyt-nmn")).Right, "the Gap parameter");
    }

    [TestMethod]
    public async Task Rtl_ThePanelsAndTheDividersMirror()
    {
        await OpenAsync("/e2e/layout");

        var layout = Page.Locator("#rtl");
        await Expect(layout).ToHaveAttributeAsync("dir", "rtl");
        await Expect(layout).ToHaveClassAsync(HasClass("bit-rtl"));
        var box = await BoxAsync("#rtl");
        AssertClose(box.Right, (await BoxAsync("#rtl .bit-lyt-nmn")).Right, "the nav panel at the right");
        AssertClose(box.X, (await BoxAsync("#rtl .bit-lyt-asd")).X, "the aside at the left");
        await Expect(Page.Locator("#rtl .bit-lyt-nmn")).ToHaveCSSAsync("border-left-width", "1px");
        await Expect(Page.Locator("#rtl .bit-lyt-asd")).ToHaveCSSAsync("border-right-width", "1px");

        Assert.IsNull(await Page.Locator("#rtl-inherited").GetAttributeAsync("dir"));
        AssertClose((await BoxAsync("#rtl-inherited")).Right, (await BoxAsync("#rtl-inherited .bit-lyt-nmn")).Right, "inherited: the nav panel at the right");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAndMountingAgain_RaisesNoError()
    {
        await OpenAsync("/e2e/layout");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-layout")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#mount-layout-main")).ToHaveTextAsync("mounted");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
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
