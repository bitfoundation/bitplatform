namespace Bit.BlazorUI.Tests.E2E.Layouts;

/// <summary>
/// BitStack in a real browser, read off the real layout: the direction (column, Horizontal, Reversed), every value of
/// HorizontalAlign / VerticalAlign / Alignment on a row and on a column, Wrap / WrapReverse / AlignContent, the per
/// breakpoint chains (HorizontalXs..Xxl, WrapXs..Xxl, GapXs..Xxl, HorizontalGapXs..Xxl, VerticalGapXs..Xxl) by resizing
/// the viewport, Gap / HorizontalGap / VerticalGap / Size / --bit-Stack-gap, the content sizing switches (FillContent,
/// GrowContent, EqualContent, NoShrinkContent), the Auto* / Fit* sizing, a stack as a flex item (Grow, Grows, Basis,
/// Shrink, NoShrink, Shrinkable, Self, Order), Padding, Element / AriaLabel / Inline, the BitParams cascade, nesting,
/// Style / Class / HtmlAttributes / hidden / Visibility / TabIndex / Disabled, RTL, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitStackE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    // The breakpoints of the stylesheet and one viewport width inside each band.
    private static readonly (string Name, int Width)[] Breakpoints =
    [
        ("xs", 500), ("sm", 700), ("md", 1000), ("lg", 1300), ("xl", 2000), ("xxl", 2600)
    ];

    [TestMethod]
    public async Task Prerender_TheStackIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/stack");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"basic-row\"");
        StringAssert.Contains(html, "flex-direction:row");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_TheDirections_LayTheChildrenOutAsAColumnARowAndReversed()
    {
        await OpenAsync("/e2e/stack");

        // A column by default.
        await Expect(Page.Locator("#basic-col")).ToHaveCSSAsync("display", "flex");
        await Expect(Page.Locator("#basic-col")).ToHaveCSSAsync("flex-direction", "column");
        AssertClose((await BoxAsync("#bc-1")).Bottom, (await BoxAsync("#bc-2")).Y, "column: 2 under 1");
        AssertClose((await BoxAsync("#bc-1")).X, (await BoxAsync("#bc-2")).X, "column: same x");

        // Horizontal makes it a row.
        await Expect(Page.Locator("#basic-row")).ToHaveCSSAsync("flex-direction", "row");
        AssertClose((await BoxAsync("#br-1")).Right, (await BoxAsync("#br-2")).X, "row: 2 right of 1");

        // Reversed flips the painted order and packs from the far edge.
        await Expect(Page.Locator("#basic-col-rev")).ToHaveCSSAsync("flex-direction", "column-reverse");
        AssertClose((await BoxAsync("#basic-col-rev")).Bottom, (await BoxAsync("#bcr-1")).Bottom, "column-reverse: 1 at the bottom");
        Assert.IsTrue((await BoxAsync("#bcr-3")).Y < (await BoxAsync("#bcr-1")).Y, "column-reverse: 3 above 1");

        await Expect(Page.Locator("#basic-row-rev")).ToHaveCSSAsync("flex-direction", "row-reverse");
        AssertClose((await BoxAsync("#basic-row-rev")).Right, (await BoxAsync("#brr-1")).Right, "row-reverse: 1 at the right");
        Assert.IsTrue((await BoxAsync("#brr-3")).X < (await BoxAsync("#brr-1")).X, "row-reverse: 3 left of 1");

        // The source order is untouched.
        Assert.AreEqual("123", await Page.Locator("#basic-row-rev").InnerTextAsync().ContinueWith(t => t.Result.Replace("\n", "").Replace(" ", "")));
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0)]
    [DataRow("End", "flex-end", 220)]
    [DataRow("Center", "center", 110)]
    [DataRow("SpaceBetween", "space-between", 0)]
    [DataRow("SpaceAround", "space-around", 36.67)]
    [DataRow("SpaceEvenly", "space-evenly", 55)]
    [DataRow("Baseline", "flex-start", 0)]
    [DataRow("Stretch", "flex-start", 0)]
    public async Task Alignment_HorizontalAlignOnARow_PlacesTheChildrenAlongIt(string value, string justify, double firstX)
    {
        await OpenAsync("/e2e/stack");

        await SelectAsync("#sel-ha", value, "#al-state", $"ha={value};");

        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("justify-content", justify);
        var host = await BoxAsync("#al-host");
        AssertClose(firstX, (await BoxAsync("#al-1")).X - host.X, $"{value}: first item x");
        if (value == "SpaceBetween") AssertClose(host.Right, (await BoxAsync("#al-3")).Right, "SpaceBetween: last item at the end");
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0)]
    [DataRow("End", "flex-end", 210)]
    [DataRow("Center", "center", 105)]
    [DataRow("SpaceBetween", "flex-start", 0)]
    [DataRow("SpaceAround", "flex-start", 0)]
    [DataRow("SpaceEvenly", "flex-start", 0)]
    [DataRow("Baseline", "baseline", 0)]
    [DataRow("Stretch", "stretch", 0)]
    public async Task Alignment_VerticalAlignOnARow_PlacesTheChildrenAcrossIt(string value, string alignItems, double firstY)
    {
        await OpenAsync("/e2e/stack");

        await SelectAsync("#sel-va", value, "#al-state", $"va={value};");

        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("align-items", alignItems);
        var host = await BoxAsync("#al-host");
        var first = await BoxAsync("#al-1");
        AssertClose(firstY, first.Y - host.Y, $"{value}: first item y");
        AssertClose(value == "Stretch" ? 240 : 30, first.H, $"{value}: item height");
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0)]
    [DataRow("End", "flex-end", 340)]
    [DataRow("Center", "center", 170)]
    [DataRow("SpaceBetween", "flex-start", 0)]
    [DataRow("SpaceAround", "flex-start", 0)]
    [DataRow("SpaceEvenly", "flex-start", 0)]
    [DataRow("Baseline", "baseline", 0)]
    [DataRow("Stretch", "stretch", 0)]
    public async Task Alignment_HorizontalAlignOnAColumn_PlacesTheChildrenAcrossIt(string value, string alignItems, double firstX)
    {
        await OpenAsync("/e2e/stack");

        await Page.ClickAsync("#btn-al-horizontal");
        await Expect(Page.Locator("#al-state")).ToContainTextAsync("h=False");
        await SelectAsync("#sel-ha", value, "#al-state", $"ha={value};");

        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("flex-direction", "column");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("align-items", alignItems);
        var host = await BoxAsync("#al-host");
        var first = await BoxAsync("#al-1");
        AssertClose(firstX, first.X - host.X, $"{value}: first item x");
        AssertClose(value == "Stretch" ? 400 : 60, first.W, $"{value}: item width");
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0)]
    [DataRow("End", "flex-end", 150)]
    [DataRow("Center", "center", 75)]
    [DataRow("SpaceBetween", "space-between", 0)]
    [DataRow("SpaceAround", "space-around", 25)]
    [DataRow("SpaceEvenly", "space-evenly", 37.5)]
    [DataRow("Baseline", "flex-start", 0)]
    [DataRow("Stretch", "flex-start", 0)]
    public async Task Alignment_VerticalAlignOnAColumn_PlacesTheChildrenAlongIt(string value, string justify, double firstY)
    {
        await OpenAsync("/e2e/stack");

        await Page.ClickAsync("#btn-al-horizontal");
        await Expect(Page.Locator("#al-state")).ToContainTextAsync("h=False");
        await SelectAsync("#sel-va", value, "#al-state", $"va={value};");

        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("justify-content", justify);
        var host = await BoxAsync("#al-host");
        AssertClose(firstY, (await BoxAsync("#al-1")).Y - host.Y, $"{value}: first item y");
        if (value == "SpaceBetween") AssertClose(host.Bottom, (await BoxAsync("#al-3")).Bottom, "SpaceBetween: last at the bottom");
    }

    [TestMethod]
    public async Task Alignment_TheShorthand_AlignsBothAxesAndTheNamedAxesWinOverIt()
    {
        await OpenAsync("/e2e/stack");

        var host = await BoxAsync("#al-host");

        // Center on both axes.
        await SelectAsync("#sel-al", "Center", "#al-state", "al=Center;");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("justify-content", "center");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("align-items", "center");
        var first = await BoxAsync("#al-1");
        AssertClose(110, first.X - host.X, "centered x");
        AssertClose(105, first.Y - host.Y, "centered y");

        // A named axis wins over the shorthand on that axis only.
        await SelectAsync("#sel-ha", "End", "#al-state", "ha=End;");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("justify-content", "flex-end");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("align-items", "center");

        // A named value that means nothing on its axis falls back to the shorthand.
        await SelectAsync("#sel-ha", "Stretch", "#al-state", "ha=Stretch;");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("justify-content", "center");

        // A shorthand value meaning nothing on an axis leaves it at the start.
        await SelectAsync("#sel-ha", "", "#al-state", "ha=;");
        await SelectAsync("#sel-al", "SpaceBetween", "#al-state", "al=SpaceBetween;");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("justify-content", "space-between");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("align-items", "flex-start");
    }

    [TestMethod]
    public async Task Alignment_ReversedRow_MovesTheStartEdgeWithTheDirection()
    {
        await OpenAsync("/e2e/stack");

        var host = await BoxAsync("#al-host");

        await Page.ClickAsync("#btn-al-reversed");
        await Expect(Page.Locator("#al-state")).ToContainTextAsync("r=True");
        await Expect(Page.Locator("#al-stack")).ToHaveCSSAsync("flex-direction", "row-reverse");
        AssertClose(host.Right, (await BoxAsync("#al-1")).Right, "row-reverse, start: item 1 at the right");

        await SelectAsync("#sel-ha", "End", "#al-state", "ha=End;");
        AssertClose(host.X, (await BoxAsync("#al-3")).X, "row-reverse, end: item 3 at the left");
        AssertClose(120, (await BoxAsync("#al-1")).X - host.X, "row-reverse, end: item 1 third from the left");
    }

    [TestMethod]
    public async Task Wrap_AHorizontalStack_BreaksIntoRows()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#wrap-stack")).ToHaveCSSAsync("flex-wrap", "wrap");
        var one = await BoxAsync("#wr-1");
        var four = await BoxAsync("#wr-4");
        AssertClose(one.X, four.X, "item 4 starts the second row");
        Assert.IsTrue(four.Y > one.Bottom, "the second row is below the first");
        AssertClose(one.Y, (await BoxAsync("#wr-3")).Y, "three items per row");

        // Without Wrap the children stay on one line and overflow.
        await Expect(Page.Locator("#nowrap-stack")).ToHaveCSSAsync("flex-wrap", "nowrap");
        AssertClose((await BoxAsync("#nw-1")).Y, (await BoxAsync("#nw-6")).Y, "one line");
    }

    [TestMethod]
    public async Task Wrap_AVerticalStackOfFixedHeight_StartsAnotherColumn()
    {
        await OpenAsync("/e2e/stack");

        var one = await BoxAsync("#vw-1");
        var three = await BoxAsync("#vw-3");

        AssertClose(one.Y, three.Y, "item 3 starts the second column");
        AssertClose(one.Right + 10, three.X, "the second column is a gap to the right");
    }

    [TestMethod]
    [DataRow("Start", "flex-start", 0, 40)]
    [DataRow("End", "flex-end", 230, 270)]
    [DataRow("Center", "center", 115, 155)]
    [DataRow("SpaceBetween", "space-between", 0, 270)]
    [DataRow("SpaceAround", "space-around", 57.5, 212.5)]
    [DataRow("SpaceEvenly", "space-evenly", 76.67, 193.33)]
    [DataRow("Stretch", "stretch", 0, 155)]
    [DataRow("Baseline", "normal", 0, 155)]
    public async Task AlignContent_EveryValue_PlacesTheRowsOfAWrappingStack(string value, string alignContent, double firstRowY, double secondRowY)
    {
        await OpenAsync("/e2e/stack");

        await SelectAsync("#sel-ac", value, "#wrap-state", $"ac={value};");

        await Expect(Page.Locator("#wrap-stack")).ToHaveCSSAsync("align-content", alignContent);
        var host = await BoxAsync("#wrap-host");
        AssertClose(firstRowY, (await BoxAsync("#wr-1")).Y - host.Y, $"{value}: first row y");

        // Two rows of 30px and a 10px gap leave 230px to place; Stretch hands each line half of it, and so does Baseline,
        // which is dropped and so stretches like the flex default.
        AssertClose(secondRowY, (await BoxAsync("#wr-4")).Y - host.Y, $"{value}: second row y");
    }

    [TestMethod]
    public async Task WrapReverse_StacksTheRowsFromTheFarEdge()
    {
        await OpenAsync("/e2e/stack");

        await SelectAsync("#sel-ac", "Start", "#wrap-state", "ac=Start;");
        await Page.ClickAsync("#btn-wrap-reverse");
        await Expect(Page.Locator("#wrap-state")).ToHaveTextAsync("ac=Start;wr=True");

        await Expect(Page.Locator("#wrap-stack")).ToHaveCSSAsync("flex-wrap", "wrap-reverse");
        var host = await BoxAsync("#wrap-host");
        AssertClose(host.Bottom, (await BoxAsync("#wr-1")).Bottom, "the first row at the bottom");
        Assert.IsTrue((await BoxAsync("#wr-4")).Bottom < (await BoxAsync("#wr-1")).Y, "the second row above it");
    }

    [TestMethod]
    public async Task WrapPerBreakpoint_EveryBreakpoint_TurnsWrappingOnAndOff()
    {
        await OpenAsync("/e2e/stack");

        // WrapXs=false, WrapSm=true, WrapMd=false, WrapLg=true, WrapXl=false, WrapXxl=true.
        var expected = new Dictionary<string, bool> { ["xs"] = false, ["sm"] = true, ["md"] = false, ["lg"] = true, ["xl"] = false, ["xxl"] = true };

        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            var wraps = expected[name];

            await Expect(Page.Locator("#wrap-bp")).ToHaveCSSAsync("flex-wrap", wraps ? "wrap" : "nowrap");
            var one = await BoxAsync("#wbp-1");
            var three = await BoxAsync("#wbp-3");
            Assert.AreEqual(wraps, three.Y > one.Bottom - Tolerance, $"{name} ({width}px): item 3 on a second row");
        }
    }

    [TestMethod]
    public async Task WrapPerBreakpoint_AValueCarriesUpwards_AndWrapReverseKeepsItsWayRound()
    {
        await OpenAsync("/e2e/stack");

        var cases = new (int Width, string MdOff, string ReverseMdOff)[]
        {
            (700, "wrap", "wrap-reverse"),
            (1000, "nowrap", "nowrap"),
            (1300, "nowrap", "nowrap"),
        };

        foreach (var (width, mdOff, reverse) in cases)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await Expect(Page.Locator("#wrap-md-off")).ToHaveCSSAsync("flex-wrap", mdOff);
            await Expect(Page.Locator("#wrap-reverse-bp")).ToHaveCSSAsync("flex-wrap", reverse);
        }
    }

    [TestMethod]
    public async Task Gap_TheDefaultAndAnExplicitGap_SpaceTheChildren()
    {
        await OpenAsync("/e2e/stack");

        AssertClose(16, (await BoxAsync("#gd-2")).Y - (await BoxAsync("#gd-1")).Bottom, "the default gap is spacing(2)");
        AssertClose(32, (await BoxAsync("#g2-2")).Y - (await BoxAsync("#g2-1")).Bottom, "Gap=2rem");

        // Changed at runtime.
        await Page.ClickAsync("#btn-gap");
        await Expect(Page.Locator("#gap-2rem")).ToHaveCSSAsync("row-gap", "5px");
        AssertClose(5, (await BoxAsync("#g2-2")).Y - (await BoxAsync("#g2-1")).Bottom, "Gap=5px");
    }

    [TestMethod]
    public async Task Gap_TwoLengthsAndThePerAxisGaps_SpaceRowsAndColumnsApart()
    {
        await OpenAsync("/e2e/stack");

        // "10px 30px" is the row gap then the column gap.
        AssertClose(30, (await BoxAsync("#gt-2")).X - (await BoxAsync("#gt-1")).Right, "column gap 30px");
        AssertClose(10, (await BoxAsync("#gt-3")).Y - (await BoxAsync("#gt-1")).Bottom, "row gap 10px");

        AssertClose(32, (await BoxAsync("#ga-2")).X - (await BoxAsync("#ga-1")).Right, "HorizontalGap 2rem");
        AssertClose(4, (await BoxAsync("#ga-3")).Y - (await BoxAsync("#ga-1")).Bottom, "VerticalGap 0.25rem");
    }

    [TestMethod]
    public async Task CssVariable_StackGapOnAnAncestor_IsTheDefaultGapOfTheStacksInside()
    {
        await OpenAsync("/e2e/stack");

        AssertClose(6, (await BoxAsync("#gv-2")).Y - (await BoxAsync("#gv-1")).Bottom, "--bit-Stack-gap: 6px");
        AssertClose(20, (await BoxAsync("#gvo-2")).Y - (await BoxAsync("#gvo-1")).Bottom, "an own Gap wins");
    }

    [TestMethod]
    public async Task Responsive_HorizontalMdAndHorizontalLgFalse_ChangeTheDirectionAtTheirBreakpoints()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#rsp-md")).ToHaveClassAsync(HasClass("bit-stc-rsp"));

        await Page.SetViewportSizeAsync(800, 900);
        await Expect(Page.Locator("#rsp-md")).ToHaveCSSAsync("flex-direction", "column");
        AssertClose((await BoxAsync("#rm-1")).Bottom, (await BoxAsync("#rm-2")).Y, "a column below md");
        await Expect(Page.Locator("#rsp-lg-off")).ToHaveCSSAsync("flex-direction", "row");

        await Page.SetViewportSizeAsync(1000, 900);
        await Expect(Page.Locator("#rsp-md")).ToHaveCSSAsync("flex-direction", "row");
        AssertClose((await BoxAsync("#rm-1")).Right, (await BoxAsync("#rm-2")).X, "a row from md");
        await Expect(Page.Locator("#rsp-lg-off")).ToHaveCSSAsync("flex-direction", "row");

        await Page.SetViewportSizeAsync(1300, 900);
        await Expect(Page.Locator("#rsp-md")).ToHaveCSSAsync("flex-direction", "row");
        await Expect(Page.Locator("#rsp-lg-off")).ToHaveCSSAsync("flex-direction", "column");
    }

    [TestMethod]
    public async Task Responsive_EveryHorizontalBreakpoint_SetsTheDirectionAndReversedHolds()
    {
        await OpenAsync("/e2e/stack");

        // HorizontalXs=true, Sm=false, Md=true, Lg=false, Xl=true, Xxl=false, all reversed.
        var expected = new Dictionary<string, string>
        {
            ["xs"] = "row-reverse", ["sm"] = "column-reverse", ["md"] = "row-reverse",
            ["lg"] = "column-reverse", ["xl"] = "row-reverse", ["xxl"] = "column-reverse"
        };

        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await Expect(Page.Locator("#rsp-all")).ToHaveCSSAsync("flex-direction", expected[name], new() { Timeout = 5000 });
        }
    }

    [TestMethod]
    public async Task Responsive_TheAlignments_FollowTheDirection()
    {
        await OpenAsync("/e2e/stack");

        // A row from md: SpaceBetween spreads it, Center centers it vertically.
        await Page.SetViewportSizeAsync(1000, 900);
        await Expect(Page.Locator("#rsp-align")).ToHaveCSSAsync("justify-content", "space-between");
        await Expect(Page.Locator("#rsp-align")).ToHaveCSSAsync("align-items", "center");
        var host = await BoxAsync("#rsp-align");
        AssertClose(host.X, (await BoxAsync("#ra-1")).X, "row: first at the start");
        AssertClose(host.Right, (await BoxAsync("#ra-3")).Right, "row: last at the end");
        AssertClose(85, (await BoxAsync("#ra-1")).Y - host.Y, "row: centered vertically");

        // A column below md: Center centers it along the column, SpaceBetween means nothing across it.
        await Page.SetViewportSizeAsync(700, 900);
        await Expect(Page.Locator("#rsp-align")).ToHaveCSSAsync("justify-content", "center");
        await Expect(Page.Locator("#rsp-align")).ToHaveCSSAsync("align-items", "flex-start");
        host = await BoxAsync("#rsp-align");
        AssertClose(55, (await BoxAsync("#ra-1")).Y - host.Y, "column: centered along it");
        AssertClose(host.X, (await BoxAsync("#ra-1")).X, "column: at the start across it");
    }

    [TestMethod]
    public async Task Responsive_GapPerBreakpoint_ChangesTheSpacingWithTheWindow()
    {
        await OpenAsync("/e2e/stack");

        await Page.SetViewportSizeAsync(700, 900);
        await Expect(Page.Locator("#rsp-gap-md")).ToHaveCSSAsync("row-gap", "4px");
        await Page.SetViewportSizeAsync(1000, 900);
        await Expect(Page.Locator("#rsp-gap-md")).ToHaveCSSAsync("column-gap", "48px");

        await Expect(Page.Locator("#rsp-gaps")).ToHaveClassAsync(HasClass("bit-stc-rsg"));
        var px = 1;
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await Expect(Page.Locator("#rsp-gaps")).ToHaveCSSAsync("row-gap", $"{px}px", new() { Timeout = 5000 });
            px++;
        }
    }

    [TestMethod]
    public async Task Responsive_HorizontalAndVerticalGapPerBreakpoint_ChangeOneAxisEach()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#rsp-axis-gaps")).ToHaveClassAsync(HasClass("bit-stc-rshg"));
        await Expect(Page.Locator("#rsp-axis-gaps")).ToHaveClassAsync(HasClass("bit-stc-rsvg"));

        var i = 0;
        foreach (var (name, width) in Breakpoints)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await Expect(Page.Locator("#rsp-axis-gaps")).ToHaveCSSAsync("column-gap", $"{11 + i}px", new() { Timeout = 5000 });
            await Expect(Page.Locator("#rsp-axis-gaps")).ToHaveCSSAsync("row-gap", $"{21 + i}px", new() { Timeout = 5000 });
            i++;
        }

        // An axis chain over the shorthand: the rows keep the Gap, the columns open at md and lg.
        var chain = new (int Width, string Column)[] { (700, "8px"), (1000, "48px"), (1300, "80px"), (2000, "80px") };
        foreach (var (width, column) in chain)
        {
            await Page.SetViewportSizeAsync(width, 900);
            await Expect(Page.Locator("#rsp-hgap-chain")).ToHaveCSSAsync("column-gap", column);
            await Expect(Page.Locator("#rsp-hgap-chain")).ToHaveCSSAsync("row-gap", "8px");
        }
    }

    [TestMethod]
    public async Task Nesting_EachStack_KeepsItsOwnDirection()
    {
        await OpenAsync("/e2e/stack");

        AssertClose((await BoxAsync("#nest-1")).Right, (await BoxAsync("#nest-col")).X, "the outer row");
        AssertClose((await BoxAsync("#nest-c1")).Bottom, (await BoxAsync("#nest-c2")).Y, "the inner column");
        AssertClose((await BoxAsync("#nest-r1")).Right, (await BoxAsync("#nest-r2")).X, "the inner row");

        // A stack inside a responsive one is laid out by its own direction, never its parent's.
        await Page.SetViewportSizeAsync(1000, 900);
        await Expect(Page.Locator("#nest-rsp")).ToHaveCSSAsync("flex-direction", "row");
        await Expect(Page.Locator("#nest-rsp-inner")).ToHaveCSSAsync("flex-direction", "column");
        await Expect(Page.Locator("#nest-rsp-inner")).ToHaveCSSAsync("align-items", "flex-start");
    }

    [TestMethod]
    public async Task ContentSizing_FillContent_StretchesTheChildrenAcrossTheStack()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#fill-v")).ToHaveClassAsync(HasClass("bit-stc-fcv"));
        AssertClose(300, (await BoxAsync("#fv-1")).W, "a column: full width");

        await Expect(Page.Locator("#fill-h")).ToHaveClassAsync(HasClass("bit-stc-fch"));
        AssertClose(100, (await BoxAsync("#fh-1")).H, "a row: full height");

        // A wrapping stack stretches a child into its own row instead.
        await Expect(Page.Locator("#fill-r")).ToHaveClassAsync(HasClass("bit-stc-fcr"));
        await Expect(Page.Locator("#fr-1")).ToHaveCSSAsync("align-self", "stretch");
        AssertClose(50, (await BoxAsync("#fr-1")).H, "the height of its row");
    }

    [TestMethod]
    public async Task ContentSizing_GrowAndEqualContent_ShareTheMainAxis()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#grow-content")).ToHaveClassAsync(HasClass("bit-stc-grc"));
        var g1 = await BoxAsync("#gc-1");
        var g2 = await BoxAsync("#gc-2");
        AssertClose((await BoxAsync("#grow-content")).Right, (await BoxAsync("#gc-3")).Right, "the row is filled");
        Assert.IsTrue(g1.W > 60 + Tolerance, "the children grew");
        Assert.IsTrue(g2.W > g1.W + Tolerance, "the longer child stays longer");

        await Expect(Page.Locator("#equal-content")).ToHaveClassAsync(HasClass("bit-stc-eqc"));
        foreach (var id in new[] { "#ec-1", "#ec-2", "#ec-3" })
        {
            AssertClose(200, (await BoxAsync(id)).W, $"{id}: an exact third");
        }
    }

    [TestMethod]
    public async Task ContentSizing_NoShrinkContent_KeepsTheChildrenAtTheirSizeAndOverflows()
    {
        await OpenAsync("/e2e/stack");

        var squeezedHost = await BoxAsync("#squeezed");
        Assert.IsTrue((await BoxAsync("#sq-8")).Right <= squeezedHost.Right + Tolerance, "squeezed to fit");
        Assert.IsTrue((await BoxAsync("#sq-1")).W < 50, "squeezed below 50px");

        await Expect(Page.Locator("#no-shrink-content")).ToHaveClassAsync(HasClass("bit-stc-nsc"));
        AssertClose(50, (await BoxAsync("#ns-1")).W, "kept at 50px");
        Assert.IsTrue((await BoxAsync("#ns-8")).Right > (await BoxAsync("#no-shrink-content")).Right, "overflows");
    }

    [TestMethod]
    public async Task Sizing_TheDefaultFillsTheContainer_AutoHandsItBackToTheContent()
    {
        await OpenAsync("/e2e/stack");

        var host = (await BoxAsync("#basic-col"));
        AssertClose(400, host.W, "default width 100%");
        AssertClose(240, host.H, "default height 100%");

        AssertClose(70, (await BoxAsync("#sz-auto-width")).W, "AutoWidth");
        AssertClose(160, (await BoxAsync("#sz-auto-width")).H, "AutoWidth keeps the full height");
        AssertClose(40, (await BoxAsync("#sz-auto-height")).H, "AutoHeight");
        var autoSize = await BoxAsync("#sz-auto-size");
        AssertClose(75, autoSize.W, "AutoSize width");
        AssertClose(45, autoSize.H, "AutoSize height");
    }

    [TestMethod]
    public async Task Sizing_FitHoldsInsideAParentThatStretches_AutoDoesNot()
    {
        await OpenAsync("/e2e/stack");

        AssertClose(160, (await BoxAsync("#st-auto-height")).H, "AutoHeight is stretched by the row");
        AssertClose(40, (await BoxAsync("#st-fit-height")).H, "FitHeight holds");
        await Expect(Page.Locator("#st-fit-height")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("height:fit-content"));
        var fitSize = await BoxAsync("#st-fit-size");
        AssertClose(85, fitSize.W, "FitSize width");
        AssertClose(40, fitSize.H, "FitSize height");

        AssertClose(400, (await BoxAsync("#st-auto-width")).W, "AutoWidth is stretched by the column");
        AssertClose(70, (await BoxAsync("#st-fit-width")).W, "FitWidth holds");
    }

    [TestMethod]
    public async Task FlexItem_GrowGrowsAndBasis_ShareTheRoomOfTheParent()
    {
        await OpenAsync("/e2e/stack");

        AssertClose(300, (await BoxAsync("#fi-grow3")).W, "Grow 3");
        AssertClose(200, (await BoxAsync("#fi-grow2")).W, "Grow 2");
        AssertClose(100, (await BoxAsync("#fi-grows")).W, "Grows");
        await Expect(Page.Locator("#fi-grow3")).ToHaveCSSAsync("flex-grow", "3");
        await Expect(Page.Locator("#fi-grows")).ToHaveCSSAsync("flex-grow", "1");
        await Expect(Page.Locator("#fi-grows")).ToHaveCSSAsync("flex-basis", "0px");

        // Grows alone: sized by what they hold plus an equal share; with Basis 0: equal.
        var g1 = await BoxAsync("#fi-g1");
        var g2 = await BoxAsync("#fi-g2");
        var g3 = await BoxAsync("#fi-g3");
        AssertClose(600, g1.W + g2.W + g3.W, "the row is filled");
        Assert.IsTrue(g2.W > g1.W + 20, "the longer content gets more");

        foreach (var id in new[] { "#fi-b1", "#fi-b2", "#fi-b3" })
        {
            AssertClose(200, (await BoxAsync(id)).W, $"{id}: Basis 0 gives equal columns");
        }
    }

    [TestMethod]
    public async Task FlexItem_ShrinkNoShrinkAndShrinkable_ControlTheSqueezing()
    {
        await OpenAsync("/e2e/stack");

        // 600px of children in 400px: the 200px missing is given up by the shrink factors 3, 1 and 0.
        AssertClose(50, (await BoxAsync("#fi-shrink3")).W, "Shrink 3");
        AssertClose(150, (await BoxAsync("#fi-shrink-default")).W, "the default shrink of 1");
        AssertClose(200, (await BoxAsync("#fi-no-shrink")).W, "NoShrink");
        await Expect(Page.Locator("#fi-no-shrink")).ToHaveCSSAsync("flex-shrink", "0");
        await Expect(Page.Locator("#fi-shrink3")).ToHaveCSSAsync("flex-shrink", "3");

        // Without Shrinkable the nested stack keeps the floor of its content; with it, it shrinks into the row.
        Assert.IsTrue((await BoxAsync("#fi-not-shrinkable")).W >= 600 - Tolerance, "the content floor holds");
        await Expect(Page.Locator("#fi-shrinkable")).ToHaveCSSAsync("min-width", "0px");
        AssertClose(220, (await BoxAsync("#fi-shrinkable")).W, "Shrinkable shrinks to what is left");
        Assert.IsTrue((await BoxAsync("#fi-shrinkable-fixed")).Right <= 300 + (await BoxAsync("#fi-shrinkable")).X + Tolerance);
    }

    [TestMethod]
    public async Task FlexItem_SelfAndOrder_OverrideTheParent()
    {
        await OpenAsync("/e2e/stack");

        var host = await BoxAsync("#fi-self-host");

        await Expect(Page.Locator("#fi-self-start")).ToHaveCSSAsync("align-self", "flex-start");
        AssertClose(host.Y, (await BoxAsync("#fi-self-start")).Y, "Self Start at the top");

        var centered = await BoxAsync("#fi-self-none");
        AssertClose(host.Y + (host.H - centered.H) / 2, centered.Y, "the parent's center");

        await Expect(Page.Locator("#fi-self-end")).ToHaveCSSAsync("align-self", "flex-end");
        AssertClose(host.Bottom, (await BoxAsync("#fi-self-end")).Bottom, "Self End at the bottom");

        await Expect(Page.Locator("#fi-self-stretch")).ToHaveCSSAsync("align-self", "stretch");
        AssertClose(host.H, (await BoxAsync("#fi-self-stretch")).H, "Self Stretch fills the height");

        await Expect(Page.Locator("#fi-self-baseline")).ToHaveCSSAsync("align-self", "baseline");

        // A distribution means nothing for a single child: dropped.
        await Expect(Page.Locator("#fi-self-between")).ToHaveCSSAsync("align-self", "auto");

        // Order -1, written last, is painted first.
        await Expect(Page.Locator("#fi-order")).ToHaveCSSAsync("order", "-1");
        AssertClose(host.X, (await BoxAsync("#fi-order")).X, "Order -1 first");
    }

    [TestMethod]
    public async Task Padding_TheShorthand_InsetsTheChildrenWithoutGrowingTheStack()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#pad-1rem")).ToHaveCSSAsync("padding-top", "16px");
        await Expect(Page.Locator("#pad-1rem")).ToHaveCSSAsync("box-sizing", "border-box");
        AssertClose(200, (await BoxAsync("#pad-1rem")).W, "border-box keeps the 200px");
        AssertClose(16, (await BoxAsync("#pad-1rem-it")).X - (await BoxAsync("#pad-1rem")).X, "inset 16px");

        await Expect(Page.Locator("#pad-two")).ToHaveCSSAsync("padding-top", "32px");
        await Expect(Page.Locator("#pad-two")).ToHaveCSSAsync("padding-left", "8px");
        AssertClose(32, (await BoxAsync("#pad-two-it")).Y - (await BoxAsync("#pad-two")).Y, "inset 32px from the top");
        AssertClose(8, (await BoxAsync("#pad-two-it")).X - (await BoxAsync("#pad-two")).X, "inset 8px from the left");
    }

    [TestMethod]
    public async Task Element_TheTagTheListRoleAndTheLandmarkName()
    {
        await OpenAsync("/e2e/stack");

        Assert.AreEqual("NAV", await Page.Locator("#el-nav").EvaluateAsync<string>("el => el.tagName"));
        await Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = "Main" })).ToHaveCountAsync(1);

        await Expect(Page.Locator("#el-ul")).ToHaveAttributeAsync("role", "list");
        await Expect(Page.GetByRole(AriaRole.List, new() { Name = "Tags" }).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);
        await Expect(Page.Locator("#el-ol")).ToHaveAttributeAsync("role", "list");

        // A role given through HtmlAttributes replaces the list role.
        await Expect(Page.Locator("#el-tablist")).ToHaveAttributeAsync("role", "tablist");

        // A plain stack is a div without a role.
        Assert.AreEqual("DIV", await Page.Locator("#basic-col").EvaluateAsync<string>("el => el.tagName"));
        Assert.IsNull(await Page.Locator("#basic-col").GetAttributeAsync("role"));
    }

    [TestMethod]
    public async Task Inline_AnInlineStack_FlowsWithTheTextAroundIt()
    {
        await OpenAsync("/e2e/stack");

        var inline = Page.Locator("#el-inline");
        await Expect(inline).ToHaveClassAsync(HasClass("bit-stc-inl"));
        await Expect(inline).ToHaveCSSAsync("display", "inline-flex");

        var box = await BoxAsync("#el-inline");
        var line = await BoxAsync("#inline-line");
        Assert.IsTrue(box.W < 200, $"sized by its content, not the full width ({box.W})");
        Assert.IsTrue(box.X > line.X + 20, "it sits after the text that precedes it");
    }

    [TestMethod]
    public async Task CascadingParameters_BitStackParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#casc-row")).ToHaveCSSAsync("flex-direction", "row");
        AssertClose(32, (await BoxAsync("#cr-2")).X - (await BoxAsync("#cr-1")).Right, "the cascaded gap between the columns");

        // A stack that sets Horizontal itself keeps its own and takes the rest.
        await Expect(Page.Locator("#casc-own")).ToHaveCSSAsync("flex-direction", "column");
        AssertClose(32, (await BoxAsync("#co-2")).Y - (await BoxAsync("#co-1")).Bottom, "the cascaded gap between the rows");

        await Expect(Page.Locator("#casc-out")).ToHaveCSSAsync("flex-direction", "column");
        AssertClose(16, (await BoxAsync("#cx-2")).Y - (await BoxAsync("#cx-1")).Bottom, "outside: the default gap");
    }

    [TestMethod]
    [DataRow("#size-sm", "bit-stc-sm", "#ss-1", "#ss-2", 8)]
    [DataRow("#size-md", "bit-stc-md", "#sm-1", "#sm-2", 16)]
    [DataRow("#size-lg", "bit-stc-lg", "#sl-1", "#sl-2", 32)]
    public async Task Size_EveryValue_TakesTheGapFromTheSpacingScale(string stack, string cls, string first, string second, double gap)
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator(stack)).ToHaveClassAsync(HasClass(cls));
        AssertClose(gap, (await BoxAsync(second)).X - (await BoxAsync(first)).Right, cls);
    }

    [TestMethod]
    public async Task Size_GapAndHorizontalGap_WinOverIt()
    {
        await OpenAsync("/e2e/stack");

        AssertClose(3, (await BoxAsync("#sg-2")).X - (await BoxAsync("#sg-1")).Right, "Gap wins");
        AssertClose(5, (await BoxAsync("#sh-2")).X - (await BoxAsync("#sh-1")).Right, "HorizontalGap wins");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot_AndStyleWinsOverTheGeneratedDisplay()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#style-grid")).ToHaveCSSAsync("display", "grid");
        await Expect(Page.Locator("#style-grid")).ToHaveCSSAsync("padding-top", "3px");

        await Expect(Page.Locator("#class-stack")).ToHaveClassAsync(HasClass("stc-custom"));
        await Expect(Page.Locator("#class-stack")).ToHaveClassAsync(HasClass("bit-stc"));
        await Expect(Page.Locator("#class-stack")).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");
    }

    [TestMethod]
    public async Task HtmlAttributes_IdTabIndexAndTheHiddenAttribute()
    {
        await OpenAsync("/e2e/stack");

        var attr = Page.Locator("#attr-stack");
        await Expect(attr).ToHaveAttributeAsync("data-test", "stack");
        await Expect(attr).ToHaveAttributeAsync("title", "A stack");
        await Expect(attr).ToHaveAttributeAsync("data-own", "yes");
        await Expect(attr).ToHaveAttributeAsync("tabindex", "0");
        await attr.FocusAsync();
        await Expect(attr).ToBeFocusedAsync();

        // hidden still hides a stack, whose inline display would otherwise win over the browser's display:none.
        await Expect(Page.Locator("#hidden-stack")).ToBeHiddenAsync();
        await Expect(Page.Locator("#hidden-stack")).ToHaveCSSAsync("display", "none");
        // hidden="until-found" is left to the browser, which hides it through content-visibility instead.
        await Expect(Page.Locator("#until-found-stack")).ToHaveCSSAsync("display", "flex");
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheStack()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#vis-stack")).ToBeVisibleAsync();

        await Page.ClickAsync("#btn-hidden");
        await Expect(Page.Locator("#vis-stack")).ToHaveCSSAsync("visibility", "hidden");

        await Page.ClickAsync("#btn-collapsed");
        await Expect(Page.Locator("#vis-stack")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Disabled_TheRootCarriesBitDis()
    {
        await OpenAsync("/e2e/stack");

        await Expect(Page.Locator("#disabled-stack")).ToHaveClassAsync(HasClass("bit-dis"));
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(Page.Locator("#disabled-stack")).Not.ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task Rtl_ExplicitAndInherited_StartTheRowAtTheRight()
    {
        await OpenAsync("/e2e/stack");

        var row = Page.Locator("#rtl-row");
        await Expect(row).ToHaveAttributeAsync("dir", "rtl");
        await Expect(row).ToHaveClassAsync(HasClass("bit-rtl"));
        AssertClose((await BoxAsync("#rtl-row")).Right, (await BoxAsync("#rr-1")).Right, "Rtl: item 1 at the right");
        Assert.IsTrue((await BoxAsync("#rr-2")).X < (await BoxAsync("#rr-1")).X, "Rtl: item 2 to its left");

        AssertClose((await BoxAsync("#rtl-row-rev")).X, (await BoxAsync("#rrr-1")).X, "Rtl reversed: item 1 at the left");

        // End of the horizontal axis is the left in a right-to-left column.
        AssertClose((await BoxAsync("#rtl-col-end")).X, (await BoxAsync("#rce-1")).X, "Rtl column, HorizontalAlign End: at the left");

        Assert.IsNull(await Page.Locator("#rtl-inherited").GetAttributeAsync("dir"));
        AssertClose((await BoxAsync("#rtl-inherited")).Right, (await BoxAsync("#ri-1")).Right, "inherited rtl: item 1 at the right");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAndMountingAgain_RaisesNoError()
    {
        await OpenAsync("/e2e/stack");

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-stack")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#mount-stack")).ToHaveCSSAsync("flex-direction", "row");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task SelectAsync(string select, string value, string state, string expectedState)
    {
        await Page.SelectOptionAsync(select, value);
        await Expect(Page.Locator(state)).ToContainTextAsync(expectedState);
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
