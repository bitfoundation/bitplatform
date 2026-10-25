namespace Bit.BlazorUI.Tests.E2E.Lists;

/// <summary>
/// BitTimeline in a real browser, every feature run over its three APIs - a BitTimelineItem list, a model of its own
/// read through NameSelectors (by Name and by Selector), and BitTimelineOption children: the ordered list and its items,
/// the two sides of the line vertical and Horizontal, Disabled (timeline and item), every Variant with icons, Reversed
/// (timeline and item) and Alternate, ReverseOrder, TruncateLine and LineStyle (timeline and item), DotAlignment,
/// LinePlacement, the item's own Color / Size / Variant / HideDot, the templates (PrimaryContent, SecondaryContent,
/// DotTemplate, Template, and the timeline's ItemTemplate and DotTemplate), the events (an item's OnClick and the
/// timeline's OnItemClick, by mouse, Enter and Space, and none on a disabled item), the accessibility attributes, an
/// external Icon winning over IconName, Size, Style / Class / Styles / Classes, RTL (explicit and inherited); then
/// every Color with every variant (timeline and item), the BitParams cascade, the --bit-Timeline-* variables and their
/// variant tiers, runtime changes of the items and the options, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitTimelineE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    // Every BitColor, in the enum's order, with the class suffixes of the timeline and of an item, and its role's tokens.
    private static readonly (string Name, string Suffix, string ItemSuffix, string Main, string On)[] Colors =
    [
        ("Primary", "pri", "ipr", "--bit-clr-pri", "--bit-clr-pri-text"),
        ("Secondary", "sec", "ise", "--bit-clr-sec", "--bit-clr-sec-text"),
        ("Tertiary", "ter", "ite", "--bit-clr-ter", "--bit-clr-ter-text"),
        ("Info", "inf", "iin", "--bit-clr-inf", "--bit-clr-inf-text"),
        ("Success", "suc", "isu", "--bit-clr-suc", "--bit-clr-suc-text"),
        ("Warning", "wrn", "iwr", "--bit-clr-wrn", "--bit-clr-wrn-text"),
        ("SevereWarning", "swr", "isw", "--bit-clr-swr", "--bit-clr-swr-text"),
        ("Error", "err", "ier", "--bit-clr-err", "--bit-clr-err-text"),
        ("PrimaryBackground", "pbg", "ipbg", "--bit-clr-bg-pri", "--bit-clr-fg-pri"),
        ("SecondaryBackground", "sbg", "isbg", "--bit-clr-bg-sec", "--bit-clr-fg-pri"),
        ("TertiaryBackground", "tbg", "itbg", "--bit-clr-bg-ter", "--bit-clr-fg-pri"),
        ("PrimaryForeground", "pfg", "ipfg", "--bit-clr-fg-pri", "--bit-clr-bg-pri"),
        ("SecondaryForeground", "sfg", "isfg", "--bit-clr-fg-sec", "--bit-clr-bg-pri"),
        ("TertiaryForeground", "tfg", "itfg", "--bit-clr-fg-ter", "--bit-clr-bg-pri"),
        ("PrimaryBorder", "pbr", "ipbr", "--bit-clr-brd-pri", "--bit-clr-fg-pri"),
        ("SecondaryBorder", "sbr", "isbr", "--bit-clr-brd-sec", "--bit-clr-fg-pri"),
        ("TertiaryBorder", "tbr", "itbr", "--bit-clr-brd-ter", "--bit-clr-fg-pri"),
    ];

    [TestMethod]
    public async Task Prerender_TheTimelineIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/timeline");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<ol id=\"items-basic\"");
        StringAssert.Contains(html, "id=\"custom-basic\"");
        StringAssert.Contains(html, "bit-tln-dot");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task Basic_EachItemPutsADotOnTheLine_PrimaryBeforeItSecondaryAfter(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-basic";
        var root = Page.Locator($"#{id}");
        Assert.AreEqual("OL", await root.EvaluateAsync<string>("el => el.tagName"));
        await Expect(root).ToHaveAttributeAsync("role", "list");
        await Expect(root.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);
        await Expect(Item(id, 1).Locator(".bit-tln-pcn .bit-tln-ttx")).ToHaveTextAsync("P2");
        await Expect(Item(id, 1).Locator(".bit-tln-scn .bit-tln-ttx")).ToHaveTextAsync("S2");
        await Expect(Item(id, 0).Locator(".bit-tln-scn .bit-tln-ttx")).ToHaveCountAsync(0);

        // A vertical line: the dots on one x, the primary text to their left, the secondary to their right.
        var dots = await DotsAsync(id);
        Assert.IsTrue(dots.All(d => Math.Abs(d.X - dots[0].X) <= Tolerance), "the dots on one vertical line");
        Assert.IsTrue(dots[1].Y > dots[0].Bottom && dots[2].Y > dots[1].Bottom, "one under the other");
        Assert.IsTrue((await BoxAsync($"#{id} > li >> nth=1", ".bit-tln-pcn .bit-tln-ttx")).Right <= dots[1].X, "primary before the line");
        Assert.IsTrue((await BoxAsync($"#{id} > li >> nth=1", ".bit-tln-scn .bit-tln-ttx")).X >= dots[1].Right, "secondary after the line");

        // Horizontal: the dots on one y, the primary text above them, the secondary below.
        var h = $"{api}-basic-h";
        await Expect(Page.Locator($"#{h}")).ToHaveClassAsync(HasClass("bit-tln-hrz"));
        var hDots = await DotsAsync(h);
        Assert.IsTrue(hDots.All(d => Math.Abs(d.Y - hDots[0].Y) <= Tolerance), "the dots on one horizontal line");
        Assert.IsTrue(hDots[1].X > hDots[0].Right, "side by side");
        Assert.IsTrue((await BoxAsync($"#{h} > li >> nth=1", ".bit-tln-pcn .bit-tln-ttx")).Bottom <= hDots[1].Y, "primary above the line");
        Assert.IsTrue((await BoxAsync($"#{h} > li >> nth=1", ".bit-tln-scn .bit-tln-ttx")).Y >= hDots[1].Bottom, "secondary below the line");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task Disabled_TheTimelineDisablesEveryItem_AndAnItemItsOwn(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-disabled";
        await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator($"#{id}")).ToHaveCSSAsync("pointer-events", "none");
        for (var i = 0; i < 3; i++)
        {
            await Expect(Item(id, i)).ToHaveAttributeAsync("aria-disabled", "true");
        }
        var disabledBackground = await ResolveAsync("var(--bit-clr-pri-dis)");
        await Expect(Item(id, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", disabledBackground);

        var item = $"{api}-disabled-item";
        await Expect(Page.Locator($"#{item}")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Item(item, 1)).ToHaveClassAsync(HasClass("bit-tln-ids"));
        await Expect(Item(item, 1)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Item(item, 1).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", disabledBackground);
        Assert.IsNull(await Item(item, 0).GetAttributeAsync("aria-disabled"));
        await Expect(Item(item, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", await ResolveAsync("var(--bit-clr-pri)"));
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task IconAndVariant_EveryVariantPaintsTheDotDifferently(string api)
    {
        await OpenAsync("/e2e/timeline");

        var primary = await ResolveAsync("var(--bit-clr-pri)");
        var primaryText = await ResolveAsync("var(--bit-clr-pri-text)");
        const string transparent = "rgba(0, 0, 0, 0)";

        await Expect(Item($"{api}-fill", 0).Locator("i.bit-tln-ico")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Item($"{api}-fill", 1).Locator("i.bit-tln-ico")).ToHaveClassAsync(HasClass("bit-icon--Edit"));

        var fill = Item($"{api}-fill", 0);
        await Expect(Page.Locator($"#{api}-fill")).ToHaveClassAsync(HasClass("bit-tln-fil"));
        await Expect(fill.Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", primary);
        await Expect(fill.Locator(".bit-tln-ico")).ToHaveCSSAsync("color", primaryText);

        var outline = Item($"{api}-outline", 0);
        await Expect(Page.Locator($"#{api}-outline")).ToHaveClassAsync(HasClass("bit-tln-otl"));
        await Expect(outline.Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", transparent);
        await Expect(outline.Locator(".bit-tln-dot")).ToHaveCSSAsync("border-top-color", primary);
        await Expect(outline.Locator(".bit-tln-ico")).ToHaveCSSAsync("color", primary);

        var text = Item($"{api}-text", 0);
        await Expect(Page.Locator($"#{api}-text")).ToHaveClassAsync(HasClass("bit-tln-txt"));
        await Expect(text.Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", transparent);
        await Expect(text.Locator(".bit-tln-dot")).ToHaveCSSAsync("border-top-color", transparent);
        await Expect(text.Locator(".bit-tln-ico")).ToHaveCSSAsync("color", primary);
        await Expect(text.Locator(".bit-tln-ico")).ToHaveCSSAsync("width", "30px");

        await Expect(Page.Locator($"#{api}-default-variant")).ToHaveClassAsync(HasClass("bit-tln-fil"));
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task ReversedAndAlternate_SwapTheSidesOfTheLine(string api)
    {
        await OpenAsync("/e2e/timeline");

        await Expect(Page.Locator($"#{api}-reversed")).ToHaveClassAsync(HasClass("bit-tln-rvs"));
        for (var i = 0; i < 4; i++)
        {
            Assert.IsTrue(await PrimaryAfterTheLineAsync($"{api}-reversed", i), $"reversed: item {i} has its primary after the line");
        }

        await Expect(Item($"{api}-item-reversed", 1)).ToHaveClassAsync(HasClass("bit-tln-irv"));
        Assert.IsFalse(await PrimaryAfterTheLineAsync($"{api}-item-reversed", 0), "an item that is not reversed keeps its sides");
        Assert.IsTrue(await PrimaryAfterTheLineAsync($"{api}-item-reversed", 1), "the reversed item swaps its sides");

        await Expect(Page.Locator($"#{api}-alternate")).ToHaveClassAsync(HasClass("bit-tln-alt"));
        var alternate = new List<bool>();
        var alternateReversed = new List<bool>();
        for (var i = 0; i < 4; i++)
        {
            alternate.Add(await PrimaryAfterTheLineAsync($"{api}-alternate", i));
            alternateReversed.Add(await PrimaryAfterTheLineAsync($"{api}-alternate-reversed", i));
        }
        CollectionAssert.AreEqual(new[] { false, true, false, true }, alternate, "alternate zig-zags");
        CollectionAssert.AreEqual(new[] { true, false, true, false }, alternateReversed, "alternate & reversed starts on the other side");

        // Horizontal: reversed puts the primary below the line, alternate every second one.
        var hDot = await BoxAsync($"#{api}-reversed-h > li >> nth=0", ".bit-tln-dot");
        Assert.IsTrue((await BoxAsync($"#{api}-reversed-h > li >> nth=0", ".bit-tln-pcn .bit-tln-ttx")).Y >= hDot.Bottom, "horizontal reversed: primary below");
        var aDot0 = await BoxAsync($"#{api}-alternate-h > li >> nth=0", ".bit-tln-dot");
        var aDot1 = await BoxAsync($"#{api}-alternate-h > li >> nth=1", ".bit-tln-dot");
        Assert.IsTrue((await BoxAsync($"#{api}-alternate-h > li >> nth=0", ".bit-tln-pcn .bit-tln-ttx")).Bottom <= aDot0.Y, "horizontal alternate: first above");
        Assert.IsTrue((await BoxAsync($"#{api}-alternate-h > li >> nth=1", ".bit-tln-pcn .bit-tln-ttx")).Y >= aDot1.Bottom, "horizontal alternate: second below");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task ReverseOrder_PaintsTheLastItemFirst_WithoutChangingTheReadingOrder(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-reverse-order";
        await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-tln-rvo"));
        await Expect(Page.Locator($"#{id}")).ToHaveCSSAsync("flex-direction", "column-reverse");
        Assert.IsTrue((await BoxAsync($"#{id} > li >> nth=3")).Y < (await BoxAsync($"#{id} > li >> nth=0")).Y, "the last item painted on top");
        await Expect(Page.Locator($"#{id} > li .bit-tln-pcn")).ToHaveTextAsync(new[] { "A1", "A2", "A3", "A4" });

        var h = $"{api}-reverse-order-h";
        await Expect(Page.Locator($"#{h}")).ToHaveCSSAsync("flex-direction", "row-reverse");
        Assert.IsTrue((await BoxAsync($"#{h} > li >> nth=3")).X < (await BoxAsync($"#{h} > li >> nth=0")).X, "horizontal: the last item painted first");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task TruncateLine_EndsTheLineAtTheFirstOrLastDot(string api)
    {
        await OpenAsync("/e2e/timeline");

        var cases = new (string Name, string FirstBefore, string LastAfter, string Class)[]
        {
            ("truncate-none", "visible", "visible", ""),
            ("truncate-start", "hidden", "visible", "bit-tln-tls"),
            ("truncate-end", "visible", "hidden", "bit-tln-tle"),
            ("truncate-both", "hidden", "hidden", "bit-tln-tlb"),
        };

        foreach (var (name, firstBefore, lastAfter, cls) in cases)
        {
            var id = $"{api}-{name}";
            if (cls.Length > 0) await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass(cls));
            Assert.AreEqual(firstBefore, await PseudoAsync($"#{id} > li:first-child .bit-tln-dvd", "::before", "visibility"), $"{name}: the line before the first dot");
            Assert.AreEqual(lastAfter, await PseudoAsync($"#{id} > li:last-child .bit-tln-dvd", "::after", "visibility"), $"{name}: the line after the last dot");
            Assert.AreEqual("visible", await PseudoAsync($"#{id} > li:nth-child(2) .bit-tln-dvd", "::before", "visibility"), $"{name}: the middle stays");
        }
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task LineStyle_PaintsTheLineSolidDashedOrDotted_ForTheTimelineAndForAnItem(string api)
    {
        await OpenAsync("/e2e/timeline");

        var solid = await PseudoAsync($"#{api}-line-solid > li:first-child .bit-tln-dvd", "::after", "background-image");
        var dashed = await PseudoAsync($"#{api}-line-dashed > li:first-child .bit-tln-dvd", "::after", "background-image");
        var dotted = await PseudoAsync($"#{api}-line-dotted > li:first-child .bit-tln-dvd", "::after", "background-image");

        await Expect(Page.Locator($"#{api}-line-dashed")).ToHaveClassAsync(HasClass("bit-tln-ldd"));
        await Expect(Page.Locator($"#{api}-line-dotted")).ToHaveClassAsync(HasClass("bit-tln-ldt"));
        StringAssert.Contains(solid, "100%");
        StringAssert.Contains(dashed, "8px");
        StringAssert.Contains(dashed, "14px");
        StringAssert.Contains(dotted, "2px");
        StringAssert.Contains(dotted, "6px");
        Assert.AreNotEqual(solid, dashed);
        Assert.AreNotEqual(dashed, dotted);

        // An item paints its own stretch its own way.
        await Expect(Item($"{api}-line-items", 0)).ToHaveClassAsync(HasClass("bit-tln-ild"));
        await Expect(Item($"{api}-line-items", 1)).ToHaveClassAsync(HasClass("bit-tln-ilt"));
        await Expect(Item($"{api}-line-items", 2)).ToHaveClassAsync(HasClass("bit-tln-ils"));
        Assert.AreEqual(dashed, await PseudoAsync($"#{api}-line-items > li:nth-child(1) .bit-tln-dvd", "::after", "background-image"));
        Assert.AreEqual(dotted, await PseudoAsync($"#{api}-line-items > li:nth-child(2) .bit-tln-dvd", "::after", "background-image"));
        Assert.AreEqual(solid, await PseudoAsync($"#{api}-line-items > li:nth-child(3) .bit-tln-dvd", "::after", "background-image"));
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task DotAlignment_PinsTheDotToTheCenterTheStartOrTheEndOfTheItem(string api)
    {
        await OpenAsync("/e2e/timeline");

        var centerItem = await BoxAsync($"#{api}-dot-center > li >> nth=0");
        var centerDot = await BoxAsync($"#{api}-dot-center > li >> nth=0", ".bit-tln-dot");
        AssertClose(centerItem.Y + centerItem.H / 2, centerDot.Y + centerDot.H / 2, "centered by default");

        await Expect(Page.Locator($"#{api}-dot-start")).ToHaveClassAsync(HasClass("bit-tln-das"));
        var startItem = await BoxAsync($"#{api}-dot-start > li >> nth=0");
        var startDot = await BoxAsync($"#{api}-dot-start > li >> nth=0", ".bit-tln-dot");
        AssertClose(startItem.Y + 15, startDot.Y, "at the start, half a dot in");

        await Expect(Page.Locator($"#{api}-dot-end")).ToHaveClassAsync(HasClass("bit-tln-dae"));
        var endItem = await BoxAsync($"#{api}-dot-end > li >> nth=0");
        var endDot = await BoxAsync($"#{api}-dot-end > li >> nth=0", ".bit-tln-dot");
        AssertClose(endItem.Bottom - 15, endDot.Bottom, "at the end, half a dot in");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task LinePlacement_MovesTheLineToAnEdge_AndStacksTheContentsBesideIt(string api)
    {
        await OpenAsync("/e2e/timeline");

        await Expect(Page.Locator($"#{api}-line-start")).ToHaveClassAsync(HasClass("bit-tln-lps"));
        var item = await BoxAsync($"#{api}-line-start > li >> nth=0");
        var divider = await BoxAsync($"#{api}-line-start > li >> nth=0", ".bit-tln-dvd");
        var primary = await BoxAsync($"#{api}-line-start > li >> nth=0", ".bit-tln-pcn");
        var secondary = await BoxAsync($"#{api}-line-start > li >> nth=0", ".bit-tln-scn");
        AssertClose(item.X, divider.X, "the line at the start edge");
        Assert.IsTrue(primary.X >= divider.Right - Tolerance, "the contents after it");
        Assert.IsTrue(secondary.Y >= primary.Bottom - Tolerance, "the secondary stacked under the primary");

        await Expect(Page.Locator($"#{api}-line-end")).ToHaveClassAsync(HasClass("bit-tln-lpe"));
        var endItem = await BoxAsync($"#{api}-line-end > li >> nth=0");
        var endDivider = await BoxAsync($"#{api}-line-end > li >> nth=0", ".bit-tln-dvd");
        AssertClose(endItem.Right, endDivider.Right, "the line at the end edge");
        Assert.IsTrue((await BoxAsync($"#{api}-line-end > li >> nth=0", ".bit-tln-pcn")).Right <= endDivider.X + Tolerance, "the contents before it");

        var hDivider = await BoxAsync($"#{api}-line-start-h > li >> nth=0", ".bit-tln-dvd");
        Assert.IsTrue((await BoxAsync($"#{api}-line-start-h > li >> nth=0", ".bit-tln-pcn")).Y >= hDivider.Bottom - Tolerance, "horizontal: the line above the contents");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task ItemCustomization_ColorSizeVariantAndHideDotOfOneItem(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-customized";
        await Expect(Item(id, 0)).ToHaveClassAsync(HasClass("bit-tln-ier"));
        await Expect(Item(id, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", await ResolveAsync("var(--bit-clr-err)"));

        await Expect(Item(id, 1)).ToHaveClassAsync(HasClass("bit-tln-ilg"));
        await Expect(Item(id, 1).Locator(".bit-tln-dot")).ToHaveCSSAsync("width", "42px");

        await Expect(Item(id, 2)).ToHaveClassAsync(HasClass("bit-tln-ivo"));
        await Expect(Item(id, 2).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(Item(id, 2).Locator(".bit-tln-dot")).ToHaveCSSAsync("border-top-color", await ResolveAsync("var(--bit-clr-pri)"));

        // HideDot keeps the place of the dot on the line.
        await Expect(Item(id, 3).Locator(".bit-tln-dot")).ToHaveCountAsync(0);
        await Expect(Item(id, 3).Locator(".bit-tln-hdd")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Item(id, 3).Locator(".bit-tln-dvd")).ToHaveClassAsync(HasClass("bit-tln-dvh"));
        var hidden = await BoxAsync($"#{id} > li >> nth=3", ".bit-tln-hdd");
        var plainDot = await BoxAsync($"#{id} > li >> nth=4", ".bit-tln-dot");
        AssertClose(plainDot.X, hidden.X, "the line keeps its axis");

        await Expect(Item(id, 4).Locator(".bit-tln-dot")).ToHaveCSSAsync("width", "30px");
        await Expect(Item(id, 4).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", await ResolveAsync("var(--bit-clr-pri)"));
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task Templates_ReplaceThePartsOrTheWholeItem(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-templates";
        await Expect(Item(id, 0).Locator(".bit-tln-pcn .tl-pc")).ToHaveTextAsync("T1 content");
        await Expect(Item(id, 0).Locator(".bit-tln-scn .tl-sc")).ToHaveTextAsync("S1 content");
        await Expect(Item(id, 0).Locator(".bit-tln-ttx")).ToHaveCountAsync(0);

        await Expect(Item(id, 1).Locator(".bit-tln-dvd .tl-dot")).ToHaveTextAsync("*");
        await Expect(Item(id, 1).Locator(".bit-tln-dot")).ToHaveCountAsync(0);

        await Expect(Item(id, 2).Locator(".tl-whole")).ToHaveTextAsync("whole T3");
        await Expect(Item(id, 2).Locator(".bit-tln-pcn")).ToHaveCountAsync(0);
        await Expect(Item(id, 2).Locator(".bit-tln-dvd")).ToHaveCountAsync(0);

        // The timeline's own templates reach every item.
        await Expect(Page.Locator($"#{api}-timeline-templates .tl-item-template")).ToHaveTextAsync(new[] { "item P1", "item P2", "item P3" });
        await Expect(Page.Locator($"#{api}-timeline-dot-template .bit-tln-dvd .tl-tdot")).ToHaveCountAsync(3);
        await Expect(Page.Locator($"#{api}-timeline-dot-template .bit-tln-dot")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task Events_AClickableItem_IsAButtonClickedByMouseEnterAndSpace(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-events";
        var button = Item(id, 0).Locator(".bit-tln-btn");
        await Expect(button).ToHaveAttributeAsync("role", "button");
        await Expect(button).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Item(id, 0)).ToHaveClassAsync(HasClass("bit-tln-int"));
        await Expect(Item(id, 2).Locator(".bit-tln-btn")).ToHaveCountAsync(0);

        await button.ClickAsync();
        await Expect(Page.Locator("#click-log")).ToHaveTextAsync($"{id}:C1");

        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#click-log")).ToHaveTextAsync($"{id}:C1|{id}:C1");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#click-log")).ToHaveTextAsync($"{id}:C1|{id}:C1|{id}:C1");

        // A disabled clickable item is a disabled button, out of the tab order, that fires nothing.
        var disabled = Item(id, 1).Locator(".bit-tln-btn");
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        Assert.IsNull(await disabled.GetAttributeAsync("tabindex"));
        Assert.IsNull(await Item(id, 1).GetAttributeAsync("aria-disabled"), "the state is announced once, by the button");
        await disabled.DispatchEventAsync("click");
        await button.ClickAsync();
        await Expect(Page.Locator("#click-log")).ToHaveTextAsync($"{id}:C1|{id}:C1|{id}:C1|{id}:C1");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task OnItemClick_MakesEveryItemClickable_AndADisabledTimelineFiresNothing(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-on-item-click";
        await Expect(Page.Locator($"#{id} .bit-tln-btn")).ToHaveCountAsync(3);
        await Item(id, 1).Locator(".bit-tln-btn").ClickAsync();
        await Expect(Page.Locator("#click-log")).ToHaveTextAsync($"{id}/timeline:P2");

        var disabled = $"{api}-disabled-click";
        await Item(disabled, 0).Locator(".bit-tln-btn").DispatchEventAsync("click");
        await Item(id, 2).Locator(".bit-tln-btn").ClickAsync();
        await Expect(Page.Locator("#click-log")).ToHaveTextAsync($"{id}/timeline:P2|{id}/timeline:P3");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task Accessibility_TheListItsLabelAndTheItemsNamesAndTitles(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-a11y";
        await Expect(Page.Locator($"#{id}")).ToHaveAttributeAsync("aria-label", "Project history");
        await Expect(Page.Locator($"#{id}").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);

        await Expect(Item(id, 0)).ToHaveAttributeAsync("aria-label", "First milestone");
        await Expect(Item(id, 0)).ToHaveAttributeAsync("title", "Tip 1");

        // A clickable item is named through its button, and still titled on the item.
        Assert.IsNull(await Item(id, 1).GetAttributeAsync("aria-label"));
        await Expect(Item(id, 1).Locator(".bit-tln-btn")).ToHaveAttributeAsync("aria-label", "Clickable milestone");
        await Expect(Item(id, 1)).ToHaveAttributeAsync("title", "Tip 2");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Clickable milestone" })).ToHaveCountAsync(3);

        // The dot and its icon are decoration.
        await Expect(Item(id, 2).Locator(".bit-tln-dot")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task ExternalIcon_WinsOverIconName(string api)
    {
        await OpenAsync("/e2e/timeline");

        var icon = Item($"{api}-external", 0).Locator("i.bit-tln-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("tl-ext-icon"));
        await Expect(icon).ToHaveClassAsync(HasClass("tl-ext-name"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Item($"{api}-external", 1).Locator("i.bit-tln-ico")).ToHaveClassAsync(HasClass("bit-icon--Edit"));
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task Size_ScalesTheDotsAndTheText(string api)
    {
        await OpenAsync("/e2e/timeline");

        var sizes = new (string Name, string Class, string Dot)[] { ("size-sm", "bit-tln-sm", "20px"), ("size-md", "bit-tln-md", "30px"), ("size-lg", "bit-tln-lg", "42px") };
        var fontSizes = new List<double>();
        foreach (var (name, cls, dot) in sizes)
        {
            var id = $"{api}-{name}";
            await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass(cls));
            await Expect(Item(id, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("width", dot);
            await Expect(Item(id, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("height", dot);
            fontSizes.Add(double.Parse((await ComputedStyleAsync($"#{id} > li .bit-tln-pcn", "font-size")).Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture));
        }
        Assert.IsTrue(fontSizes[0] < fontSizes[1] && fontSizes[1] < fontSizes[2], $"the text grows with the size ({string.Join(", ", fontSizes)})");
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task StyleClassStylesClasses_ReachTheRootTheItemsAndEveryPart(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-styled";
        var root = Page.Locator($"#{id}");
        await Expect(root).ToHaveClassAsync(HasClass("tl-custom-root"));
        await Expect(root).ToHaveCSSAsync("max-width", "500px");
        await Expect(root).ToHaveCSSAsync("color", "rgb(30, 144, 255)");
        await Expect(root).ToHaveCSSAsync("padding-top", "1px");

        var first = Item(id, 0);
        var parts = new (ILocator Locator, string Class, string Property, string Value)[]
        {
            (first, "tl-item-class", "padding-top", "2px"),
            (first.Locator(".bit-tln-pcn"), "tl-pcn-class", "margin-top", "3px"),
            (first.Locator(".bit-tln-pcn .bit-tln-ttx"), "tl-ptx-class", "color", "rgb(32, 178, 170)"),
            (first.Locator(".bit-tln-scn"), "tl-scn-class", "margin-top", "4px"),
            (first.Locator(".bit-tln-scn .bit-tln-ttx"), "tl-stx-class", "color", "rgb(5, 6, 7)"),
            (first.Locator(".bit-tln-dvd"), "tl-dvd-class", "margin-top", "5px"),
            (first.Locator(".bit-tln-dot"), "tl-dot-class", "background-color", "rgb(32, 178, 170)"),
            (first.Locator(".bit-tln-ico"), "tl-ico-class", "color", "rgb(245, 245, 245)"),
        };

        foreach (var (locator, cls, property, value) in parts)
        {
            await Expect(locator).ToHaveClassAsync(HasClass(cls));
            await Expect(locator).ToHaveCSSAsync(property, value);
        }

        // The divider class sets the line color the divider paints with.
        StringAssert.Contains(await PseudoAsync($"#{id} > li:first-child .bit-tln-dvd", "::after", "background-image"), "rgb(6, 7, 8)");

        // The item's own Class and Style.
        await Expect(first).ToHaveClassAsync(HasClass("tl-entry-class"));
        await Expect(first).ToHaveCSSAsync("background-color", "rgb(250, 250, 210)");
        await Expect(Item(id, 1)).Not.ToHaveClassAsync(HasClass("tl-entry-class"));
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task Rtl_ExplicitAndInherited_MirrorTheTimeline(string api)
    {
        await OpenAsync("/e2e/timeline");

        var id = $"{api}-rtl";
        await Expect(Page.Locator($"#{id}")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass("bit-rtl"));
        Assert.IsTrue(await PrimaryAfterTheLineAsync(id, 0), "right to left: the primary on the right of the line");
        Assert.IsTrue(await PrimaryAfterTheLineAsync($"{api}-rtl-inherited", 0), "inherited: the primary on the right of the line");

        var h = $"{api}-rtl-h";
        Assert.IsTrue((await BoxAsync($"#{h} > li >> nth=0")).X > (await BoxAsync($"#{h} > li >> nth=1")).X, "horizontal: the first item at the right");
    }

    [TestMethod]
    public async Task Color_EveryColor_PaintsEveryVariant_ForTheTimelineAndForAnItem()
    {
        await OpenAsync("/e2e/timeline");

        foreach (var (name, suffix, itemSuffix, main, on) in Colors)
        {
            var mainColor = await ResolveAsync($"var({main})");
            Assert.AreNotEqual("rgba(0, 0, 0, 0)", mainColor, $"{main} resolves to a color");

            var id = $"items-color-{name}";
            await Expect(Page.Locator($"#{id}")).ToHaveClassAsync(HasClass($"bit-tln-{suffix}"));
            await Expect(Item(id, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", mainColor);
            await Expect(Item(id, 0).Locator(".bit-tln-ico")).ToHaveCSSAsync("color", await ResolveAsync($"var({on})"));
            await Expect(Item(id, 1).Locator(".bit-tln-dot")).ToHaveCSSAsync("border-top-color", mainColor);
            await Expect(Item(id, 2).Locator(".bit-tln-ico")).ToHaveCSSAsync("color", mainColor);
            await Expect(Item(id, 3).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", await ResolveAsync($"var({main}-dis)"));

            var item = $"items-item-color-{name}";
            await Expect(Item(item, 0)).ToHaveClassAsync(HasClass($"bit-tln-{itemSuffix}"));
            await Expect(Item(item, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", mainColor);
        }
    }

    [TestMethod]
    [DataRow("items")]
    [DataRow("custom")]
    [DataRow("options")]
    public async Task CascadingParameters_BitTimelineParams_AreDefaultsForEveryApi(string api)
    {
        await OpenAsync("/e2e/timeline");

        var cascaded = Page.Locator($"#{api}-casc");
        foreach (var cls in new[] { "bit-tln-hrz", "bit-tln-suc", "bit-tln-otl", "bit-tln-tlb" })
        {
            await Expect(cascaded).ToHaveClassAsync(HasClass(cls));
        }
        await Expect(Item($"{api}-casc", 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("border-top-color", await ResolveAsync("var(--bit-clr-suc)"));

        var own = Page.Locator($"#{api}-casc-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-tln-err"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-tln-suc"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-tln-otl"));

        var outside = Page.Locator("#items-casc-out");
        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-tln-hrz"));
        await Expect(outside).ToHaveClassAsync(HasClass("bit-tln-fil"));
    }

    [TestMethod]
    public async Task CssVariables_ReskinTheDotsTheLineAndTheItems_AndAnExplicitColorOrSizeWins()
    {
        await OpenAsync("/e2e/timeline");

        await Expect(Page.Locator("#items-vars")).ToHaveCSSAsync("font-size", "17px");
        var dot = Item("items-vars", 0).Locator(".bit-tln-dot");
        await Expect(dot).ToHaveCSSAsync("width", "40px");
        await Expect(dot).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(dot).ToHaveCSSAsync("border-top-color", "rgb(40, 50, 60)");
        await Expect(dot).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(dot).ToHaveCSSAsync("border-top-left-radius", "6px");
        await Expect(dot).ToHaveCSSAsync("box-shadow", "rgb(255, 0, 0) 0px 0px 0px 4px");
        await Expect(Item("items-vars", 0).Locator(".bit-tln-ico")).ToHaveCSSAsync("color", "rgb(70, 80, 90)");
        await Expect(Item("items-vars", 0).Locator(".bit-tln-ico")).ToHaveCSSAsync("font-size", "21px");

        var divider = "#items-vars > li:first-child .bit-tln-dvd";
        StringAssert.Contains(await PseudoAsync(divider, "::after", "background-image"), "rgb(0, 128, 0)");
        Assert.AreEqual("5px", await PseudoAsync(divider, "::after", "width"));
        await Expect(Page.Locator(divider)).ToHaveCSSAsync("padding-left", "13px");
        await Expect(Item("items-vars", 2).Locator(".bit-tln-pcn")).ToHaveCSSAsync("padding-top", "11px");

        var button = Item("items-vars", 0).Locator(".bit-tln-btn");
        await Expect(button).ToHaveCSSAsync("border-top-left-radius", "9px");
        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(100, 110, 120)");
        await Page.Mouse.DownAsync();
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(130, 140, 150)");
        await Page.Mouse.UpAsync();

        // The variables restyle the default, never a choice.
        await Expect(Item("items-vars-color", 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", await ResolveAsync("var(--bit-clr-err)"));
        await Expect(Item("items-vars-size", 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("width", "20px");

        // Outline and Text read the shared dot background, which only Fill's Color would paint over.
        await Expect(Item("items-vars-outline", 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Item("items-vars-text", 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
    }

    [TestMethod]
    public async Task CssVariables_EachVariantReadsItsOwnTierAheadOfTheSharedOne()
    {
        await OpenAsync("/e2e/timeline");

        var cases = new (string Id, string Background, string Border, string Icon)[]
        {
            ("items-tier-fill", "rgb(2, 2, 2)", "rgb(5, 5, 5)", "rgb(8, 8, 8)"),
            ("items-tier-outline", "rgb(3, 3, 3)", "rgb(6, 6, 6)", "rgb(9, 9, 9)"),
            ("items-tier-text", "rgb(4, 4, 4)", "rgb(7, 7, 7)", "rgb(12, 12, 12)"),
        };

        foreach (var (id, background, border, icon) in cases)
        {
            await Expect(Item(id, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("background-color", background);
            await Expect(Item(id, 0).Locator(".bit-tln-dot")).ToHaveCSSAsync("border-top-color", border);
            await Expect(Item(id, 0).Locator(".bit-tln-ico")).ToHaveCSSAsync("color", icon);
        }
    }

    [TestMethod]
    public async Task Runtime_ItemsOptionsAndParametersChangeWithoutAnError()
    {
        await OpenAsync("/e2e/timeline");

        await Expect(Page.Locator("#items-runtime-items > li")).ToHaveCountAsync(3);
        await Page.ClickAsync("#btn-swap-items");
        await Expect(Page.Locator("#items-runtime-items > li .bit-tln-pcn")).ToHaveTextAsync(new[] { "A1", "A2", "A3", "A4" });

        await Expect(Page.Locator("#options-runtime-options > li")).ToHaveCountAsync(2);
        await Page.ClickAsync("#btn-add-option");
        await Expect(Page.Locator("#options-runtime-options > li .bit-tln-pcn")).ToHaveTextAsync(new[] { "Option 1", "Option 2", "Option 3" });
        await Page.ClickAsync("#btn-remove-option");
        await Page.ClickAsync("#btn-remove-option");
        await Expect(Page.Locator("#options-runtime-options > li .bit-tln-pcn")).ToHaveTextAsync(new[] { "Option 1" });

        // The options can be handed over through Options as well as through the child content.
        await Expect(Page.Locator("#options-options-parameter > li .bit-tln-pcn")).ToHaveTextAsync(new[] { "P1", "P2", "P3" });

        await Expect(Page.Locator("#custom-runtime-horizontal")).Not.ToHaveClassAsync(HasClass("bit-tln-hrz"));
        await Page.ClickAsync("#btn-toggle-horizontal");
        await Expect(Page.Locator("#custom-runtime-horizontal")).ToHaveClassAsync(HasClass("bit-tln-hrz"));

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#options-mount-timeline")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#options-mount-timeline > li")).ToHaveCountAsync(3);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private ILocator Item(string id, int index) => Page.Locator($"#{id} > li").Nth(index);

    // Whether the primary text of an item sits after its dot along the reading line (to its right).
    private async Task<bool> PrimaryAfterTheLineAsync(string id, int index)
    {
        var dot = await BoxAsync($"#{id} > li >> nth={index}", ".bit-tln-dot");
        var text = await BoxAsync($"#{id} > li >> nth={index}", ".bit-tln-pcn .bit-tln-ttx");
        return text.X >= dot.Right - Tolerance;
    }

    private async Task<List<Box>> DotsAsync(string id)
    {
        var count = await Page.Locator($"#{id} > li").CountAsync();
        var dots = new List<Box>();
        for (var i = 0; i < count; i++) dots.Add(await BoxAsync($"#{id} > li >> nth={i}", ".bit-tln-dot"));
        return dots;
    }

    private Task<string> PseudoAsync(string selector, string pseudo, string property)
    {
        return Page.EvaluateAsync<string>("([selector, pseudo, property]) => getComputedStyle(document.querySelector(selector), pseudo).getPropertyValue(property)",
                                          new[] { selector, pseudo, property });
    }

    private Task<string> ResolveAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('div');
            probe.style.setProperty('color', value);
            document.body.appendChild(probe);
            const resolved = getComputedStyle(probe).getPropertyValue('color');
            probe.remove();
            return resolved;
        }", value);
    }

    private async Task<Box> BoxAsync(string selector, string? inner = null)
    {
        var locator = Page.Locator(selector);
        if (inner is not null) locator = locator.Locator(inner);
        var box = await locator.First.BoundingBoxAsync();
        Assert.IsNotNull(box, $"{selector} {inner} has no box");
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
