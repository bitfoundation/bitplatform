using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitChart at /e2e/chart in a real browser: every chart type drawn as real SVG with its marks sized from the
/// data (bars, stacked and floating bars, lines, areas, pie and doughnut arcs, polar areas, radar, scatter, bubble,
/// mixed, a whole Config), the legend toggling and highlighting series in every placement, tooltips on hover with
/// callbacks, a template and Escape, the click and hover events, keyboard navigation with its live announcements,
/// zoom and pan from the wheel, the pointer, the keyboard and the public methods, the accessible data table and its
/// truncation, the loading and no-data states, titles, the visibility methods and exports, Disabled, Style/Class/
/// Styles/Classes, the --bit-Chart-* variables, BitParams, RTL, reduced motion, responsive resizing, runtime changes,
/// prerendering and unmounting.
/// </summary>
[TestClass]
public class BitChartE2ETests : E2ETestBase
{
    private const string Route = "/e2e/chart";

    [TestMethod]
    public async Task Bar_DrawsOneRectPerValueSizedFromTheData()
    {
        await OpenAsync(Route);

        var rects = Elements("cht-bar", "rect");
        await Expect(rects).ToHaveCountAsync(8);
        var boxes = await RectsAsync("cht-bar");

        // The first four are Sales (10, 20, 30, 40): left to right, each step the same height taller.
        for (var i = 1; i < 4; i++)
        {
            Assert.IsTrue(boxes[i].X > boxes[i - 1].X);
            Assert.AreEqual(boxes[1].H - boxes[0].H, boxes[i].H - boxes[i - 1].H, 0.5, $"step {i}");
        }
        Assert.IsTrue(boxes[3].H > boxes[0].H * 3);
        await Expect(rects.Nth(0)).ToHaveAttributeAsync("fill", "rgb(255, 0, 0)");
        await Expect(rects.Nth(4)).ToHaveAttributeAsync("fill", "rgb(0, 0, 255)");

        // Grouped: a Costs bar sits right of the Sales bar of its category, and is shorter.
        Assert.IsTrue(boxes[4].X > boxes[0].X && boxes[4].X < boxes[1].X);
        Assert.IsTrue(boxes[5].H < boxes[1].H);

        // The category ticks are drawn as text.
        var texts = await Page.Locator("#cht-bar svg text").AllTextContentsAsync();
        CollectionAssert.IsSubsetOf(new[] { "A", "B", "C", "D" }, texts.Select(t => t.Trim()).ToList());
    }

    [TestMethod]
    public async Task Bar_Horizontal_GrowsTheBarsAlongX()
    {
        await OpenAsync(Route);

        await Expect(Elements("cht-bar-h", "rect")).ToHaveCountAsync(3);
        var boxes = await RectsAsync("cht-bar-h");
        Assert.IsTrue(boxes[1].Y > boxes[0].Y && boxes[2].Y > boxes[1].Y, "Categories run down the Y axis.");

        // 10, 20, 30 on an axis from 10: the bar of 30 is twice the bar of 20, the one of 10 a sliver.
        Assert.IsTrue(boxes[0].W < boxes[1].W && boxes[1].W < boxes[2].W);
        Assert.AreEqual(2, boxes[2].W / boxes[1].W, 0.02);
        Assert.AreEqual(boxes[0].H, boxes[1].H, 0.5);
    }

    [TestMethod]
    public async Task Bar_Stacked_PutsEachSeriesOnTheOneBelowFromZero()
    {
        await OpenAsync(Route);

        var boxes = await RectsAsync("cht-bar-stacked");
        Assert.AreEqual(8, boxes.Length);
        for (var i = 0; i < 4; i++)
        {
            var sales = boxes[i];
            var costs = boxes[i + 4];
            Assert.AreEqual(sales.X, costs.X, 0.5, "Stacked bars share their column.");
            Assert.AreEqual(sales.Y, costs.Y + costs.H, 1, "Costs sits on top of Sales.");
        }

        // From zero: Sales 10 is twice Costs 5.
        Assert.AreEqual(2, boxes[0].H / boxes[4].H, 0.05);
    }

    [TestMethod]
    public async Task Bar_Floating_SpansItsRange()
    {
        await OpenAsync(Route);

        var boxes = await RectsAsync("cht-bar-floating");
        Assert.AreEqual(2, boxes.Length);
        Assert.AreEqual(2, boxes[1].H / boxes[0].H, 0.05, "5..25 is twice as tall as 10..20.");
        Assert.IsTrue(boxes[1].Y < boxes[0].Y && boxes[1].Y + boxes[1].H > boxes[0].Y + boxes[0].H);
    }

    [TestMethod]
    public async Task Line_DrawsAPathThroughAPointPerValue()
    {
        await OpenAsync(Route);

        var path = Page.Locator("#cht-line g.bit-cht-series path");
        await Expect(path).ToHaveCountAsync(1);
        await Expect(path).ToHaveAttributeAsync("stroke", "rgb(0, 128, 0)");
        await Expect(path).ToHaveAttributeAsync("fill", "none");
        var d = await path.GetAttributeAsync("d");
        Assert.AreEqual(3, Regex.Matches(d!, "L").Count, d);

        var points = await CirclesAsync("cht-line");
        Assert.AreEqual(4, points.Length);
        // 10, 30, 20, 40: the highest value is drawn highest.
        Assert.IsTrue(points[3].Cy < points[1].Cy && points[1].Cy < points[2].Cy && points[2].Cy < points[0].Cy);
        Assert.IsTrue(points.Zip(points.Skip(1)).All(p => p.Second.Cx > p.First.Cx));
    }

    [TestMethod]
    public async Task Area_FillsUnderTheLine()
    {
        await OpenAsync(Route);

        var fills = await Page.Locator("#cht-area g.bit-cht-series path").EvaluateAllAsync<string[]>("ps => ps.map(p => p.getAttribute('fill'))");
        Assert.IsTrue(fills.Any(f => f == "rgba(0, 128, 0, 0.3)"), string.Join(", ", fills));
    }

