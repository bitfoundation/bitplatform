using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitBreadcrumb (/e2e/breadcrumb) in a real browser, in its three APIs (BitBreadcrumbItem, a custom
/// class through NameSelectors, BitBreadcrumbOption markup): the prerendered landmark, links, buttons and text steps,
/// IsSelected and SelectedItemAsText, OnItemClick and the items' own OnClick, the new-tab hint, the overflow menu that
/// MaxDisplayedItems and OverflowIndex make (opened, positioned, walked with the keyboard, closed), ExpandOverflow,
/// AutoCollapse following the width of its container, MaxItemWidth, Wrap and Scrollable, icons, IconPlacement, the
/// dividers and every template, dynamic items, options inserted, renamed and reordered (AutoReorderOptions), the JSON-LD
/// StructuredData, the disabled contract, every Color and Size, the external icons, Style/Class/Styles/Classes,
/// Visibility, HtmlAttributes, the BitParams cascade, the public css variables, RTL, Escape inside a dialog and an
/// unmount while the menu is open.
/// </summary>
[TestClass]
public class BitBreadcrumbE2ETests : E2ETestBase
{
    private const string Url = "/e2e/breadcrumb";
    private const string More = "[more]";

    // The class suffix (BitBreadcrumb.razor.cs GetColorClass) and the main token of each color.
    private static readonly (string Color, string Suffix, string Main)[] Colors =
    [
        ("Primary", "pri", "--bit-clr-pri"), ("Secondary", "sec", "--bit-clr-sec"), ("Tertiary", "ter", "--bit-clr-ter"),
        ("Info", "inf", "--bit-clr-inf"), ("Success", "suc", "--bit-clr-suc"), ("Warning", "wrn", "--bit-clr-wrn"),
        ("SevereWarning", "swr", "--bit-clr-swr"), ("Error", "err", "--bit-clr-err"),
        ("PrimaryBackground", "pbg", "--bit-clr-bg-pri"), ("SecondaryBackground", "sbg", "--bit-clr-bg-sec"),
        ("TertiaryBackground", "tbg", "--bit-clr-bg-ter"), ("PrimaryForeground", "pfg", "--bit-clr-fg-pri"),
        ("SecondaryForeground", "sfg", "--bit-clr-fg-sec"), ("TertiaryForeground", "tfg", "--bit-clr-fg-ter"),
        ("PrimaryBorder", "pbr", "--bit-clr-brd-pri"), ("SecondaryBorder", "sbr", "--bit-clr-brd-sec"),
        ("TertiaryBorder", "tbr", "--bit-clr-brd-ter")
    ];

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheTrail()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"bc-basic\"");
        StringAssert.Contains(html, "aria-label=\"Breadcrumb\"");
        StringAssert.Contains(html, "bit-brc-icn");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Url);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Basic_ALandmarkHoldingAListOfLinks_WithTheCurrentPageMarked()
    {
        await OpenAsync(Url);

        var root = Root("bc-basic");
        Assert.AreEqual("NAV", await root.EvaluateAsync<string>("el => el.tagName"));
        await Expect(root).ToHaveAttributeAsync("aria-label", "Breadcrumb");
        await Expect(root.Locator("ol")).ToHaveAttributeAsync("role", "list");
        await Expect(root.Locator("ol > li[role='listitem']")).ToHaveCountAsync(4);
        await Expect(root.Locator("ol > li.bit-brc-dvw[aria-hidden='true']")).ToHaveCountAsync(3);
        await ExpectTrailAsync("bc-basic", "Item 1|Item 2|Item 3|Item 4");

        await Expect(Step("bc-basic", "Item 1")).ToHaveAttributeAsync("href", "/e2e/breadcrumb?step=1");
        await Expect(Step("bc-basic", "Item 1")).Not.ToHaveAttributeAsync("aria-current", "page");
        await Expect(Step("bc-basic", "Item 1")).ToHaveCSSAsync("font-weight", "400");

        var current = Step("bc-basic", "Item 4");
        await Expect(current).ToHaveAttributeAsync("aria-current", "page");
        await Expect(current).ToHaveClassAsync(HasClass("bit-brc-sel"));
        await Expect(current).ToHaveCSSAsync("font-weight", "600");

        await Expect(root.Locator("i.bit-brc-div").First).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        Assert.IsTrue(await root.Locator("ol").EvaluateAsync<bool>("ol => ol.getBoundingClientRect().height < 50"), "One line.");
    }

    [TestMethod]
    public async Task Basic_ClickingALink_FiresOnItemClickAndFollowsIt()
    {
        await OpenAsync(Url);

        await Step("bc-basic", "Item 2").ClickAsync();

        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Item 2");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#url")).ToHaveTextAsync("/e2e/breadcrumb?step=2");
    }

    [TestMethod]
    public async Task SelectedItemAsText_RendersTheCurrentPageAsText_AndAnIconOnlyOneIsNamed()
    {
        await OpenAsync(Url);

        var current = Root("bc-selected-text").Locator("[aria-current='page']");
        Assert.AreEqual("SPAN", await current.EvaluateAsync<string>("el => el.tagName"));
        await Expect(current).ToHaveClassAsync(HasClass("bit-brc-nii"));
        await Expect(current).ToHaveTextAsync("Item 4");
        Assert.IsNull(await current.GetAttributeAsync("href"));
        Assert.AreEqual("A", await Step("bc-selected-text", "Item 3").EvaluateAsync<string>("el => el.tagName"));

        // A span takes no aria-label, so the name is written out as visually hidden text.
        var labelled = Root("bc-selected-text-label").Locator("[aria-current='page']");
        await Expect(labelled.Locator(".bit-brc-vhd")).ToHaveTextAsync("Current home");
        await Expect(labelled.Locator(".bit-brc-nic")).ToHaveAttributeAsync("aria-hidden", "true");
        var hidden = await BoxAsync(labelled.Locator(".bit-brc-vhd"));
        Assert.IsTrue(hidden.Width <= 1 && hidden.Height <= 1);
    }

    [TestMethod]
    public async Task Disabled_EveryStepIsUnavailable_ARealClickDoesNothing_AndThePagesHandlersAreDropped()
    {
        await OpenAsync(Url);

        var root = Root("bc-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));

        var item = Step("bc-disabled", "Item 1");
        await Expect(item).ToHaveAttributeAsync("role", "link");
        await Expect(item).ToHaveAttributeAsync("aria-disabled", "true");
        Assert.IsNull(await item.GetAttributeAsync("href"));
        await Expect(item).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-dis)"));

        await Expect(OverflowButton("bc-disabled")).ToBeDisabledAsync();
        await Expect(OverflowButton("bc-disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        var box = await BoxAsync(item);
        await Page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        await root.DispatchEventAsync("dblclick");
        await root.DispatchEventAsync("pointerdown");
        await root.DispatchEventAsync("mouseover");

        await Expect(Page.Locator("#disabled-hovers")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-activations")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Step("bc-disabled", "Item 1")).ToHaveAttributeAsync("href", "/e2e/breadcrumb?step=1");
        await Expect(OverflowButton("bc-disabled")).ToBeEnabledAsync();

        await root.DispatchEventAsync("dblclick");
        await Expect(Page.Locator("#disabled-activations")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task IsDisabled_ADisabledLinkStaysALink_AndADisabledButtonIsNative()
    {
        await OpenAsync(Url);

        var disabled = Step("bc-item-disabled", "Item 1");
        await Expect(disabled).ToHaveAttributeAsync("role", "link");
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).ToHaveClassAsync(HasClass("bit-brc-dis"));
        Assert.IsNull(await disabled.GetAttributeAsync("href"));
        await Expect(Root("bc-item-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await disabled.ClickAsync(new() { Force = true });
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("0");

        var button = Step("bc-button-disabled", "Button 1");
        Assert.AreEqual("BUTTON", await button.EvaluateAsync<string>("el => el.tagName"));
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");

        await Step("bc-button-disabled", "Button 2").ClickAsync();
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Button 2");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Target_ANewTabLinkSaysSo_AndNewTabHintAndNoNewTabHintChangeTheSentence()
    {
        await OpenAsync(Url);

        var site = Step("bc-target", "Site");
        await Expect(site).ToHaveAttributeAsync("target", "_blank");
        await Expect(site).ToHaveAttributeAsync("rel", new Regex("noopener"));
        await Expect(site).ToHaveAttributeAsync("title", "Opens in a new tab");
        await Expect(site.Locator(".bit-brc-vhd")).ToHaveTextAsync("(opens in a new tab)");
        // An aria-label replaces the content, so the sentence joins the label instead.
        await Expect(Step("bc-target", "Labelled")).ToHaveAttributeAsync("aria-label", "Labelled site (opens in a new tab)");
        await Expect(Step("bc-target", "Item 4")).ToHaveAttributeAsync("rel", new Regex("nofollow"));

        await Expect(Step("bc-target-hint", "Site").Locator(".bit-brc-vhd")).ToHaveTextAsync("(new window)");
        await Expect(Step("bc-target-hint", "Labelled")).ToHaveAttributeAsync("aria-label", "Labelled site (new window)");

        await Expect(Step("bc-target-no-hint", "Site").Locator(".bit-brc-vhd")).ToHaveCountAsync(0);
        await Expect(Step("bc-target-no-hint", "Labelled")).ToHaveAttributeAsync("aria-label", "Labelled site");
    }

    [TestMethod]
    public async Task MaxDisplayedItems_CollapsesTheRestIntoAPositionedMenu_ThatAClickInsideCloses()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-max-2", $"{More}|Item 3|Item 4");
        await CenterAsync("#bc-max-2");

        var button = OverflowButton("bc-max-2");
        var calloutId = await CalloutIdAsync("bc-max-2");
        await Expect(button).ToHaveAttributeAsync("aria-haspopup", "menu");
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(button).ToHaveAttributeAsync("aria-controls", calloutId);
        await Expect(button).ToHaveAttributeAsync("aria-label", "More items");
        await Expect(button).ToHaveAttributeAsync("title", "More items");
        await Expect(button.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--More"));

        var callout = Page.Locator($"[id='{calloutId}']");
        await Expect(callout).ToBeHiddenAsync();

        await button.ClickAsync();

        await Expect(callout).ToBeVisibleAsync();
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(callout.Locator("ul")).ToHaveAttributeAsync("role", "menu");
        await Expect(callout.Locator("ul")).ToHaveAttributeAsync("aria-labelledby", (await button.GetAttributeAsync("id"))!);
        var items = callout.Locator("[role='menuitem']");
        Assert.AreEqual("Item 1|Item 2", string.Join('|', (await items.AllTextContentsAsync()).Select(t => t.Trim())));
        await Expect(items.First).ToHaveAttributeAsync("href", "/e2e/breadcrumb?step=1");
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.closest('#overflow') === null"), "Relocated to the body.");

        // Once its entry slide is over, the menu sits right under its button.
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        await Expect(callout).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var anchor = await BoxAsync(button);
        var menu = await BoxAsync(callout);
        Assert.AreEqual(anchor.Y + anchor.Height, menu.Y, 3, "The menu opens right below its button.");
        Assert.AreEqual(anchor.X, menu.X, 2, "The menu is aligned with the start of its button.");

        await items.First.ClickAsync();
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Item 1");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#url")).ToHaveTextAsync("/e2e/breadcrumb?step=1");
    }

    [TestMethod]
    public async Task OverflowMenu_AnOutsideClickClosesIt()
    {
        await OpenAsync(Url);

        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-max-2")}']");
        await OverflowButton("bc-max-2").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();

        await Page.Mouse.ClickAsync(5, 5);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(OverflowButton("bc-max-2")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task OverflowIndex_PlacesTheButton_AndAriaLabelAndOverflowAriaLabelNameThem()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-max-3-idx-1", $"Item 1|{More}|Item 3|Item 4");
        await ExpectTrailAsync("bc-max-3-idx-2", $"Item 1|Item 2|{More}|Item 4");
        // An index past the shown items falls back to the start.
        await ExpectTrailAsync("bc-max-idx-past", $"{More}|Item 3|Item 4");

        await Expect(Root("bc-max-3-idx-2")).ToHaveAttributeAsync("aria-label", "You are here");
        await Expect(OverflowButton("bc-max-3-idx-2")).ToHaveAttributeAsync("aria-label", "Show hidden levels");

        await OverflowButton("bc-max-3-idx-2").ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-max-3-idx-2")}']");
        await Expect(callout.Locator("[role='menuitem']")).ToHaveTextAsync(["Item 3"]);
    }

    [TestMethod]
    public async Task MaxDisplayedItems_ChangedAtRuntime_CollapsesAndRestoresTheTrail()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-max-runtime", "Item 1|Item 2|Item 3|Item 4");
        await Page.Locator("#btn-max-runtime").ClickAsync();
        await ExpectTrailAsync("bc-max-runtime", $"{More}|Item 3|Item 4");
        await Page.Locator("#btn-max-runtime").ClickAsync();
        await ExpectTrailAsync("bc-max-runtime", "Item 1|Item 2|Item 3|Item 4");
    }

    [TestMethod]
    public async Task OverflowMenu_Keyboard_EnterArrowsHomeEndTypeaheadEscapeAndTab()
    {
        await OpenAsync(Url);

        var button = OverflowButton("bc-keyboard");
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-keyboard")}']");
        ILocator Menu(string text) => callout.Locator("[role='menuitem']").Filter(new() { HasText = text });

        await Page.Locator("#before-keyboard").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(button).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Menu("Alpha")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Menu("Bravo")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Menu("Charlie")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Menu("Alpha")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Menu("Charlie")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Menu("Alpha")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("b");
        await Expect(Menu("Bravo")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(button).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Menu("Charlie")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(button).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(" ");
        await Expect(Menu("Alpha")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Alpha");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(button).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Truncation_ByDefaultALongTrailStaysOnOneLineAndOverflows()
    {
        await OpenAsync(Url);

        var ol = Root("bc-long-default").Locator("ol");
        var item = await BoxAsync(Step("bc-long-default", "Very long folder name 1"));
        Assert.IsTrue((await BoxAsync(ol)).Height < item.Height * 1.5, "One line.");
        Assert.IsTrue(await ol.EvaluateAsync<bool>("ol => ol.scrollWidth > 300"), "Wider than its container.");
    }

    [TestMethod]
    public async Task MaxItemWidth_TruncatesEachItem_AndItsTextBecomesTheTooltip()
    {
        await OpenAsync(Url);

        var item = Step("bc-long-max-width", "Very long folder name 1");
        var text = item.Locator(".bit-brc-itx");
        await Expect(text).ToHaveCSSAsync("max-width", "80px");
        await Expect(text).ToHaveCSSAsync("text-overflow", "ellipsis");
        Assert.IsTrue((await BoxAsync(text)).Width <= 80.5);
        Assert.IsTrue(await text.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The text is cut.");
        await Expect(item).ToHaveAttributeAsync("title", "Very long folder name 1");
    }

    [TestMethod]
    public async Task Wrap_FlowsALongTrailOntoMoreLines()
    {
        await OpenAsync(Url);

        var root = Root("bc-long-wrap");
        await Expect(root).ToHaveClassAsync(HasClass("bit-brc-wrp"));
        var item = await BoxAsync(Step("bc-long-wrap", "Very long folder name 1"));
        Assert.IsTrue((await BoxAsync(root.Locator("ol"))).Height > item.Height * 1.8, "More than one line.");

        var box = await BoxAsync(Page.Locator("#box-wrap"));
        var rights = await root.Locator("ol > li").EvaluateAllAsync<double[]>("lis => lis.map(li => li.getBoundingClientRect().right)");
        Assert.IsTrue(rights.All(r => r <= box.X + box.Width + 1), "Every step stays inside the container.");
    }

    [TestMethod]
    public async Task Scrollable_ScrollsALongTrailSideways_OpeningOnItsEnd()
    {
        await OpenAsync(Url);

        var root = Root("bc-long-scroll");
        await Expect(root).ToHaveClassAsync(HasClass("bit-brc-scr"));
        var ol = root.Locator("ol");
        await Expect(ol).ToHaveCSSAsync("overflow-x", "auto");
        Assert.IsTrue(await ol.EvaluateAsync<bool>("ol => ol.scrollWidth > ol.clientWidth"));
        await Page.WaitForFunctionAsync("() => { const ol = document.querySelector('#bc-long-scroll ol'); return ol.scrollLeft > 0 && Math.abs(ol.scrollWidth - ol.clientWidth - ol.scrollLeft) <= 2; }");

        var olBox = await BoxAsync(ol);
        var last = await BoxAsync(Step("bc-long-scroll", "Very long folder name 4"));
        Assert.IsTrue(last.X + last.Width <= olBox.X + olBox.Width + 1, "The current page is in view.");
    }

    [TestMethod]
    public async Task AutoCollapse_NarrowingTheContainerCollapsesItems_AndWideningBringsThemBack()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-auto", "Very long folder name 1|Very long folder name 2|Very long folder name 3|Very long folder name 4");
        await Expect(OverflowButton("bc-auto")).ToHaveCountAsync(0);

        await SetWidthAsync("#box-auto", 450);
        await Expect(OverflowButton("bc-auto")).ToBeVisibleAsync();
        await Page.WaitForFunctionAsync("() => { const ol = document.querySelector('#bc-auto ol'); return ol.scrollWidth <= 451; }");
        var ol = Root("bc-auto").Locator("ol");
        var item = await BoxAsync(Step("bc-auto", "Very long folder name 4"));
        Assert.IsTrue((await BoxAsync(ol)).Height < item.Height * 1.5, "The trail stays on one line.");
        // The overflow button sits at the root (OverflowIndex 0) and the current page stays.
        Assert.IsTrue((await TrailAsync("bc-auto")).StartsWith(More));

        await SetWidthAsync("#box-auto", 1000);
        await ExpectTrailAsync("bc-auto", "Very long folder name 1|Very long folder name 2|Very long folder name 3|Very long folder name 4");
        await Expect(OverflowButton("bc-auto")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AutoCollapse_DownToTheLastItem_TruncatesItsText()
    {
        await OpenAsync(Url);

        await SetWidthAsync("#box-auto", 120);

        await Expect(Root("bc-auto")).ToHaveClassAsync(HasClass("bit-brc-trc"));
        var last = Step("bc-auto", "Very long folder name 4");
        await Expect(last).ToHaveAttributeAsync("title", "Very long folder name 4");
        Assert.IsTrue(await last.Locator(".bit-brc-itx").EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The text is cut with an ellipsis.");
        var box = await BoxAsync(Page.Locator("#box-auto"));
        var lastBox = await BoxAsync(last);
        Assert.IsTrue(lastBox.X + lastBox.Width <= box.X + box.Width + 1, "Nothing runs out of the container.");
    }

    [TestMethod]
    public async Task AutoCollapse_WithOverflowIndexOne_KeepsTheRoot_AndWrapTurnsItOff()
    {
        await OpenAsync(Url);

        await SetWidthAsync("#box-auto-idx", 520);
        await Expect(OverflowButton("bc-auto-idx")).ToBeVisibleAsync();
        await Page.WaitForFunctionAsync($@"() => {{
            const parts = ({TrailScript})(document.getElementById('bc-auto-idx')).split('|');
            return parts[0] === 'Very long folder name 1' && parts[1] === '[more]';
        }}");

        // A trail that may wrap has nothing to collapse.
        await Expect(OverflowButton("bc-auto-wrap")).ToHaveCountAsync(0);
        await Expect(Root("bc-auto-wrap")).ToHaveClassAsync(HasClass("bit-brc-wrp"));
    }

    [TestMethod]
    public async Task ExpandOverflow_TheButtonRevealsTheCollapsedItemsInPlace_AndHandsTheFocusToTheFirst()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-expand", $"Item 1|{More}|Item 5|Item 6");

        var button = OverflowButton("bc-expand");
        Assert.IsNull(await button.GetAttributeAsync("aria-haspopup"));
        Assert.IsNull(await button.GetAttributeAsync("aria-expanded"));
        Assert.IsNull(await button.GetAttributeAsync("aria-controls"));

        await button.ClickAsync();

        await ExpectTrailAsync("bc-expand", "Item 1|Item 2|Item 3|Item 4|Item 5|Item 6");
        await Expect(OverflowButton("bc-expand")).ToHaveCountAsync(0);
        await Expect(Step("bc-expand", "Item 2")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Icons_ItemIconsTheDividerAndTheOverflowGlyph()
    {
        await OpenAsync(Url);

        await Expect(Step("bc-icons", "Item 1").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        await Expect(Step("bc-icons", "Item 1").Locator("i")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Root("bc-icons").Locator("i.bit-brc-div").First).ToHaveClassAsync(HasClass("bit-icon--CaretRightSolid8"));
        await Expect(OverflowButton("bc-icons").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronDown"));

        await OverflowButton("bc-icons").ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-icons")}']");
        await Expect(callout.Locator("[role='menuitem'] i")).ToHaveClassAsync([HasClass("bit-icon--Document")]);
    }

    [TestMethod]
    public async Task IconPlacement_EndPutsTheIconAfterTheText_AndAnItemsOwnPlacementWins()
    {
        await OpenAsync(Url);

        var end = Step("bc-icons-end", "Item 1");
        await Expect(end).ToHaveClassAsync(HasClass("bit-brc-eni"));
        Assert.IsTrue((await BoxAsync(end.Locator("i"))).X > (await BoxAsync(end.Locator(".bit-brc-itx"))).X);

        var own = Step("bc-icons-end", "Item 3");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-brc-eni"));
        Assert.IsTrue((await BoxAsync(own.Locator("i"))).X < (await BoxAsync(own.Locator(".bit-brc-itx"))).X);

        var start = Step("bc-icons", "Item 1");
        Assert.IsTrue((await BoxAsync(start.Locator("i"))).X < (await BoxAsync(start.Locator(".bit-brc-itx"))).X);
    }

    [TestMethod]
    public async Task DividerTextAndAnIconOnlyItem_AreRenderedAndNamed()
    {
        await OpenAsync(Url);

        var root = Root("bc-home");
        await Expect(root.Locator(".bit-brc-dtx")).ToHaveCountAsync(2);
        await Expect(root.Locator(".bit-brc-dtx").First).ToHaveTextAsync("/");
        await Expect(root.Locator("i.bit-brc-div")).ToHaveCountAsync(0);

        var home = root.Locator("a[aria-label='Home']");
        await Expect(home).ToHaveAttributeAsync("href", "/e2e/breadcrumb?step=home");
        await Expect(home.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Home"));
    }

    [TestMethod]
    public async Task Templates_DividerItemOverflowAndOverflowIcon_AndAnItemsOwnTemplatesWin()
    {
        await OpenAsync(Url);

        await Expect(Root("bc-divider-template").Locator(".custom-divider")).ToHaveCountAsync(3);
        await Expect(Root("bc-divider-template").Locator("i.bit-brc-div")).ToHaveCountAsync(0);

        var templated = Root("bc-item-template");
        await Expect(templated.Locator(".tpl-item")).ToHaveTextAsync(["[Item 1]", "[Item 2]", "[Item 4]"]);
        await Expect(OverflowButton("bc-item-template").Locator(".tpl-overflow-icon")).ToHaveTextAsync("+");
        await OverflowButton("bc-item-template").ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-item-template")}']");
        await Expect(callout.Locator(".tpl-overflow")).ToHaveTextAsync("(Item 3)");
        await Expect(OverflowButton("bc-item-template")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        var own = Root("bc-own-template");
        await Expect(own.Locator(".own-tpl")).ToHaveTextAsync("Item 1");
        await Expect(own.Locator(".own-tpl")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(own.Locator(".tpl-item")).ToHaveTextAsync(["[Item 3]", "[Item 4]"]);
        await OverflowButton("bc-own-template").ClickAsync();
        var ownCallout = Page.Locator($"[id='{await CalloutIdAsync("bc-own-template")}']");
        await Expect(ownCallout.Locator(".own-overflow-tpl")).ToHaveTextAsync("~Item 2");
    }

    [TestMethod]
    public async Task DynamicItems_OnItemClickGoesBackUp_AndAddingItemsCollapsesTheOverflow()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-dynamic", "Level 1|Level 2|Level 3|Level 4");
        Assert.AreEqual("SPAN", await Root("bc-dynamic").Locator("[aria-current='page']").EvaluateAsync<string>("el => el.tagName"));

        await Step("bc-dynamic", "Level 2").ClickAsync();
        await Expect(Page.Locator("#dynamic-count")).ToHaveTextAsync("2");
        await ExpectTrailAsync("bc-dynamic", "Level 1|Level 2");
        await Expect(Root("bc-dynamic").Locator("[aria-current='page']")).ToHaveTextAsync("Level 2");

        for (var i = 0; i < 3; i++)
        {
            await Page.Locator("#btn-add-item").ClickAsync();
        }

        await Expect(Page.Locator("#dynamic-count")).ToHaveTextAsync("5");
        await ExpectTrailAsync("bc-dynamic", $"Level 1|{More}|Level 3|Level 4|Level 5");

        await Step("bc-own-click", "Own 1").ClickAsync();
        await Expect(Page.Locator("#own-click")).ToHaveTextAsync("Own 1");
    }

    [TestMethod]
    public async Task StructuredData_WritesTheWholeTrailAsABreadcrumbListWithAbsoluteUrls()
    {
        await OpenAsync(Url);

        var json = await Page.Locator("#structured-box script[type='application/ld+json']").TextContentAsync();
        using var document = JsonDocument.Parse(json!);
        var root = document.RootElement;

        Assert.AreEqual("https://schema.org", root.GetProperty("@context").GetString());
        Assert.AreEqual("BreadcrumbList", root.GetProperty("@type").GetString());

        var items = root.GetProperty("itemListElement").EnumerateArray().ToArray();
        Assert.AreEqual(4, items.Length, "The collapsed items are written too.");
        for (var i = 0; i < items.Length; i++)
        {
            Assert.AreEqual("ListItem", items[i].GetProperty("@type").GetString());
            Assert.AreEqual(i + 1, items[i].GetProperty("position").GetInt32());
            Assert.AreEqual($"Item {i + 1}", items[i].GetProperty("name").GetString());
            Assert.AreEqual($"{BaseUrl}/e2e/breadcrumb?step={i + 1}", items[i].GetProperty("item").GetString());
        }

        await ExpectTrailAsync("bc-structured", $"Item 1|Item 2|{More}|Item 4");
    }

    [TestMethod]
    public async Task BitParams_CascadesItsDefaultsToEveryApi_AndABreadcrumbsOwnParametersWin()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-params", $"Item 1|{More}|Item 3|Item 4");
        await Expect(Root("bc-params").Locator(".bit-brc-dtx").First).ToHaveTextAsync("/");
        Assert.AreEqual("SPAN", await Root("bc-params").Locator("[aria-current='page']").EvaluateAsync<string>("el => el.tagName"));

        await Expect(Root("bc-params-own").Locator(".bit-brc-dtx").First).ToHaveTextAsync("›");
        await ExpectTrailAsync("bc-params-own", $"Item 1|{More}|Item 3|Item 4");

        await ExpectTrailAsync("bc-params-options", $"Option 1|{More}|Option 3|Option 4");
        await Expect(Root("bc-params-options").Locator(".bit-brc-dtx").First).ToHaveTextAsync("/");

        await ExpectTrailAsync("bc-params-outside", "Item 1|Item 2|Item 3|Item 4");
        await Expect(Root("bc-params-outside").Locator(".bit-brc-dtx")).ToHaveCountAsync(0);
        Assert.AreEqual("A", await Root("bc-params-outside").Locator("[aria-current='page']").EvaluateAsync<string>("el => el.tagName"));
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsTheItemsAndTheDividers()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        await Expect(Step("bc-color-none", "Item 1")).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(Root("bc-color-none").Locator("i.bit-brc-div").First).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-fg-sec)"));

        foreach (var (color, suffix, main) in Colors)
        {
            var id = $"bc-color-{color}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-brc-{suffix}"));

            var expected = await ResolveColorAsync($"var({main})");
            await Expect(Step(id, "Item 1")).ToHaveCSSAsync("color", expected);
            await Expect(Step(id, "Item 4")).ToHaveCSSAsync("color", expected);
            await Expect(Root(id).Locator("i.bit-brc-div").First).ToHaveCSSAsync("color", expected);
        }
    }

    [TestMethod]
    public async Task ExternalIcons_RenderTheGivenClasses_AndIconWinsOverIconName()
    {
        await OpenAsync(Url);

        await Expect(Step("bc-ext", "Home").Locator("i")).ToHaveClassAsync(HasClass("fa-house"));
        await Expect(Step("bc-ext", "Home").Locator("i")).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(Step("bc-ext", "Products").Locator("i")).ToHaveClassAsync(HasClass("fa-box"));
        await Expect(Step("bc-ext", "Electronics").Locator("i")).ToHaveClassAsync(HasClass("fa-microchip"));
        var laptops = Step("bc-ext", "Laptops").Locator("i");
        await Expect(laptops).ToHaveClassAsync(HasClass("bi-laptop-fill"));
        await Expect(laptops).ToHaveClassAsync(HasClass("bi"));
        await Expect(laptops).Not.ToHaveClassAsync(HasClass("bit-icon--Home"));
    }

    [TestMethod]
    [DataRow("Small", "bit-brc-sm")]
    [DataRow("Medium", "bit-brc-md")]
    [DataRow("Large", "bit-brc-lg")]
    public async Task Size_EverySize_ScalesTheTrailAndTheMenu(string size, string cls)
    {
        await OpenAsync(Url);

        var id = $"bc-size-{size}";
        await Expect(Root(id)).ToHaveClassAsync(HasClass(cls));
        await Expect(Root("bc-size-none")).Not.ToHaveClassAsync(new Regex(@"bit-brc-(sm|md|lg)\b"));

        var heights = new Dictionary<string, double>();
        var fonts = new Dictionary<string, double>();
        foreach (var name in new[] { "Small", "Medium", "Large", "none" })
        {
            heights[name] = (await BoxAsync(Step($"bc-size-{name}", "Item 1"))).Height;
            fonts[name] = ParsePx(await Step($"bc-size-{name}", "Item 1").EvaluateAsync<string>("el => getComputedStyle(el).fontSize"));
        }

        switch (size)
        {
            case "Small":
                Assert.IsTrue(heights["Small"] < heights["Medium"] && fonts["Small"] < fonts["Medium"]);
                break;
            case "Medium":
                Assert.AreEqual(heights["none"], heights["Medium"], 0.5);
                Assert.AreEqual(fonts["none"], fonts["Medium"], 0.1);
                break;
            default:
                Assert.IsTrue(heights["Large"] > heights["Medium"] && fonts["Large"] > fonts["Medium"]);
                break;
        }

        // The relocated menu carries the size class too.
        await OverflowButton(id).ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync(id)}']");
        await Expect(callout).ToHaveClassAsync(HasClass(cls));
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRootAndTheItems()
    {
        await OpenAsync(Url);

        await Expect(Root("bc-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("bc-class")).ToHaveClassAsync(HasClass("bit-brc"));
        await Expect(Root("bc-style")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Root("bc-style")).ToHaveCSSAsync("margin-left", "17px");

        await Expect(Step("bc-item-style", "Item 1")).ToHaveClassAsync(HasClass("custom-item-1"));
        await Expect(Step("bc-item-style", "Item 1")).ToHaveCSSAsync("color", "rgb(30, 144, 255)");
        await Expect(Step("bc-item-style", "Item 2")).ToHaveClassAsync(HasClass("custom-item-2"));
        await Expect(Step("bc-item-style", "Item 3")).ToHaveCSSAsync("color", "rgb(0, 255, 255)");

        await Expect(Root("bc-attrs")).ToHaveAttributeAsync("data-test", "breadcrumb-attr");
        await Expect(Root("bc-attrs")).ToHaveAttributeAsync("aria-label", "Splatted trail");

        var visibility = Root("bc-visibility");
        await Page.Locator("#btn-visibility-hidden").ClickAsync();
        await Expect(visibility).ToHaveCSSAsync("visibility", "hidden");
        await Page.Locator("#btn-visibility-collapsed").ClickAsync();
        await Expect(visibility).ToHaveCSSAsync("display", "none");
        await Page.Locator("#btn-visibility-visible").ClickAsync();
        await Expect(visibility).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart_TheMenuIncluded()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var root = Root("bc-styles");
        await Expect(root).ToHaveCSSAsync("padding-top", "3px");
        await Expect(root.Locator("ol")).ToHaveCSSAsync("padding-bottom", "5px");
        await Expect(OverflowButton("bc-styles")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(OverflowButton("bc-styles").Locator("i")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(root.Locator("ol > li[role='listitem']").First).ToHaveCSSAsync("padding-left", "2px");
        await Expect(Step("bc-styles", "Item 3")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Step("bc-styles", "Item 3").Locator("i")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(Step("bc-styles", "Item 3").Locator(".bit-brc-itx")).ToHaveCSSAsync("letter-spacing", "1px");
        await Expect(root.Locator("li.bit-brc-dvw").First).ToHaveCSSAsync("padding-right", "4px");
        await Expect(root.Locator("i.bit-brc-div").First).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(Root("bc-styles-sel").Locator("[aria-current='page']")).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");

        await OverflowButton("bc-styles").ClickAsync();
        var calloutId = await CalloutIdAsync("bc-styles");
        var callout = Page.Locator($"[id='{calloutId}']");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(callout).ToHaveCSSAsync("border-top-color", "rgb(19, 20, 21)");
        await Expect(callout.Locator("ul")).ToHaveCSSAsync("padding-top", "6px");
        await Expect(callout.Locator("ul > li").First).ToHaveCSSAsync("padding-left", "3px");

        var selected = callout.Locator("[role='menuitem']").Filter(new() { HasText = "Item 1" });
        var other = callout.Locator("[role='menuitem']").Filter(new() { HasText = "Item 2" });
        await Expect(other).ToHaveCSSAsync("color", "rgb(22, 23, 24)");
        await Expect(other.Locator("i")).ToHaveCSSAsync("color", "rgb(25, 26, 27)");
        await Expect(other.Locator(".bit-brc-itx")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(selected).ToHaveCSSAsync("background-color", "rgb(28, 29, 30)");
        await Expect(other).Not.ToHaveCSSAsync("background-color", "rgb(28, 29, 30)");
        await Expect(Page.Locator($"[id='{calloutId.Replace("-callout", "-overlay")}']")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0.2)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart_TheMenuIncluded()
    {
        await OpenAsync(Url);

        var root = Root("bc-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root.Locator("ol")).ToHaveClassAsync(HasClass("c-container"));
        await Expect(OverflowButton("bc-classes")).ToHaveClassAsync(HasClass("c-obt"));
        await Expect(OverflowButton("bc-classes").Locator("i")).ToHaveClassAsync(HasClass("c-obt-icon"));
        await Expect(root.Locator("ol > li[role='listitem']").First).ToHaveClassAsync(HasClass("c-wrapper"));
        await Expect(Step("bc-classes", "Item 3")).ToHaveClassAsync(HasClass("c-item"));
        await Expect(Step("bc-classes", "Item 3").Locator("i")).ToHaveClassAsync(HasClass("c-item-icon"));
        await Expect(Step("bc-classes", "Item 3").Locator(".bit-brc-itx")).ToHaveClassAsync(HasClass("c-item-text"));
        await Expect(root.Locator("li.bit-brc-dvw").First).ToHaveClassAsync(HasClass("c-divider"));
        await Expect(root.Locator("i.bit-brc-div").First).ToHaveClassAsync(HasClass("c-divider-icon"));
        await Expect(Root("bc-styles-sel").Locator("[aria-current='page']")).ToHaveClassAsync(HasClass("c-selected"));

        await OverflowButton("bc-classes").ClickAsync();
        var calloutId = await CalloutIdAsync("bc-classes");
        var callout = Page.Locator($"[id='{calloutId}']");
        await Expect(callout).ToHaveClassAsync(HasClass("c-callout"));
        await Expect(callout.Locator("ul")).ToHaveClassAsync(HasClass("c-callout-container"));
        await Expect(callout.Locator("ul > li").First).ToHaveClassAsync(HasClass("c-ofi-wrapper"));
        var selected = callout.Locator("[role='menuitem']").Filter(new() { HasText = "Item 1" });
        var other = callout.Locator("[role='menuitem']").Filter(new() { HasText = "Item 2" });
        await Expect(other).ToHaveClassAsync(HasClass("c-ofi"));
        await Expect(other).Not.ToHaveClassAsync(HasClass("c-ofi-selected"));
        await Expect(other.Locator("i")).ToHaveClassAsync(HasClass("c-ofi-icon"));
        await Expect(other.Locator(".bit-brc-itx")).ToHaveClassAsync(HasClass("c-ofi-text"));
        await Expect(selected).ToHaveClassAsync(HasClass("c-ofi-selected"));
        await Expect(Page.Locator($"[id='{calloutId.Replace("-callout", "-overlay")}']")).ToHaveClassAsync(HasClass("c-overlay"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheTrailAndTheRelocatedMenu()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var item = Step("bc-css-vars", "Item 1");
        await Expect(item).ToHaveCSSAsync("color", "rgb(124, 58, 237)");
        await Expect(item).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(item).ToHaveCSSAsync("font-size", "19px");
        await Expect(item).ToHaveCSSAsync("line-height", "40px");
        await Expect(Step("bc-css-vars", "Item 4")).ToHaveCSSAsync("color", "rgb(190, 24, 93)");
        await Expect(Step("bc-css-vars", "Item 4")).ToHaveCSSAsync("font-weight", "800");
        await Expect(Root("bc-css-vars").Locator("i.bit-brc-div").First).ToHaveCSSAsync("color", "rgb(167, 139, 250)");
        await Expect(Root("bc-css-vars").Locator("li.bit-brc-dvw").First).ToHaveCSSAsync("margin-left", "7px");

        await item.HoverAsync();
        await Expect(item).ToHaveCSSAsync("background-color", "rgb(240, 230, 220)");

        await OverflowButton("bc-css-vars").ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-css-vars")}']");
        await Expect(callout).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(callout).ToHaveCSSAsync("border-top-left-radius", "12px");
    }

    [TestMethod]
    public async Task CssVariables_AnExplicitColorAndSize_WinOverTheVariables()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        var item = Step("bc-css-vars-color", "Item 1");
        await Expect(item).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-err)"));
        Assert.IsTrue(ParsePx(await item.EvaluateAsync<string>("el => getComputedStyle(el).fontSize")) < 19);
    }

    [TestMethod]
    public async Task Hover_ThePointerPaintsTheHoverBackground()
    {
        await OpenAsync(Url);

        var item = Step("bc-basic", "Item 1");
        await item.HoverAsync();
        await Expect(item).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-pri-hover)"));
    }

    [TestMethod]
    public async Task Keyboard_TabWalksTheLinks_WithAFocusRing()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit leaves links out of the Tab order unless the user turns that on (Safari's \"Press Tab to highlight each item\").");

        await OpenAsync(Url);

        await Step("bc-basic", "Item 1").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Step("bc-basic", "Item 2")).ToBeFocusedAsync();
        await Expect(Step("bc-basic", "Item 2")).Not.ToHaveCSSAsync("box-shadow", "none");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Item 2");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_RunsTheTrailRightToLeft_AndMirrorsTheDividersAndTheMenu()
    {
        await OpenAsync(Url);

        var root = Root("bc-rtl");
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue((await BoxAsync(Step("bc-rtl", "Item 1"))).X > (await BoxAsync(Step("bc-rtl", "Item 3"))).X);
        await Expect(root.Locator("i.bit-brc-div").First).ToHaveCSSAsync("transform", "matrix(-1, 0, 0, 1, 0, 0)");

        await OverflowButton("bc-rtl").ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-rtl")}']");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
        await Expect(callout).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var button = await BoxAsync(OverflowButton("bc-rtl"));
        var menu = await BoxAsync(callout);
        Assert.AreEqual(button.X + button.Width, menu.X + menu.Width, 2, "The menu is aligned with the right edge of its button.");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_MirrorsTheDividers()
    {
        await OpenAsync(Url);

        var root = Root("bc-rtl-inherited");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue((await BoxAsync(Step("bc-rtl-inherited", "Item 1"))).X > (await BoxAsync(Step("bc-rtl-inherited", "Item 2"))).X);
        await Expect(root.Locator("i.bit-brc-div").First).ToHaveCSSAsync("transform", "matrix(-1, 0, 0, 1, 0, 0)");

        var ltr = Root("bc-ltr");
        Assert.IsTrue((await BoxAsync(Step("bc-ltr", "Item 1"))).X < (await BoxAsync(Step("bc-ltr", "Item 2"))).X);
        await Expect(ltr.Locator("i.bit-brc-div").First).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
    }

    [TestMethod]
    public async Task EscapeInsideADialog_ClosesTheOverflowMenuFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("open");

        await OverflowButton("bc-in-dialog").ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-in-dialog")}']");
        await Expect(callout).ToBeVisibleAsync();
        // WebKit focuses no button on a click; the breadcrumb focuses its overflow button itself, where Escape is answered.
        await Expect(OverflowButton("bc-in-dialog")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("open");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task CustomItems_NameSelectorsMapEveryProperty_ByNameAndBySelector()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-custom", $"Root|{More}|Guides|Intro");

        var rootStep = Step("bc-custom", "Root");
        await Expect(rootStep).ToHaveAttributeAsync("href", "/e2e/breadcrumb?step=root");
        await Expect(rootStep).ToHaveAttributeAsync("title", "The root");
        await Expect(rootStep.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Home"));

        var guides = Step("bc-custom", "Guides");
        Assert.AreEqual("BUTTON", await guides.EvaluateAsync<string>("el => el.tagName"));
        await Expect(guides).ToHaveClassAsync(HasClass("guides-crumb"));
        await Expect(Step("bc-custom", "Intro")).ToHaveAttributeAsync("aria-current", "page");

        await guides.ClickAsync();
        await Expect(Page.Locator("#custom-clicked")).ToHaveTextAsync("Guides");

        // The disabled one, in the menu, is a disabled menu item the keyboard skips.
        await OverflowButton("bc-custom").ClickAsync();
        var callout = Page.Locator($"[id='{await CalloutIdAsync("bc-custom")}']");
        var docs = callout.Locator("[role='menuitem']").Filter(new() { HasText = "Docs" });
        await Expect(docs).ToBeDisabledAsync();
        await Expect(docs).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(docs).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(docs).ToHaveClassAsync(HasClass("bit-brc-dis"));
    }

    [TestMethod]
    public async Task Options_RenderInMarkupOrder_AndFollowParametersChangedAtRuntime()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-options", "Option 1|Option 2|Option 3|Option 4");

        var two = Step("bc-options", "Option 2");
        Assert.AreEqual("BUTTON", await two.EvaluateAsync<string>("el => el.tagName"));
        await Expect(two.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Folder"));
        await two.ClickAsync();
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Option 2");

        var four = Step("bc-options", "Option 4");
        await Expect(four).ToHaveAttributeAsync("aria-current", "page");
        await Expect(four).ToHaveAttributeAsync("title", "The current option");
        await Expect(four).ToHaveClassAsync(HasClass("option-four"));
        await Expect(four).ToHaveCSSAsync("text-decoration-line", "underline");

        // An option rendered later takes its place in the markup, not the end of the trail.
        await Page.Locator("#btn-toggle-middle").ClickAsync();
        await ExpectTrailAsync("bc-options", "Option 1|Option middle|Option 2|Option 3|Option 4");
        await Page.Locator("#btn-toggle-middle").ClickAsync();
        await ExpectTrailAsync("bc-options", "Option 1|Option 2|Option 3|Option 4");

        await Page.Locator("#btn-rename-two").ClickAsync();
        await ExpectTrailAsync("bc-options", "Option 1|Option two|Option 3|Option 4");

        await Page.Locator("#btn-disable-three").ClickAsync();
        await Expect(Step("bc-options", "Option 3")).ToBeDisabledAsync();

        await Page.Locator("#btn-options-max").ClickAsync();
        await ExpectTrailAsync("bc-options", $"Option 1|{More}|Option 3|Option 4");
    }

    [TestMethod]
    public async Task AutoReorderOptions_FollowsAKeyedLoopThatIsResorted()
    {
        await OpenAsync(Url);

        await ExpectTrailAsync("bc-reorder", "Apple|Banana|Cherry");
        await Page.Locator("#btn-reverse").ClickAsync();
        await ExpectTrailAsync("bc-reorder", "Cherry|Banana|Apple");
        await Page.Locator("#btn-reverse").ClickAsync();
        await ExpectTrailAsync("bc-reorder", "Apple|Banana|Cherry");
    }

    [TestMethod]
    public async Task Unmount_WhileTheMenuIsOpen_RemovesItWithoutAnError()
    {
        await OpenAsync(Url);

        var calloutId = await CalloutIdAsync("bc-unmount");
        await OverflowButton("bc-unmount").ClickAsync();
        await Expect(Page.Locator($"[id='{calloutId}']")).ToBeVisibleAsync();

        await Page.Locator("#btn-toggle-mounted").DispatchEventAsync("click");

        await Expect(Page.Locator("#mounted-state")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#bc-unmount")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{calloutId}']")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{calloutId.Replace("-callout", "-overlay")}']")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted-state")).ToHaveTextAsync("True");
        await ExpectTrailAsync("bc-unmount", $"{More}|Item 3|Item 4");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }



    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator OverflowButton(string id) => Page.Locator($"#{id} .bit-brc-obt");

    // A step of the trail (a link, a button or a text step) by its text.
    private ILocator Step(string id, string text)
    {
        return Page.Locator($"#{id} .bit-brc-icn > li > :is(a, button.bit-brc-itm, span.bit-brc-nii)").Filter(new() { HasText = text });
    }

    // The callout of a breadcrumb, named after its overflow button (both carry the instance's unique id).
    private async Task<string> CalloutIdAsync(string id)
    {
        var anchorId = await Page.Locator($"#{id} .bit-brc-obt").GetAttributeAsync("id");
        return anchorId!.Replace("-overflow-anchor", "-callout");
    }

    private Task<string> TrailAsync(string id)
    {
        return Page.Locator($"#{id}").EvaluateAsync<string>(TrailScript);
    }

    private const string TrailScript = @"root => [...root.querySelectorAll('.bit-brc-icn > li:not(.bit-brc-dvw)')]
        .map(li => li.querySelector('.bit-brc-obt') ? '[more]' : (li.querySelector('.bit-brc-itx, .tpl-item, .own-tpl') ?? li).textContent.trim())
        .join('|')";

    // Waits for the trail to show the given steps, the overflow button standing as [more].
    private async Task ExpectTrailAsync(string id, string expected)
    {
        try
        {
            await Page.WaitForFunctionAsync($@"([id, expected]) => {{
                const root = document.getElementById(id);
                if (!root) return false;
                return ({TrailScript})(root) === expected;
            }}", new object[] { id, expected }, new() { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            Assert.AreEqual(expected, await TrailAsync(id), $"The trail of {id}.");
        }
    }

    // Brings an element to the middle of the viewport, so a menu opened from it has room on both sides, and lets the
    // scroll it caused settle first (a scroll dismisses an open menu).
    private async Task CenterAsync(string selector)
    {
        await Page.Locator(selector).EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await NextFramesAsync();
        await NextFramesAsync();
    }

    private async Task SetWidthAsync(string selector, int width)
    {
        await Page.Locator(selector).EvaluateAsync($"el => el.style.width = '{width}px'");
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no box.");
        return box;
    }

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const probe = document.createElement('div');
            probe.style.backgroundColor = v;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).backgroundColor;
            probe.remove();
            return color;
        }", value);
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);
}
