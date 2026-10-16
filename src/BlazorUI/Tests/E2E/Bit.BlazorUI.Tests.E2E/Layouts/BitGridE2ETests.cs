namespace Bit.BlazorUI.Tests.E2E.Layouts;

/// <summary>
/// BitGrid and BitGridItem in a real browser, read off the real layout of 600px grids: Columns (and the default twelve,
/// and --bit-Grid-columns), Span, ColumnSpan, the per breakpoint columns and spans (ColumnsXs..Xxl, Xs..Xxl) by resizing
/// the viewport, Auto / Grow (item and grid) and AutoXs..Xxl / GrowXs..Xxl, Offset / AutoOffset and their breakpoints,
/// Order and OrderXs..Xxl, HorizontalAlign / VerticalAlign / Alignment / AlignContent / AlignSelf for every value,
/// Spacing / HorizontalSpacing / VerticalSpacing with their breakpoints and the --bit-Grid-*spacing variables, hiding a
/// span of zero, MinItemWidth, Container queries, NoWrap and Reversed, nesting, Element / AriaLabel and the list
/// semantics, the BitParams cascade, Style / Class / HtmlAttributes / Visibility / Disabled, RTL, prerendering and
/// unmounting.
/// </summary>
[TestClass]
public class BitGridE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    private static readonly (string Name, int Width)[] Breakpoints =
    [
        ("xs", 500), ("sm", 700), ("md", 1000), ("lg", 1300), ("xl", 2000), ("xxl", 2600)
    ];

    [TestMethod]
    public async Task Prerender_TheGridIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/grid");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"cols-4\"");
        StringAssert.Contains(html, "bit-grd-itm");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Columns_FourColumns_SplitTheRowIntoFourAndWrap()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#cols-4")).ToHaveClassAsync(HasClass("bit-grd"));
        await Expect(Page.Locator("#c4-1")).ToHaveClassAsync(HasClass("bit-grd-itm"));
        for (var i = 1; i <= 4; i++)
        {
            AssertClose(150, (await BoxAsync($"#c4-{i}")).W, $"item {i}");
        }

        var first = await BoxAsync("#c4-1");
        var fifth = await BoxAsync("#c4-5");
        AssertClose(first.X, fifth.X, "item 5 starts the second row");
        AssertClose(first.Bottom, fifth.Y, "the second row");
    }

    [TestMethod]
    public async Task Columns_TheDefault_IsTwelveWithTheThemeSpacing()
    {
        await OpenAsync("/e2e/grid");

        // 600 / 12 minus the share of the eleven 4px gaps.
        AssertClose(600.0 / 12 - 4.0 * 11 / 12, (await BoxAsync("#cd-1")).W, "a twelfth");
        AssertClose(4, (await BoxAsync("#cd-2")).X - (await BoxAsync("#cd-1")).Right, "the default 4px gap");
        AssertClose((await BoxAsync("#cols-default")).Right, (await BoxAsync("#cd-12")).Right, "twelve fill the row");
        AssertClose((await BoxAsync("#cd-1")).Bottom + 4, (await BoxAsync("#cd-13")).Y, "the thirteenth wraps, 4px below");
    }

    [TestMethod]
    public async Task CssVariable_GridColumnsOnAnAncestor_SetsTheColumnsOfAGridThatSetsNone()
    {
        await OpenAsync("/e2e/grid");

        AssertClose(200, (await BoxAsync("#cv-1")).W, "--bit-Grid-columns: 3");
        AssertClose((await BoxAsync("#cv-1")).X, (await BoxAsync("#cv-4")).X, "three per row");
        AssertClose(300, (await BoxAsync("#cvo-1")).W, "an own Columns wins");
    }

    [TestMethod]
    public async Task Span_TheGridsSpan_SizesEveryItem_AndReRendersAtRuntime()
    {
        await OpenAsync("/e2e/grid");

        AssertClose(200, (await BoxAsync("#s4-1")).W, "Span 4 of 12");
        AssertClose((await BoxAsync("#s4-1")).X, (await BoxAsync("#s4-4")).X, "three per row");

        await Page.ClickAsync("#btn-span");

        await Expect(Page.Locator("#span-4")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-grd-span:6"));
        AssertClose(300, (await BoxAsync("#s4-1")).W, "Span 6 of 12");
    }

    [TestMethod]
    public async Task ColumnSpan_PerItem_AndASpanLargerThanTheColumns_TakesAFullRow()
    {
        await OpenAsync("/e2e/grid");

        AssertClose(400, (await BoxAsync("#cs-8")).W, "ColumnSpan 8");
        AssertClose(200, (await BoxAsync("#cs-4")).W, "ColumnSpan 4");
        AssertClose((await BoxAsync("#cs-8")).Y, (await BoxAsync("#cs-4")).Y, "8 + 4 share a row");
        AssertClose(600, (await BoxAsync("#cs-20")).W, "ColumnSpan 20 is capped at a row");
        AssertClose(150, (await BoxAsync("#cs-3")).W, "ColumnSpan 3");
        Assert.IsTrue((await BoxAsync("#cs-3")).Y > (await BoxAsync("#cs-20")).Y, "after the full row");
    }

    [TestMethod]
    public async Task Breakpoints_ColumnsXsToXxl_SetTheColumnsAtEveryWidth()
    {
        await OpenAsync("/e2e/grid");

        var widths = new Dictionary<string, double> { ["xs"] = 600, ["sm"] = 300, ["md"] = 200, ["lg"] = 150, ["xl"] = 120, ["xxl"] = 100 };
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await NextFramesAsync();
            AssertClose(widths[name], (await BoxAsync("#bpc-1")).W, $"{name} ({width}px)");
        }
    }

    [TestMethod]
    public async Task Breakpoints_XsToXxl_SetTheSpanAtEveryWidth_AndAValueCarriesUpwards()
    {
        await OpenAsync("/e2e/grid");

        var widths = new Dictionary<string, double> { ["xs"] = 600, ["sm"] = 300, ["md"] = 200, ["lg"] = 150, ["xl"] = 100, ["xxl"] = 50 };
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await NextFramesAsync();
            AssertClose(widths[name], (await BoxAsync("#bps-1")).W, $"{name} ({width}px)");
        }

        // Columns="2" ColumnsMd="4" and ColumnSpan="2" Md="1": both carried from md up to lg.
        var carry = new (int Width, double Expected)[] { (700, 600), (1000, 150), (1300, 150) };
        foreach (var (width, expected) in carry)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await NextFramesAsync();
            AssertClose(expected, (await BoxAsync("#bpcr-1")).W, $"carry at {width}px");
        }
    }

    [TestMethod]
    public async Task AutoAndGrow_AnAutoItemTakesItsContent_AGrowItemTheRest()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#ag-auto")).ToHaveClassAsync(HasClass("bit-grd-itm-atc"));
        await Expect(Page.Locator("#ag-grow")).ToHaveClassAsync(HasClass("bit-grd-itm-grw"));
        AssertClose(77, (await BoxAsync("#ag-auto")).W, "Auto: its content");
        AssertClose(150, (await BoxAsync("#ag-span")).W, "ColumnSpan 3");
        AssertClose(600 - 77 - 150, (await BoxAsync("#ag-grow")).W, "Grow: what is left");

        AssertClose((600 - 77) / 2.0, (await BoxAsync("#ag2-a")).W, "two growing items share equally");
        AssertClose((600 - 77) / 2.0, (await BoxAsync("#ag2-b")).W, "two growing items share equally");
    }

    [TestMethod]
    public async Task GridGrow_FillsTheShortLastRow_ButLeavesAnAutoItemAlone()
    {
        await OpenAsync("/e2e/grid");

        AssertClose(200, (await BoxAsync("#ngg-5")).W, "without Grow the last row stays short");

        await Expect(Page.Locator("#grid-grow")).ToHaveClassAsync(HasClass("bit-grd-grw"));
        AssertClose(200, (await BoxAsync("#gg-1")).W, "a full row is unchanged");
        AssertClose(77, (await BoxAsync("#gg-auto")).W, "an Auto item keeps its content width");
        AssertClose((600 - 77) / 2.0, (await BoxAsync("#gg-4")).W, "the last row is filled");
        AssertClose((await BoxAsync("#grid-grow")).Right, (await BoxAsync("#gg-auto")).Right, "to the end");
    }

    [TestMethod]
    public async Task AutoAndGrowPerBreakpoint_SwitchTheSizingAtEveryWidth()
    {
        await OpenAsync("/e2e/grid");

        // AutoXs/Md/Xl true and Sm/Lg/Xxl false on a 6-column item; GrowSm/Lg/Xxl true and Xs/Md/Xl false on a 3-column one.
        var auto = new Dictionary<string, double> { ["xs"] = 77, ["sm"] = 300, ["md"] = 77, ["lg"] = 300, ["xl"] = 77, ["xxl"] = 300 };
        var grow = new Dictionary<string, double> { ["xs"] = 150, ["sm"] = 600, ["md"] = 150, ["lg"] = 600, ["xl"] = 150, ["xxl"] = 600 };

        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await NextFramesAsync();
            AssertClose(auto[name], (await BoxAsync("#ag-bp-auto")).W, $"Auto at {name}");
            AssertClose(grow[name], (await BoxAsync("#ag-bp-grow")).W, $"Grow at {name}");
        }
    }

    [TestMethod]
    public async Task Offset_LeavesColumnsEmptyBeforeAnItem_GapsIncluded()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#off-2")).ToHaveClassAsync(HasClass("bit-grd-itm-off"));
        AssertClose(100, (await BoxAsync("#off-2")).X - (await BoxAsync("#off")).X, "Offset 2 of 12");
        // (600 + 12) * 3 / 12: the offset lines the item up with the fourth column, its gap included.
        AssertClose(153, (await BoxAsync("#offg-1")).X - (await BoxAsync("#off-gap")).X, "Offset 3 with 12px gaps");
    }

    [TestMethod]
    public async Task AutoOffset_PushesTheItemToTheEnd_AndWinsOverOffset()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#aoff-2")).ToHaveClassAsync(HasClass("bit-grd-itm-aof"));
        AssertClose((await BoxAsync("#aoff")).X, (await BoxAsync("#aoff-1")).X, "the first item at the start");
        AssertClose((await BoxAsync("#aoff")).Right, (await BoxAsync("#aoff-2")).Right, "the auto offset item at the end");
    }

    [TestMethod]
    public async Task OffsetPerBreakpoint_OffsetXsToXxlAndAutoOffsetXsToXxl()
    {
        await OpenAsync("/e2e/grid");

        var autoOffset = new Dictionary<string, bool> { ["xs"] = true, ["sm"] = false, ["md"] = true, ["lg"] = false, ["xl"] = true, ["xxl"] = false };
        var n = 1;
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await NextFramesAsync();
            AssertClose(50 * n, (await BoxAsync("#offbp-1")).X - (await BoxAsync("#off-bp")).X, $"Offset{name} = {n}");
            AssertClose(autoOffset[name] ? 500 : 0, (await BoxAsync("#aoffbp-1")).X - (await BoxAsync("#aoff-bp")).X, $"AutoOffset at {name}");
            n++;
        }
    }

    [TestMethod]
    public async Task Order_PaintsTheItemsLowestFirst_NegativeIncluded()
    {
        await OpenAsync("/e2e/grid");

        var x = (await BoxAsync("#ord")).X;
        await Expect(Page.Locator("#ord-2")).ToHaveCSSAsync("order", "-1");
        AssertClose(0, (await BoxAsync("#ord-2")).X - x, "order -1 first");
        AssertClose(150, (await BoxAsync("#ord-3")).X - x, "no order second");
        AssertClose(300, (await BoxAsync("#ord-1")).X - x, "order 3 last");

        // The DOM order is untouched.
        var ids = await Page.Locator("#ord > .bit-grd-itm").EvaluateAllAsync<string[]>("els => els.map(e => e.id)");
        CollectionAssert.AreEqual(new[] { "ord-1", "ord-2", "ord-3" }, ids);
    }

    [TestMethod]
    public async Task OrderPerBreakpoint_OrderXsToXxl_ReorderAtEveryWidth()
    {
        await OpenAsync("/e2e/grid");

        var after = new Dictionary<string, bool> { ["xs"] = true, ["sm"] = false, ["md"] = true, ["lg"] = false, ["xl"] = true, ["xxl"] = false };
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await NextFramesAsync();
            var a = await BoxAsync("#ordbp-a");
            var b = await BoxAsync("#ordbp-b");
            Assert.AreEqual(after[name], a.X > b.X, $"{name}: item a painted after b");
        }
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0)]
    [DataRow("End", "flex-end", 300)]
    [DataRow("Center", "center", 150)]
    [DataRow("SpaceBetween", "space-between", 0)]
    [DataRow("SpaceAround", "space-around", 50)]
    [DataRow("SpaceEvenly", "space-evenly", 75)]
    [DataRow("Baseline", "flex-start", 0)]
    [DataRow("Stretch", "flex-start", 0)]
    public async Task HorizontalAlign_EveryValue_DistributesTheWidthTheRowLeaves(string value, string justify, double firstX)
    {
        await OpenAsync("/e2e/grid");

        await SelectAsync("#sel-ha", value, $"ha={value};");

        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("justify-content", justify);
        var grid = await BoxAsync("#al-grid");
        AssertClose(firstX, (await BoxAsync("#al-1")).X - grid.X, $"{value}: first item x");
        if (value == "SpaceBetween") AssertClose(grid.Right, (await BoxAsync("#al-3")).Right, "the last at the end");

        // Baseline and Stretch only make sense across the row, which is where HorizontalAlign hands them.
        if (value is "Baseline" or "Stretch")
        {
            await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("align-items", value.ToLowerInvariant());
        }
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0, 30)]
    [DataRow("End", "flex-end", 70, 30)]
    [DataRow("Center", "center", 35, 30)]
    [DataRow("Stretch", "stretch", 0, 100)]
    [DataRow("Baseline", "baseline", -1, 30)]
    [DataRow("SpaceBetween", "flex-start", 0, 30)]
    [DataRow("SpaceAround", "flex-start", 0, 30)]
    [DataRow("SpaceEvenly", "flex-start", 0, 30)]
    public async Task VerticalAlign_EveryValue_AlignsTheItemsWithinTheirRow(string value, string alignItems, double shortY, double shortHeight)
    {
        await OpenAsync("/e2e/grid");

        await SelectAsync("#sel-va", value, $"va={value};");

        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("align-items", alignItems);
        var grid = await BoxAsync("#al-grid");
        var shortItem = await BoxAsync("#al-2");
        if (shortY >= 0) AssertClose(shortY, shortItem.Y - grid.Y, $"{value}: the short item's y");
        AssertClose(shortHeight, shortItem.H, $"{value}: the short item's height");
    }

    [TestMethod]
    public async Task Alignment_TheShorthand_AlignsBothAxes_AndTheNamedAxesWin()
    {
        await OpenAsync("/e2e/grid");

        await SelectAsync("#sel-al", "Center", "al=Center;");
        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("justify-content", "center");
        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("align-items", "center");
        var grid = await BoxAsync("#al-grid");
        AssertClose(150, (await BoxAsync("#al-1")).X - grid.X, "centered across");
        AssertClose(35, (await BoxAsync("#al-2")).Y - grid.Y, "centered down");

        await SelectAsync("#sel-ha", "End", "ha=End;");
        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("justify-content", "flex-end");
        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("align-items", "center");

        await SelectAsync("#sel-va", "End", "va=End;");
        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("align-items", "flex-end");

        // VerticalAlign wins over a Stretch handed to the cross axis by HorizontalAlign.
        await SelectAsync("#sel-ha", "Stretch", "ha=Stretch;");
        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("align-items", "flex-end");
        await Expect(Page.Locator("#al-grid")).ToHaveCSSAsync("justify-content", "center");
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0, 30)]
    [DataRow("End", "flex-end", 240, 270)]
    [DataRow("Center", "center", 120, 150)]
    [DataRow("SpaceBetween", "space-between", 0, 270)]
    [DataRow("SpaceAround", "space-around", 60, 210)]
    [DataRow("SpaceEvenly", "space-evenly", 80, 190)]
    [DataRow("Stretch", "stretch", 0, 150)]
    [DataRow("Baseline", "baseline", 0, -1)]
    public async Task AlignContent_EveryValue_MovesTheRowsInATallGrid(string value, string alignContent, double firstRowY, double secondRowY)
    {
        await OpenAsync("/e2e/grid");

        await SelectAsync("#sel-ac", value, $"ac={value};");

        await Expect(Page.Locator("#ac-grid")).ToHaveCSSAsync("align-content", alignContent);
        var grid = await BoxAsync("#ac-grid");
        AssertClose(firstRowY, (await BoxAsync("#ac-1")).Y - grid.Y, $"{value}: first row");
        if (secondRowY >= 0) AssertClose(secondRowY, (await BoxAsync("#ac-4")).Y - grid.Y, $"{value}: second row");
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0)]
    [DataRow("End", "flex-end", 70)]
    [DataRow("Center", "center", 35)]
    [DataRow("Stretch", "stretch", 0)]
    [DataRow("Baseline", "baseline", -1)]
    [DataRow("SpaceBetween", "auto", 0)]
    public async Task AlignSelf_EveryValue_OverridesTheRowForOneItem(string value, string alignSelf, double y)
    {
        await OpenAsync("/e2e/grid");

        await SelectAsync("#sel-as", value, $"as={value};");

        await Expect(Page.Locator("#al-2")).ToHaveCSSAsync("align-self", alignSelf);
        var grid = await BoxAsync("#al-grid");
        var item = await BoxAsync("#al-2");
        if (y >= 0) AssertClose(y, item.Y - grid.Y, $"{value}: y");
        if (value == "Stretch") AssertClose(100, item.H, "stretched to the row");
        // The other items keep the grid's alignment.
        AssertClose(0, (await BoxAsync("#al-3")).Y - grid.Y, "a sibling keeps the start");
    }

    [TestMethod]
    public async Task Spacing_TheDefaultAnExplicitAndThePerAxisSpacing_AreTakenOutOfTheItems()
    {
        await OpenAsync("/e2e/grid");

        AssertClose(150 - 4 * 3 / 4.0, (await BoxAsync("#spd-1")).W, "default 4px");
        AssertClose(4, (await BoxAsync("#spd-5")).Y - (await BoxAsync("#spd-1")).Bottom, "default row gap");

        AssertClose(150 - 10 * 3 / 4.0, (await BoxAsync("#sp10-1")).W, "Spacing 10px");
        AssertClose(10, (await BoxAsync("#sp10-2")).X - (await BoxAsync("#sp10-1")).Right, "column gap 10");
        AssertClose(10, (await BoxAsync("#sp10-5")).Y - (await BoxAsync("#sp10-1")).Bottom, "row gap 10");
        AssertClose((await BoxAsync("#sp-10")).Right, (await BoxAsync("#sp10-4")).Right, "the row never overflows");

        // The per axis spacing wins over Spacing.
        AssertClose(150 - 20 * 3 / 4.0, (await BoxAsync("#spa-1")).W, "HorizontalSpacing 20px");
        AssertClose(20, (await BoxAsync("#spa-2")).X - (await BoxAsync("#spa-1")).Right, "column gap 20");
        AssertClose(5, (await BoxAsync("#spa-5")).Y - (await BoxAsync("#spa-1")).Bottom, "row gap 5");

        await Expect(Page.Locator("#sp-number")).ToHaveCSSAsync("column-gap", "8px");
        await Expect(Page.Locator("#sp-negative")).ToHaveCSSAsync("column-gap", "0px");
    }

    [TestMethod]
    public async Task SpacingPerBreakpoint_EveryBreakpointAndEveryAxis()
    {
        await OpenAsync("/e2e/grid");

        var i = 0;
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await Expect(Page.Locator("#sp-bp")).ToHaveCSSAsync("column-gap", $"{1 + i}px", new() { Timeout = 5000 });
            await Expect(Page.Locator("#sp-bp")).ToHaveCSSAsync("row-gap", $"{1 + i}px", new() { Timeout = 5000 });
            await Expect(Page.Locator("#sp-axes-bp")).ToHaveCSSAsync("column-gap", $"{11 + i}px", new() { Timeout = 5000 });
            await Expect(Page.Locator("#sp-axes-bp")).ToHaveCSSAsync("row-gap", $"{21 + i}px", new() { Timeout = 5000 });
            i++;
        }

        // Spacing 0.25rem, SpacingSm 0.75rem, VerticalSpacingMd 2rem.
        var mixed = new (int Width, string Column, string Row)[] { (500, "4px", "4px"), (700, "12px", "12px"), (1000, "12px", "32px"), (1300, "12px", "32px") };
        foreach (var (width, column, row) in mixed)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await Expect(Page.Locator("#sp-mixed")).ToHaveCSSAsync("column-gap", column);
            await Expect(Page.Locator("#sp-mixed")).ToHaveCSSAsync("row-gap", row);
        }
    }

    [TestMethod]
    public async Task CssVariables_TheSpacingVariablesOnAnAncestor_SetTheGutterOfGridsThatSetNone()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#sp-var")).ToHaveCSSAsync("column-gap", "16px");
        await Expect(Page.Locator("#sp-var")).ToHaveCSSAsync("row-gap", "4px");
        await Expect(Page.Locator("#sp-var-own")).ToHaveCSSAsync("column-gap", "2px");
        await Expect(Page.Locator("#sp-var-own")).ToHaveCSSAsync("row-gap", "2px");
        await Expect(Page.Locator("#sp-var-h")).ToHaveCSSAsync("column-gap", "7px");
        await Expect(Page.Locator("#sp-var-h")).ToHaveCSSAsync("row-gap", "4px");
    }

    [TestMethod]
    public async Task Hiding_ASpanOfZero_HidesTheItem_PerBreakpointToo()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#hide-0")).ToHaveClassAsync(HasClass("bit-grd-itm-non"));
        await Expect(Page.Locator("#hide-0")).ToBeHiddenAsync();
        await Expect(Page.Locator("#hide-shown")).ToBeVisibleAsync();

        var shown = new Dictionary<string, bool> { ["xs"] = false, ["sm"] = true, ["md"] = false, ["lg"] = true, ["xl"] = false, ["xxl"] = true };
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            if (shown[name])
            {
                await Expect(Page.Locator("#hide-bp")).ToBeVisibleAsync(new() { Timeout = 5000 });
                AssertClose(300, (await BoxAsync("#hide-bp")).W, $"{name}: shown at 6 columns");
            }
            else
            {
                await Expect(Page.Locator("#hide-bp")).ToBeHiddenAsync(new() { Timeout = 5000 });
            }
        }
    }

    [TestMethod]
    public async Task MinItemWidth_FitsAsManyEqualItemsAsTheWidthAllows()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#fluid-200")).ToHaveClassAsync(HasClass("bit-grd-mnw"));
        AssertClose(200, (await BoxAsync("#fl-1")).W, "three of 200px per row");
        AssertClose((await BoxAsync("#fl-1")).X, (await BoxAsync("#fl-4")).X, "the fourth wraps");
        AssertClose(300, (await BoxAsync("#fl-7")).W, "the last row is shared equally");

        // A bare number is pixels, and a change re-renders.
        await Page.ClickAsync("#btn-min-width");
        await Expect(Page.Locator("#fluid-200")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-grd-mnw:250px"));
        AssertClose(300, (await BoxAsync("#fl-1")).W, "two of 300px per row");
        AssertClose((await BoxAsync("#fl-1")).X, (await BoxAsync("#fl-3")).X, "the third wraps");

        // Auto still applies.
        AssertClose(77, (await BoxAsync("#fla-auto")).W, "an Auto item keeps its content width");
        AssertClose(600 - 77, (await BoxAsync("#fla-1")).W, "the fluid item fills the rest");
    }

    [TestMethod]
    public async Task Container_TheBreakpointsFollowTheGridsOwnWidth()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#cq")).ToHaveClassAsync(HasClass("bit-grd-cnq"));
        await Expect(Page.Locator("#cq")).ToHaveCSSAsync("container-type", "inline-size");

        // A 500px container is an xs one, whatever the window: full width, and the Xs="0" item hidden.
        AssertClose(500, (await BoxAsync("#cq-1")).W, "Xs=12 in a 500px container");
        await Expect(Page.Locator("#cq-hide")).ToBeHiddenAsync();

        // Without Container, the same item follows the 1280px window: Sm=6.
        AssertClose(250, (await BoxAsync("#nocq-1")).W, "Sm=6 by the window");

        // A 700px container is an sm one.
        AssertClose(350, (await BoxAsync("#cqw-1")).W, "Sm=6 in a 700px container");
    }

    [TestMethod]
    public async Task NoWrap_KeepsEveryItemOnOneRow_ShrinkingThemToFit()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#nowrap")).ToHaveCSSAsync("flex-wrap", "nowrap");
        var first = await BoxAsync("#nw-1");
        var sixth = await BoxAsync("#nw-6");
        AssertClose(first.Y, sixth.Y, "one row");
        AssertClose(100, first.W, "six items shrunk into 600px");
        AssertClose((await BoxAsync("#nowrap")).Right, sixth.Right, "no overflow");
    }

    [TestMethod]
    public async Task Reversed_PaintsFromTheEndEdge_AndTheOffsetsFollow()
    {
        await OpenAsync("/e2e/grid");

        await Expect(Page.Locator("#reversed")).ToHaveCSSAsync("flex-direction", "row-reverse");
        var grid = await BoxAsync("#reversed");
        AssertClose(450, (await BoxAsync("#rv-1")).X - grid.X, "item 1 at the end");
        AssertClose(150, (await BoxAsync("#rv-3")).X - grid.X, "item 3 third from the end");

        await Expect(Page.Locator("#rvo-1")).ToHaveCSSAsync("margin-right", "150px");
        AssertClose(300, (await BoxAsync("#rvo-1")).X - (await BoxAsync("#reversed-off")).X, "the offset on the end edge");

        AssertClose((await BoxAsync("#reversed-aoff")).X, (await BoxAsync("#rva-1")).X, "the auto offset pushes it to the start edge");
    }

    [TestMethod]
    public async Task Nesting_AGridInsideAnItem_DividesThatItemsWidth()
    {
        await OpenAsync("/e2e/grid");

        var item = await BoxAsync("#nest-item");
        AssertClose(400, item.W, "the outer span 8");
        AssertClose(50, item.X - (await BoxAsync("#nest-outer")).X, "the outer offset 1");

        AssertClose(200, (await BoxAsync("#nest-a")).W, "half of the item");
        AssertClose(item.X, (await BoxAsync("#nest-a")).X, "the inner items do not inherit the outer offset");
        AssertClose(item.Right, (await BoxAsync("#nest-b")).Right, "the inner row fills the item");
    }

    [TestMethod]
    public async Task Accessibility_AListGrid_RendersListItemsAndItsRole()
    {
        await OpenAsync("/e2e/grid");

        var ul = Page.Locator("#a11y-ul");
        Assert.AreEqual("UL", await ul.EvaluateAsync<string>("el => el.tagName"));
        await Expect(ul).ToHaveAttributeAsync("role", "list");
        await Expect(ul).ToHaveCSSAsync("list-style-type", "none");
        await Expect(ul).ToHaveCSSAsync("padding-left", "0px");
        await Expect(ul).ToHaveCSSAsync("margin-top", "0px");

        var list = Page.GetByRole(AriaRole.List, new() { Name = "Fruits" });
        await Expect(list.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);
        await Expect(list.GetByRole(AriaRole.Listitem, new() { Name = "Cherry fruit" })).ToHaveCountAsync(1);
        var tags = await Page.Locator("#a11y-ul > *").EvaluateAllAsync<string[]>("els => els.map(e => e.tagName)");
        CollectionAssert.AreEqual(new[] { "LI", "LI", "LI" }, tags);

        await Expect(Page.Locator("#a11y-ol")).ToHaveAttributeAsync("role", "list");
        Assert.AreEqual("LI", await Page.Locator("#a11y-ol-item").EvaluateAsync<string>("el => el.tagName"));

        // A named section is a region; an item's Element wins over the default.
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Gallery" })).ToHaveCountAsync(1);
        Assert.AreEqual("ARTICLE", await Page.Locator("#a11y-article").EvaluateAsync<string>("el => el.tagName"));

        Assert.AreEqual("DIV", await Page.Locator("#a11y-div-item").EvaluateAsync<string>("el => el.tagName"));
        Assert.IsNull(await Page.Locator("#a11y-div").GetAttributeAsync("role"));
    }

    [TestMethod]
    public async Task CascadingParameters_GridAndItemParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/grid");

        // Columns 3, Spacing 0 and ColumnSpan 2: one item of 400px per row.
        AssertClose(400, (await BoxAsync("#casc-1")).W, "the cascaded span of the cascaded columns");
        AssertClose((await BoxAsync("#casc-1")).X, (await BoxAsync("#casc-2")).X, "one per row");

        // Own values win, the rest still cascades.
        AssertClose(150, (await BoxAsync("#casc-own-1")).W, "own Columns 4 and ColumnSpan 1, cascaded Spacing 0");

        AssertClose(50, (await BoxAsync("#casc-out-1")).W, "outside: twelve columns again");
    }

    [TestMethod]
    public async Task StyleClassAndAttributes_LandOnTheGridAndItsItems()
    {
        await OpenAsync("/e2e/grid");

        var grid = Page.Locator("#styled");
        await Expect(grid).ToHaveClassAsync(HasClass("grd-custom"));
        await Expect(grid).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await Expect(grid).ToHaveCSSAsync("padding-top", "5px");
        await Expect(grid).ToHaveAttributeAsync("data-test", "grid");
        await Expect(grid).ToHaveAttributeAsync("title", "A grid");
        await Expect(grid).ToHaveAttributeAsync("data-own", "grid");
        await Expect(grid).ToHaveAttributeAsync("tabindex", "0");

        var item = Page.Locator("#styled-item");
        await Expect(item).ToHaveClassAsync(HasClass("grd-item-custom"));
        await Expect(item).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(item).ToHaveCSSAsync("padding-top", "3px");
        await Expect(item).ToHaveAttributeAsync("data-test", "item");
        await item.FocusAsync();
        await Expect(item).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task VisibilityAndDisabled_OfTheGridAndItsItems()
    {
        await OpenAsync("/e2e/grid");

        await Page.ClickAsync("#btn-hidden");
        await Expect(Page.Locator("#vis-item")).ToHaveCSSAsync("visibility", "hidden");
        await Page.ClickAsync("#btn-collapsed");
        await Expect(Page.Locator("#vis-item")).ToHaveCSSAsync("display", "none");

        await Expect(Page.Locator("#disabled-grid")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#disabled-item")).ToHaveClassAsync(HasClass("bit-dis"));
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Page.Locator("#disabled-grid")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#disabled-item")).Not.ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_StartFromTheRight_AndTheOffsetsFollow()
    {
        await OpenAsync("/e2e/grid");

        var grid = Page.Locator("#rtl-grid");
        await Expect(grid).ToHaveAttributeAsync("dir", "rtl");
        await Expect(grid).ToHaveClassAsync(HasClass("bit-rtl"));
        var box = await BoxAsync("#rtl-grid");
        AssertClose(box.Right, (await BoxAsync("#rtl-1")).Right, "item 1 at the right");
        AssertClose(box.Right - 150, (await BoxAsync("#rtl-2")).Right, "item 2 to its left");

        await Expect(Page.Locator("#rtlo-1")).ToHaveCSSAsync("margin-right", "150px");
        AssertClose((await BoxAsync("#rtl-off")).Right - 150, (await BoxAsync("#rtlo-1")).Right, "the offset from the right");

        AssertClose((await BoxAsync("#rtl-inherited")).Right, (await BoxAsync("#rtli-1")).Right, "inherited: item 1 at the right");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAndMountingAgain_RaisesNoError()
    {
        await OpenAsync("/e2e/grid");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-grid")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#mount-item")).ToBeVisibleAsync();
        AssertClose(300, (await BoxAsync("#mount-item")).W, "mounted again with its layout");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task SelectAsync(string select, string value, string expectedState)
    {
        await Page.SelectOptionAsync(select, value);
        await Expect(Page.Locator("#al-state")).ToContainTextAsync(expectedState);
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