    [TestMethod]
    public async Task Pie_DrawsASlicePerValueWithAnAngleFromIt()
    {
        await OpenAsync(Route);

        var slices = Elements("cht-pie", "path");
        await Expect(slices).ToHaveCountAsync(3);
        var angles = await SliceAnglesAsync("cht-pie");
        Assert.AreEqual(90, angles[0], 1);
        Assert.AreEqual(90, angles[1], 1);
        Assert.AreEqual(180, angles[2], 1);
        await Expect(slices.Nth(2)).ToHaveAttributeAsync("fill", "rgb(0, 128, 0)");
    }

    [TestMethod]
    public async Task Doughnut_CutsOutTheMiddle()
    {
        await OpenAsync(Route);

        var ds = await Elements("cht-doughnut", "path").EvaluateAllAsync<string[]>("ps => ps.map(p => p.getAttribute('d'))");
        Assert.AreEqual(3, ds.Length);
        Assert.IsTrue(ds.All(d => Regex.Matches(d, "A").Count == 2), "Every slice has an outer and an inner arc.");

        // The middle of the doughnut is no slice.
        var center = await Page.Locator("#cht-doughnut svg").EvaluateAsync<bool>(@"svg => {
            const r = svg.getBoundingClientRect();
            const el = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
            return !!el && !!el.closest('.bit-cht-el');
        }");
        Assert.IsFalse(center);
    }

    [TestMethod]
    public async Task PolarArea_GrowsEachSliceFromItsValue()
    {
        await OpenAsync(Route);

        var sizes = await Elements("cht-polar", "path").EvaluateAllAsync<double[]>("ps => ps.map(p => { const b = p.getBBox(); return Math.max(b.width, b.height); })");
        Assert.AreEqual(3, sizes.Length);
        Assert.IsTrue(sizes[0] < sizes[1] && sizes[1] < sizes[2], string.Join(", ", sizes));
    }

    [TestMethod]
    public async Task Radar_DrawsAShapePerSeriesAndAPointPerValue()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#cht-radar g.bit-cht-series").Locator("path, polygon")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#cht-radar g.bit-cht-data > g.bit-cht-el")).ToHaveCountAsync(10);
        var labels = await Page.Locator("#cht-radar svg text").AllTextContentsAsync();
        CollectionAssert.IsSubsetOf(new[] { "Speed", "Power", "Skill", "Stamina", "Luck" }, labels.Select(t => t.Trim()).ToList());
    }

    [TestMethod]
    public async Task Scatter_PlacesAPointPerXY()
    {
        await OpenAsync(Route);

        var points = await CirclesAsync("cht-scatter");
        Assert.AreEqual(3, points.Length);
        Assert.IsTrue(points[0].Cx < points[1].Cx && points[1].Cx < points[2].Cx);
        Assert.IsTrue(points[0].Cy > points[1].Cy && points[1].Cy > points[2].Cy);

        // y = x squared: the rise from 2 to 3 is the rise from 1 to 2 again and a third more.
        Assert.AreEqual(5d / 3, (points[1].Cy - points[2].Cy) / (points[0].Cy - points[1].Cy), 0.05);
    }

    [TestMethod]
    public async Task Bubble_SizesEachPointFromItsRadius()
    {
        await OpenAsync(Route);

        var points = await CirclesAsync("cht-bubble");
        Assert.AreEqual(3, points.Length);
        Assert.AreEqual(2, points[1].R / points[0].R, 0.05);
        Assert.AreEqual(2, points[2].R / points[1].R, 0.05);
    }

    [TestMethod]
    public async Task Mixed_DrawsBarsAndALine()
    {
        await OpenAsync(Route);

        await Expect(Elements("cht-mixed", "rect")).ToHaveCountAsync(3);
        await Expect(Elements("cht-mixed", "circle")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#cht-mixed g.bit-cht-series path")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Config_DrawsTheChartItDescribes()
    {
        await OpenAsync(Route);

        await Expect(Elements("cht-config", "rect")).ToHaveCountAsync(2);
        var boxes = await RectsAsync("cht-config");
        Assert.IsTrue(boxes[1].H > boxes[0].H);
        await Expect(Page.Locator("#cht-config svg")).ToHaveAttributeAsync("aria-label", "Bar chart with 1 data series.");
    }

    [TestMethod]
    public async Task Legend_ClickAnItem_TogglesItsSeriesAndFiresTheEvent()
    {
        await OpenAsync(Route);

        var legend = Page.Locator("#cht-legend .bit-cht-lgd");
        await Expect(legend).ToHaveAttributeAsync("role", "group");
        await Expect(legend).ToHaveAttributeAsync("aria-label", "Chart legend");
        var costs = legend.Locator("button.bit-cht-lgd-itm", new() { HasTextString = "Costs" });
        await Expect(costs).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(costs.Locator(".bit-cht-lgd-box")).ToHaveCSSAsync("background-color", "rgb(0, 0, 255)");

        await costs.ClickAsync();
        await Expect(costs).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(costs).ToHaveClassAsync(HasClass("bit-cht-hdn"));
        await Expect(Elements("cht-legend", "rect")).ToHaveCountAsync(4);
        await Expect(Page.Locator("#legend-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#legend-last")).ToHaveTextAsync("Costs|1|False");

        await costs.ClickAsync();
        await Expect(costs).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Elements("cht-legend", "rect")).ToHaveCountAsync(8);
        await Expect(Page.Locator("#legend-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Legend_TheKeyboard_TogglesAnItem()
    {
        await OpenAsync(Route);

        var sales = Page.Locator("#cht-legend .bit-cht-lgd-itm", new() { HasTextString = "Sales" });
        await sales.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(sales).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Elements("cht-legend", "rect")).ToHaveCountAsync(4);
        await Page.Keyboard.PressAsync(" ");
        await Expect(sales).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Elements("cht-legend", "rect")).ToHaveCountAsync(8);
    }

    [TestMethod]
    public async Task Legend_HoverAnItem_FadesTheOtherSeries()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-legend .bit-cht-lgd-itm", new() { HasTextString = "Sales" }).HoverAsync();
        var els = Page.Locator("#cht-legend g.bit-cht-data > g.bit-cht-el");
        await Expect(els.Nth(4)).ToHaveClassAsync(HasClass("bit-cht-dim"));
        await Expect(els.Nth(4)).ToHaveCSSAsync("opacity", "0.2");
        await Expect(els.Nth(0)).Not.ToHaveClassAsync(HasClass("bit-cht-dim"));

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(els.Nth(4)).Not.ToHaveClassAsync(HasClass("bit-cht-dim"));
    }

    [TestMethod]
    public async Task Legend_Placements_PutTheLegendOnTheirSide()
    {
        await OpenAsync(Route);

        var right = Page.Locator("#cht-legend-right .bit-cht-lgd");
        await Expect(right).ToHaveClassAsync(HasClass("bit-cht-lgd-v"));
        Assert.IsTrue((await right.BoundingBoxAsync())!.X > (await Page.Locator("#cht-legend-right .bit-cht-plot").BoundingBoxAsync())!.X);

        var bottom = Page.Locator("#cht-legend-bottom .bit-cht-lgd");
        await Expect(bottom).ToHaveClassAsync(HasClass("bit-cht-lgd-h"));
        var plot = (await Page.Locator("#cht-legend-bottom .bit-cht-plot").BoundingBoxAsync())!;
        Assert.IsTrue((await bottom.BoundingBoxAsync())!.Y >= plot.Y + plot.Height - 1);

        var top = (await Page.Locator("#cht-legend .bit-cht-lgd").BoundingBoxAsync())!;
        Assert.IsTrue(top.Y < (await Page.Locator("#cht-legend .bit-cht-plot").BoundingBoxAsync())!.Y);
    }

    [TestMethod]
    public async Task Legend_StaticHiddenAndPerSlice()
    {
        await OpenAsync(Route);

        var staticLegend = Page.Locator("#cht-legend-static .bit-cht-lgd");
        await Expect(staticLegend.Locator("button")).ToHaveCountAsync(0);
        await Expect(staticLegend.Locator("span.bit-cht-lgd-sta")).ToHaveCountAsync(2);
        await Expect(staticLegend.Locator(".bit-cht-lgd-ttl")).ToHaveTextAsync("Static legend");
        await Expect(staticLegend).ToHaveAttributeAsync("aria-label", "Static legend");
        await staticLegend.Locator(".bit-cht-lgd-itm").First.ClickAsync();
        await Expect(Elements("cht-legend-static", "rect")).ToHaveCountAsync(8);

        await Expect(Page.Locator("#cht-legend-hidden .bit-cht-lgd")).ToHaveCountAsync(0);

        // A pie's legend has an item per slice, and toggling one hides that slice.
        var pieItems = Page.Locator("#cht-legend-pie .bit-cht-lgd-itm");
        await Expect(pieItems).ToHaveCountAsync(3);
        await pieItems.Nth(2).ClickAsync();
        await Expect(Elements("cht-legend-pie", "path")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Tooltip_HoverABar_ShowsItsValueAndFiresOnElementHover()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-tooltip g.bit-cht-data > g.bit-cht-el").Nth(1).HoverAsync();
        var tooltip = Page.Locator("#cht-tooltip .bit-cht-tt");
        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(tooltip.Locator(".bit-cht-tt-ttl")).ToHaveTextAsync("B");
        await Expect(tooltip.Locator(".bit-cht-tt-itm")).ToHaveCountAsync(1);
        await Expect(tooltip.Locator(".bit-cht-tt-itm")).ToContainTextAsync("20");
        await Expect(tooltip.Locator(".bit-cht-tt-swt")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await Expect(tooltip.Locator(".bit-cht-tt-ftr")).ToHaveTextAsync("Total: 20");
        await Expect(Page.Locator("#hover-last")).ToHaveTextAsync("B|0:1:20");

        // Placed over the plot, near the bar.
        var tip = (await tooltip.BoundingBoxAsync())!;
        var plot = (await Page.Locator("#cht-tooltip .bit-cht-plot").BoundingBoxAsync())!;
        Assert.IsTrue(tip.X >= plot.X - 1 && tip.X + tip.Width <= plot.X + plot.Width + 1);

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(tooltip).ToHaveCountAsync(0);
        await Expect(Page.Locator("#hover-last")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Tooltip_HoverTheEmptyPartOfACategory_ShowsTheWholeIndexWithACrosshair()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-tooltip").ScrollIntoViewIfNeededAsync();
        var bar = (await Elements("cht-tooltip", "rect").Nth(0).BoundingBoxAsync())!;
        var plot = await PlotAreaAsync("cht-tooltip");
        await Page.Mouse.MoveAsync(bar.X + bar.Width / 2, (float)(plot[1] + 10));

        var tooltip = Page.Locator("#cht-tooltip .bit-cht-tt");
        await Expect(tooltip.Locator(".bit-cht-tt-ttl")).ToHaveTextAsync("A");
        await Expect(tooltip.Locator(".bit-cht-tt-itm")).ToHaveCountAsync(2);
        await Expect(tooltip.Locator(".bit-cht-tt-ftr")).ToHaveTextAsync("Total: 15");
        await Expect(Page.Locator("#cht-tooltip g.bit-cht-hover line[stroke-dasharray='4,3']")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Tooltip_EscapeDismissesTheOnePointerOpened()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-tooltip g.bit-cht-data > g.bit-cht-el").Nth(2).HoverAsync();
        var tooltip = Page.Locator("#cht-tooltip .bit-cht-tt");
        await Expect(tooltip).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(tooltip).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Tooltip_TemplateAndDisabledTooltip()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-tooltip-template g.bit-cht-data > g.bit-cht-el").Nth(3).HoverAsync();
        await Expect(Page.Locator("#cht-tooltip-template .bit-cht-tt-custom .custom-tooltip")).ToHaveTextAsync(new Regex("^D: .*40"));

        await Page.Locator("#cht-tooltip-off g.bit-cht-data > g.bit-cht-el").Nth(3).HoverAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#cht-tooltip-off .bit-cht-tt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cht-tooltip-off g.bit-cht-hover > *").First).ToBeAttachedAsync();
    }

    [TestMethod]
    public async Task OnElementClick_ClickABar_FiresWithItsIndexes()
    {
        await OpenAsync(Route);

        var el = Page.Locator("#cht-tooltip g.bit-cht-data > g.bit-cht-el").Nth(6);
        await Expect(el).ToHaveCSSAsync("cursor", "pointer");
        await el.ClickAsync();
        await Expect(Page.Locator("#click-last")).ToHaveTextAsync("1,2");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");

        // A chart without a handler shows no pointer cursor.
        await Expect(Page.Locator("#cht-bar g.bit-cht-data > g.bit-cht-el").First).ToHaveCSSAsync("cursor", "default");
    }

    [TestMethod]
    public async Task Accessibility_ThePlotIsANamedFocusableApplicationWithAHint()
    {
        await OpenAsync(Route);

        var svg = Page.Locator("#cht-keyboard svg.bit-cht-svg");
        await Expect(svg).ToHaveAttributeAsync("role", "application");
        await Expect(svg).ToHaveAttributeAsync("tabindex", "0");
        await Expect(svg).ToHaveAttributeAsync("aria-roledescription", "chart");
        await Expect(svg).ToHaveAccessibleNameAsync("Bar chart with 2 data series.");
        await Expect(svg).ToHaveAccessibleDescriptionAsync(new Regex("Quarterly sales and costs\\..*arrow keys"));

        var texts = Page.Locator("#cht-texts svg.bit-cht-svg");
        await Expect(texts).ToHaveAccessibleNameAsync("Custom chart name");
        await Expect(texts).ToHaveAttributeAsync("aria-roledescription", "graph");
        await Expect(texts).ToHaveAccessibleDescriptionAsync("Custom hint.");
        await Expect(Page.Locator("#cht-texts .bit-cht-lgd")).ToHaveAttributeAsync("aria-label", "Series of the graph");
        await Expect(Page.Locator("#cht-texts table thead th").First).ToHaveTextAsync("Reihe");

        // A shown title names the chart.
        await Expect(Page.Locator("#cht-titled svg.bit-cht-svg")).ToHaveAccessibleNameAsync("Quarterly figures");
    }

    [TestMethod]
    public async Task Keyboard_ArrowsHomeEnd_MoveThroughTheDataAndAnnounceIt()
    {
        await OpenAsync(Route);

        var svg = Page.Locator("#cht-keyboard svg.bit-cht-svg");
        var live = Page.Locator("#cht-keyboard [role=status]");
        await svg.FocusAsync();
        await Expect(svg).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(live).ToHaveTextAsync(new Regex("^A, .*10.*, 1 of 4, series 1 of 2$"));
        await Expect(Page.Locator("#cht-keyboard .bit-cht-focus-ring")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#cht-keyboard .bit-cht-tt")).ToBeVisibleAsync();
        await Expect(svg).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(live).ToHaveTextAsync(new Regex("^B, .*20.*, 2 of 4, series 1 of 2$"));
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync(new Regex("^B, .*15.*, 2 of 4, series 2 of 2$"));
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(live).ToHaveTextAsync(new Regex("^A, .*5.*, 1 of 4, series 2 of 2$"));
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(live).ToHaveTextAsync(new Regex("^A, .*10.*, series 1 of 2$"));
        await Page.Keyboard.PressAsync("End");
        await Expect(live).ToHaveTextAsync(new Regex("^D, .*35.*, 4 of 4, series 2 of 2$"));
        await Page.Keyboard.PressAsync("Home");
        await Expect(live).ToHaveTextAsync(new Regex("^A, .*10"));

        // The arrows never scroll the page while the plot has the focus.
        var scroll = await Page.EvaluateAsync<double>("() => scrollY");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(live).ToHaveTextAsync(new Regex("series 2 of 2$"));
        Assert.AreEqual(scroll, await Page.EvaluateAsync<double>("() => scrollY"));
    }

    [TestMethod]
    public async Task Keyboard_EnterSelectsAndEscapeLetsGo()
    {
        await OpenAsync(Route);

        var svg = Page.Locator("#cht-keyboard svg.bit-cht-svg");
        await svg.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#key-last")).ToHaveTextAsync("0,1");
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#key-clicks")).ToHaveTextAsync("2");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#cht-keyboard .bit-cht-focus-ring")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cht-keyboard .bit-cht-tt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cht-keyboard [role=status]")).ToHaveTextAsync("");
        await Expect(svg).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
        await Expect(svg).ToBeFocusedAsync();

        // Leaving the plot lets go of the position too.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Page.Locator("#cht-keyboard .bit-cht-focus-ring")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#cht-keyboard .bit-cht-focus-ring")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task DataTable_ListsEveryValueForScreenReaders()
    {
        await OpenAsync(Route);

        var table = Page.Locator("#cht-bar table");
        await Expect(table).ToHaveCountAsync(1);
        await Expect(table.Locator("caption")).ToHaveTextAsync("Bar chart with 2 data series.");
        var headers = await table.Locator("thead th").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Series", "A", "B", "C", "D" }, headers.ToArray());
        var sales = await table.Locator("tbody tr").Nth(0).Locator("th, td").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Sales", "10", "20", "30", "40" }, sales.ToArray());
        var costs = await table.Locator("tbody tr").Nth(1).Locator("th, td").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Costs", "5", "15", "25", "35" }, costs.ToArray());
        await Expect(table.Locator("tbody th").First).ToHaveAttributeAsync("scope", "row");

        // Visually hidden, yet in the accessibility tree.
        var size = await table.EvaluateAsync<double[]>("t => { const r = t.closest('.bit-cht-sr').getBoundingClientRect(); return [r.width, r.height]; }");
        Assert.IsTrue(size[0] <= 1 && size[1] <= 1);
        await Expect(Page.GetByRole(AriaRole.Table, new() { Name = "Bar chart with 2 data series." }).First).ToBeAttachedAsync();
    }

    [TestMethod]
    public async Task DataTable_PointDataTruncationAndOptOut()
    {
        await OpenAsync(Route);

        var bubble = Page.Locator("#cht-table-scatter table");
        CollectionAssert.AreEqual(new[] { "Series", "X", "Y", "R" }, (await bubble.Locator("thead th").AllTextContentsAsync()).ToArray());
        CollectionAssert.AreEqual(new[] { "Bubbles", "3", "3", "20" }, (await bubble.Locator("tbody tr").Nth(2).Locator("th, td").AllTextContentsAsync()).ToArray());

        var truncated = Page.Locator("#cht-table-truncated table");
        await Expect(truncated.Locator("caption")).ToContainTextAsync("Showing the first 1 of 2 rows.");
        await Expect(truncated.Locator("caption")).ToContainTextAsync("Showing the first 2 of 4 columns.");
        await Expect(truncated.Locator("tbody tr")).ToHaveCountAsync(1);
        await Expect(truncated.Locator("thead th")).ToHaveCountAsync(3);

        await Expect(Page.Locator("#cht-no-table table")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Zoom_KeyboardPlusMinusAndZero_ZoomAndReset()
    {
        await OpenAsync(Route);

        var svg = Page.Locator("#cht-zoom svg.bit-cht-svg");
        await Expect(svg).ToHaveAccessibleDescriptionAsync(new Regex("Press plus or minus to zoom"));
        await svg.FocusAsync();
        await Page.Keyboard.PressAsync("+");
        await Expect(Page.Locator("#zoom-count")).ToHaveTextAsync("1");
        var (min, max) = Range(await Page.Locator("#zoom-range").InnerTextAsync());
        Assert.IsTrue(min > 0 && max < 50 && max - min < 50, $"{min}..{max}");

        await Page.Keyboard.PressAsync("-");
        await Expect(Page.Locator("#zoom-count")).ToHaveTextAsync("2");
        var (min2, max2) = Range(await Page.Locator("#zoom-range").InnerTextAsync());
        Assert.IsTrue(max2 - min2 > max - min);

        await Page.Keyboard.PressAsync("+");
        await Page.Keyboard.PressAsync("0");
        await Expect(Page.Locator("#zoom-range")).ToHaveTextAsync("0|50");
    }

    [TestMethod]
    public async Task Zoom_TheWheel_ZoomsAroundThePointerAndDoubleClickResets()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-zoom").ScrollIntoViewIfNeededAsync();
        var plot = (await Page.Locator("#cht-zoom .bit-cht-plot").BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(plot.X + plot.Width / 2, plot.Y + plot.Height / 2);
        await Page.Mouse.WheelAsync(0, -100);
        await Expect(Page.Locator("#zoom-count")).Not.ToHaveTextAsync("0");
        var (min, max) = Range(await Page.Locator("#zoom-range").InnerTextAsync());
        Assert.IsTrue(min > 0 && max < 50, $"{min}..{max}");

        // Every value on the zoomed axis: the ticks follow the range.
        await Page.Mouse.DblClickAsync(plot.X + plot.Width / 2, plot.Y + plot.Height / 2);
        await Expect(Page.Locator("#zoom-range")).ToHaveTextAsync("0|50");
    }

    [TestMethod]
    public async Task Zoom_DraggingAZoomedPlot_Pans()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-zoom-to").ClickAsync();
        await Expect(Page.Locator("#zoom-range")).ToHaveTextAsync("10|20");

        await Page.Locator("#cht-zoom").ScrollIntoViewIfNeededAsync();
        var plot = (await Page.Locator("#cht-zoom .bit-cht-plot").BoundingBoxAsync())!;
        var y = plot.Y + plot.Height / 2;
        await Page.Mouse.MoveAsync(plot.X + plot.Width * 0.6f, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(plot.X + plot.Width * 0.4f, y, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#zoom-range")).Not.ToHaveTextAsync("10|20");
        var (min, max) = Range(await Page.Locator("#zoom-range").InnerTextAsync());
        Assert.IsTrue(min > 10, "Dragging to the left shows what is to the right.");
        Assert.AreEqual(10, max - min, 0.01, "Panning keeps the span.");

        await Page.Locator("#btn-zoom-reset").ClickAsync();
        await Expect(Page.Locator("#zoom-range")).ToHaveTextAsync("0|50");
    }

    [TestMethod]
    public async Task Zoom_DragZoom_ZoomsToTheBoxDrawn()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-zoom-drag").ScrollIntoViewIfNeededAsync();
        var plotArea = await PlotAreaAsync("cht-zoom-drag");
        var x0 = plotArea[0] + plotArea[2] * 0.2;
        var x1 = plotArea[0] + plotArea[2] * 0.6;
        var y = plotArea[1] + plotArea[3] / 2;
        await Page.Mouse.MoveAsync((float)x0, (float)y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync((float)x1, (float)(y + 20), new() { Steps = 5 });
        await Expect(Page.Locator("#cht-zoom-drag g.bit-cht-hover rect[fill*='zoom-box']")).ToHaveCountAsync(1);
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#drag-range")).Not.ToHaveTextAsync("none");
        var (min, max) = Range(await Page.Locator("#drag-range").InnerTextAsync());
        Assert.AreEqual(10, min, 1);
        Assert.AreEqual(30, max, 1);
        await Expect(Page.Locator("#cht-zoom-drag g.bit-cht-hover rect[fill*='zoom-box']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task PublicMethods_DatasetAndIndexVisibility()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-hide-ds1").ClickAsync();
        await Expect(Elements("cht-methods", "rect")).ToHaveCountAsync(4);
        await Expect(Page.Locator("#cht-methods .bit-cht-lgd-itm", new() { HasTextString = "Costs" })).ToHaveAttributeAsync("aria-pressed", "false");

        await Page.Locator("#btn-toggle-ds0").ClickAsync();
        await Page.Locator("#btn-read-visibility").ClickAsync();
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("ds0=False;ds1=False");
        await Expect(Elements("cht-methods", "rect")).ToHaveCountAsync(0);

        await Page.Locator("#btn-reset-visibility").ClickAsync();
        await Expect(Elements("cht-methods", "rect")).ToHaveCountAsync(8);
        await Page.Locator("#btn-read-visibility").ClickAsync();
        await Expect(Page.Locator("#visibility")).ToHaveTextAsync("ds0=True;ds1=True");

        // Data indexes are the slices of a pie.
        await Page.Locator("#btn-hide-idx2").ClickAsync();
        await Expect(Elements("cht-methods-pie", "path")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#cht-methods-pie .bit-cht-lgd-itm").Nth(2)).ToHaveAttributeAsync("aria-pressed", "false");
        await Page.Locator("#btn-toggle-idx0").ClickAsync();
        await Expect(Elements("cht-methods-pie", "path")).ToHaveCountAsync(1);
        await Page.Locator("#btn-read-pie").ClickAsync();
        await Expect(Page.Locator("#pie-visibility")).ToHaveTextAsync("idx0=False;idx1=True;idx2=False");

        await Page.Locator("#btn-reset-pie").ClickAsync();
        await Expect(Elements("cht-methods-pie", "path")).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task PublicMethods_RefreshRedrawsAChangedDataset()
    {
        await OpenAsync(Route);

        var before = await RectsAsync("cht-methods");
        Assert.IsTrue(before[0].H < before[3].H);
        await Page.Locator("#btn-refresh").ClickAsync();
        await Expect(Elements("cht-methods", "rect").First).Not.ToHaveAttributeAsync("height", before[0].H.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        var after = await RectsAsync("cht-methods");
        Assert.AreEqual(after[3].H, after[0].H, 0.5);
    }

    [TestMethod]
    public async Task Export_CsvSvgAndImage()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-to-csv").ClickAsync();
        await Expect(Page.Locator("#csv")).ToContainTextAsync("Sales");
        var csv = await Page.Locator("#csv").InnerTextAsync();
        StringAssert.Contains(csv, "Costs");
        StringAssert.Contains(csv, "35");

        await Page.Locator("#btn-to-svg").ClickAsync();
        await Expect(Page.Locator("#svg-string")).ToHaveTextAsync(new Regex(@"^\d+\|<svg\|True$"));

        await Page.Locator("#btn-to-base64-small").ClickAsync();
        await Expect(Page.Locator("#base64")).ToHaveTextAsync("data:image/png;base64,|True");
    }

    [TestMethod]
    [Ignore("KNOWN BUG: on Blazor Server ToBase64ImageAsync hands the whole data URL back through one JS interop result, and a chart rasterized at the default scale of 2 is larger than the default 32 KB SignalR message limit, so the circuit is torn down (Connection closed with an error) instead of the image arriving. A scale of 0.5 fits (Export_CsvSvgAndImage); a stream reference would lift the limit.")]
    public async Task Export_ToBase64ImageAtTheDefaultScale_ArrivesOnBlazorServer()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-to-base64").ClickAsync();
        await Expect(Page.Locator("#base64")).ToHaveTextAsync("data:image/png;base64,");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Export_Downloads()
    {
        await OpenAsync(Route);

        var csv = await Page.RunAndWaitForDownloadAsync(() => Page.Locator("#btn-export-csv").ClickAsync());
        Assert.AreEqual("chart-data.csv", csv.SuggestedFilename);
        var svg = await Page.RunAndWaitForDownloadAsync(() => Page.Locator("#btn-export-svg").ClickAsync());
        Assert.AreEqual("chart.svg", svg.SuggestedFilename);
        var png = await Page.RunAndWaitForDownloadAsync(() => Page.Locator("#btn-export-png").ClickAsync());
        Assert.AreEqual("chart.png", png.SuggestedFilename);
    }

    [TestMethod]
    public async Task Loading_VeilsThePlotAndAnnouncesTheLabel()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#cht-loading");
        await Expect(root).ToHaveAttributeAsync("aria-busy", "true");
        var veil = root.Locator(".bit-cht-ldg");
        await Expect(veil).ToBeVisibleAsync();
        await Expect(veil.Locator(".bit-cht-spn")).ToBeVisibleAsync();
        await Expect(veil.Locator(".bit-cht-ldg-lbl")).ToHaveTextAsync("Fetching sales");
        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("Fetching sales");
        await Expect(root.Locator("svg.bit-cht-svg")).ToHaveAttributeAsync("role", "img");

        // The veil covers the plot and takes the pointer.
        var plot = (await root.Locator(".bit-cht-plot").BoundingBoxAsync())!;
        var veilBox = (await veil.BoundingBoxAsync())!;
        Assert.AreEqual(plot.Width, veilBox.Width, 1);
        await root.Locator("g.bit-cht-data > g.bit-cht-el").Nth(3).HoverAsync(new() { Force = true });
        await Expect(root.Locator(".bit-cht-tt")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-loading").ClickAsync();
        await Expect(veil).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(root.Locator("svg.bit-cht-svg")).ToHaveAttributeAsync("role", "application");

        await Expect(Page.Locator("#cht-loading-template .bit-cht-ldg .custom-loading")).ToHaveTextAsync("Please wait");
    }

    [TestMethod]
    public async Task NoData_ShowsTheMessageTextOrTemplate()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#cht-nodata .bit-cht-nodata")).ToHaveTextAsync("No data to display");
        await Expect(Page.Locator("#cht-nodata g.bit-cht-data > g")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cht-nodata svg.bit-cht-svg")).ToHaveAttributeAsync("role", "img");
        await Expect(Page.Locator("#cht-nodata-text .bit-cht-nodata")).ToHaveTextAsync("Nothing yet");
        await Expect(Page.Locator("#cht-nodata-template .bit-cht-nodata .custom-nodata")).ToHaveTextAsync("Empty!");
    }

    [TestMethod]
    public async Task Titles_TitleAndSubtitleAboveOrBelowThePlot()
    {
        await OpenAsync(Route);

        var title = Page.Locator("#cht-title .bit-cht-ttl");
        await Expect(title).ToHaveTextAsync("Quarterly figures");
        await Expect(title).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#cht-title .bit-cht-sub")).ToHaveTextAsync("In thousands");
        var plot = (await Page.Locator("#cht-title .bit-cht-plot").BoundingBoxAsync())!;
        Assert.IsTrue((await title.BoundingBoxAsync())!.Y < plot.Y);

        var bottom = Page.Locator("#cht-title-bottom .bit-cht-ttl");
        await Expect(bottom).ToHaveTextAsync("Bottom title");
        var bottomPlot = (await Page.Locator("#cht-title-bottom .bit-cht-plot").BoundingBoxAsync())!;
        Assert.IsTrue((await bottom.BoundingBoxAsync())!.Y > bottomPlot.Y);
    }

    [TestMethod]
    public async Task Disabled_DimsTheChartAndAnswersNothing()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#cht-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        Assert.IsTrue(double.Parse(await ComputedStyleAsync("#cht-disabled", "opacity"), System.Globalization.CultureInfo.InvariantCulture) < 1);
        var svg = root.Locator("svg.bit-cht-svg");
        await Expect(svg).ToHaveAttributeAsync("role", "img");
        await Expect(svg).ToHaveAttributeAsync("tabindex", "-1");
        var legendItems = root.Locator(".bit-cht-lgd-itm");
        await Expect(legendItems.First).ToBeDisabledAsync();
        await Expect(legendItems.First).ToHaveAttributeAsync("aria-disabled", "true");

        var bar = root.Locator("g.bit-cht-data > g.bit-cht-el").Nth(3);
        await bar.HoverAsync();
        await bar.ClickAsync();
        await bar.DblClickAsync();
        await NextFramesAsync();
        await Expect(root.Locator(".bit-cht-tt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#page-dblclicks")).ToHaveTextAsync("0");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(svg).ToHaveAttributeAsync("role", "application");
        await bar.ClickAsync();
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#page-pointerdowns")).ToHaveTextAsync("1");
        await Expect(root.Locator(".bit-cht-tt")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRootAndEveryPart()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#cht-styled");
        await Expect(root).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(root).ToHaveClassAsync(HasClass("custom-root"));
        Assert.AreEqual("rgb(1, 2, 3)", await ComputedStyleAsync("#cht-styled", "border-top-color"));
        var box = (await root.BoundingBoxAsync())!;
        Assert.AreEqual(500, box.Width, 1);
        Assert.AreEqual(250, box.Height, 1);
        await Expect(root.Locator(".bit-cht-plot")).ToHaveClassAsync(HasClass("custom-plot"));
        await Expect(root.Locator(".bit-cht-plot")).ToHaveCSSAsync("background-color", "rgb(250, 250, 210)");
        await Expect(root.Locator(".bit-cht-lgd")).ToHaveClassAsync(HasClass("custom-legend"));
        await Expect(root.Locator(".bit-cht-lgd")).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(root.Locator(".bit-cht-lgd-itm").First).ToHaveClassAsync(HasClass("custom-legend-item"));
        await Expect(root.Locator(".bit-cht-ttl")).ToHaveClassAsync(HasClass("custom-title"));
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_ReskinThePaletteTheLegendTheTooltipAndTheEmptyState()
    {
        await OpenAsync(Route);

        var rects = Elements("cht-vars", "rect");
        await Expect(rects.First).ToHaveCSSAsync("fill", "rgb(255, 0, 255)");
        await Expect(rects.Nth(2)).ToHaveCSSAsync("fill", "rgb(0, 255, 255)");
        await Expect(Page.Locator("#cht-vars .bit-cht-lgd-itm").First).ToHaveCSSAsync("color", "rgb(0, 100, 0)");
        await Expect(Page.Locator("#cht-vars-nodata .bit-cht-nodata")).ToHaveCSSAsync("color", "rgb(128, 0, 0)");

        await Page.Locator("#cht-vars g.bit-cht-data > g.bit-cht-el").Nth(1).HoverAsync();
        await Expect(Page.Locator("#cht-vars .bit-cht-tt")).ToHaveCSSAsync("background-color", "rgb(0, 0, 128)");
    }

    [TestMethod]
    public async Task Params_BitChartParams_CascadeAndAnOwnValueWins()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#cht-params .bit-cht-nodata")).ToHaveTextAsync("Cascaded empty text");
        Assert.AreEqual(200, (await Page.Locator("#cht-params").BoundingBoxAsync())!.Height, 1);
        await Expect(Page.Locator("#cht-params svg.bit-cht-svg")).ToHaveAttributeAsync("aria-label", "Bar chart with 0 data series.");
    }

    [TestMethod]
    public async Task Rtl_PutsAStartLegendOnTheRight()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#cht-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#cht-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var (id, startIsRight) in new[] { ("cht-rtl", true), ("cht-rtl-inherited", true), ("cht-ltr-start", false) })
        {
            var legend = (await Page.Locator($"#{id} .bit-cht-lgd").BoundingBoxAsync())!;
            var plot = (await Page.Locator($"#{id} .bit-cht-plot").BoundingBoxAsync())!;
            Assert.AreEqual(startIsRight, legend.X > plot.X, id);
        }
    }

    [TestMethod]
    public async Task ReducedMotion_DrawsTheChartAtOnceAndForceAnimationAnimates()
    {
        await OpenAsync(Route);

        var animated = Page.Locator("#cht-animated g.bit-cht-data");
        await Expect(animated).ToHaveClassAsync(HasClass("bit-cht-anim"));
        await Expect(animated).ToHaveClassAsync(HasClass("bit-cht-anim-bars-v"));
        Assert.AreEqual(0.6, Seconds(await ComputedStyleAsync("#cht-animated g.bit-cht-data", "animation-duration")), 0.001);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.IsTrue(Seconds(await ComputedStyleAsync("#cht-animated g.bit-cht-data", "animation-duration")) < 0.001);
        await Expect(Page.Locator("#cht-animated-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.AreEqual(0.6, Seconds(await ComputedStyleAsync("#cht-animated-forced g.bit-cht-data", "animation-duration")), 0.001);

        // Without the animation the chart stands in its final state.
        await Expect(Page.Locator("#cht-animated g.bit-cht-data")).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
    }

    [TestMethod]
    public async Task Responsive_TheChartFollowsTheWidthOfItsContainer()
    {
        await OpenAsync(Route);

        var svg = Page.Locator("#cht-responsive svg.bit-cht-svg");
        await Expect(svg).ToHaveAttributeAsync("viewBox", "0 0 600 300");
        await Page.Locator("#btn-resize").ClickAsync();
        await Expect(svg).ToHaveAttributeAsync("viewBox", "0 0 300 150");
        var rects = await RectsAsync("cht-responsive");
        Assert.IsTrue(rects.All(r => r.X + r.W <= 300));
    }

    [TestMethod]
    public async Task Runtime_NewDataAndTypeRedrawTheChart()
    {
        await OpenAsync(Route);

        await Expect(Elements("cht-runtime", "rect")).ToHaveCountAsync(2);
        await Page.Locator("#btn-runtime-data").ClickAsync();
        await Expect(Elements("cht-runtime", "rect")).ToHaveCountAsync(3);
        var boxes = await RectsAsync("cht-runtime");
        Assert.IsTrue(boxes[0].H > boxes[2].H);

        await Page.Locator("#btn-runtime-type").ClickAsync();
        await Expect(Elements("cht-runtime", "rect")).ToHaveCountAsync(0);
        await Expect(Elements("cht-runtime", "circle")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#cht-runtime svg.bit-cht-svg")).ToHaveAttributeAsync("aria-label", "Line chart with 1 data series.");
    }

    [TestMethod]
    public async Task Lifecycle_Prerendered_TheServerHtmlCarriesTheSvg()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        var start = html.IndexOf("id=\"cht-bar\"", StringComparison.Ordinal);
        Assert.IsTrue(start > 0);
        var segment = html[start..html.IndexOf("id=\"cht-bar-h\"", StringComparison.Ordinal)];
        StringAssert.Contains(segment, "<svg");
        Assert.AreEqual(8, Regex.Matches(segment, "<g class=\"bit-cht-el\"").Count);
        StringAssert.Contains(segment, "<table");
    }

    [TestMethod]
    public async Task Lifecycle_Unmounted_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-unmount").ScrollIntoViewIfNeededAsync();
        var plot = (await Page.Locator("#cht-unmount .bit-cht-plot").BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(plot.X + plot.Width / 2, plot.Y + plot.Height / 2);
        await Page.Mouse.WheelAsync(0, -100);
        await Page.Locator("#btn-unmount").DispatchEventAsync("click");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#cht-unmount")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#cht-unmount svg")).ToHaveCountAsync(1);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // The plot area of a chart on the screen: the clip rectangle of its data, mapped from the viewBox.
    private Task<double[]> PlotAreaAsync(string id)
    {
        return Page.Locator($"#{id} svg defs clipPath rect").EvaluateAsync<double[]>(@"r => {
            const svg = r.ownerSVGElement; const box = svg.getBoundingClientRect(); const vb = svg.viewBox.baseVal;
            const k = Math.min(box.width / vb.width, box.height / vb.height);
            const ox = box.left + (box.width - vb.width * k) / 2, oy = box.top + (box.height - vb.height * k) / 2;
            return [ox + r.x.baseVal.value * k, oy + r.y.baseVal.value * k, r.width.baseVal.value * k, r.height.baseVal.value * k];
        }");
    }

    private ILocator Elements(string id, string shape) => Page.Locator($"#{id} g.bit-cht-data > g.bit-cht-el > {shape}");

    private async Task<(double X, double Y, double W, double H)[]> RectsAsync(string id)
    {
        var values = await Elements(id, "rect").EvaluateAllAsync<double[][]>(
            "rs => rs.map(r => [r.x.baseVal.value, r.y.baseVal.value, r.width.baseVal.value, r.height.baseVal.value])");
        return values.Select(v => (v[0], v[1], v[2], v[3])).ToArray();
    }

    private async Task<(double Cx, double Cy, double R)[]> CirclesAsync(string id)
    {
        var values = await Elements(id, "circle").EvaluateAllAsync<double[][]>(
            "cs => cs.map(c => [c.cx.baseVal.value, c.cy.baseVal.value, c.r.baseVal.value])");
        return values.Select(v => (v[0], v[1], v[2])).ToArray();
    }

    private Task<double[]> SliceAnglesAsync(string id)
    {
        // Each slice is "M cx cy L x1 y1 A r r 0 large sweep x2 y2 Z": the angle between the two radii it is cut by.
        return Elements(id, "path").EvaluateAllAsync<double[]>(@"ps => ps.map(p => {
            const n = p.getAttribute('d').match(/-?[\d.]+/g).map(Number);
            const [cx, cy, x1, y1] = n; const large = n[7]; const x2 = n[9], y2 = n[10];
            let a = Math.abs(Math.atan2(y2 - cy, x2 - cx) - Math.atan2(y1 - cy, x1 - cx)) * 180 / Math.PI;
            if (a > 180 && !large) a = 360 - a;
            if (a < 180 && large) a = 360 - a;
            return a;
        })");
    }

    private static (double Min, double Max) Range(string text)
    {
        var parts = text.Split('|');
        return (double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
    }

    private static double Seconds(string value)
    {
        var first = value.Split(',')[0].Trim();
        return first.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(first[..^2], System.Globalization.CultureInfo.InvariantCulture) / 1000
            : double.Parse(first[..^1], System.Globalization.CultureInfo.InvariantCulture);
    }
}
