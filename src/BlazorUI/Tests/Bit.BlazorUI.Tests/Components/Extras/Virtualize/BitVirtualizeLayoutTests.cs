using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Virtualize;

/// <summary>
/// Covers the layouts of the Virtualize beyond a plain list - the grid of Lanes and a list an ancestor scrolls
/// (ScrollerSelector) - and the template that receives the index of each item: the tracks it virtualizes, where each
/// item is placed, how the keys move across and along the lanes, how measured items size their track, how much of a
/// list partly in view is rendered, and what the IndexedItemTemplate is handed.
/// </summary>
[TestClass]
public class BitVirtualizeLayoutTests : BunitTestContext
{
    private const string SetupFn = "BitBlazorUI.Virtualize.setup";
    private const string ScrollToOffsetFn = "BitBlazorUI.Virtualize.scrollToOffset";

    private static readonly RenderFragment<int> itemTemplate = item => builder => builder.AddContent(0, $"Item {item}");

    private void SetupViewport(double viewportSize = 300, double crossSize = 0)
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>(SetupFn, _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = viewportSize, ScrollOffset = 0, CrossSize = crossSize });
    }

    private IRenderedComponent<BitVirtualize<int>> RenderGrid(int count = 100, int lanes = 3, float itemSize = 100, Action<ComponentParameterCollectionBuilder<BitVirtualize<int>>>? extra = null)
    {
        return RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, count).ToArray());
            parameters.Add(p => p.ItemSize, itemSize);
            parameters.Add(p => p.Lanes, lanes);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            extra?.Invoke(parameters);
        });
    }

    private static int[] RenderedIndices(IRenderedComponent<BitVirtualize<int>> component) =>
        component.FindAll(".bit-vir-blk > .bit-vir-itm").Select(e => int.Parse(e.GetAttribute("data-bit-vir-index")!, CultureInfo.InvariantCulture)).ToArray();

    private static double TranslateOf(string? style)
    {
        var match = Regex.Match(style ?? "", @"translateY\((-?[\d.]+)px\)");
        return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static int Active(IRenderedComponent<BitVirtualize<int>> component) =>
        int.Parse(component.Find(".bit-vir-itm[tabindex='0']").GetAttribute("data-bit-vir-index")!, CultureInfo.InvariantCulture);



    [TestMethod]
    public void BitVirtualizeLanesShouldVirtualizeRowsOfItems()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 3, 100);

        // 34 rows of 100px; 3 rows in view plus 3 rows of overscan, of 3 items each.
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:3400px");
        CollectionAssert.AreEqual(Enumerable.Range(0, 18).ToArray(), RenderedIndices(component));
    }

    [TestMethod]
    public void BitVirtualizeLanesShouldPlaceEachItemInItsRowAndLane()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 3, 100);

        var style = component.Find("[data-bit-vir-index='4']").GetAttribute("style");

        Assert.AreEqual(100, TranslateOf(style));
        StringAssert.Contains(style, "height:100px");
        StringAssert.Contains(style, "inset-inline-start:calc(100% / 3 * 1)");
        StringAssert.Contains(style, "width:calc(100% / 3)");
    }

    [TestMethod]
    public void BitVirtualizeLanesShouldPlaceTheLanesOfAHorizontalListAcrossItsHeight()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 2, 100, p => p.Add(x => x.Horizontal, true));

        var style = component.Find("[data-bit-vir-index='3']").GetAttribute("style");

        StringAssert.Contains(style, "inset-inline-start:100px");
        StringAssert.Contains(style, "inset-block-start:calc(100% / 2 * 1)");
        StringAssert.Contains(style, "height:calc(100% / 2)");
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "width:5000px");
    }

    [TestMethod]
    public async Task BitVirtualizeLanesShouldRenderTheRowsAroundTheScrollOffset()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 3, 100);

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));

        // Rows 10-12 in view, rows 7-15 rendered.
        CollectionAssert.AreEqual(Enumerable.Range(21, 27).ToArray(), RenderedIndices(component));
    }

    [TestMethod]
    public void BitVirtualizeLanesShouldRenderThePartlyFilledLastRow()
    {
        SetupViewport(1000);

        var component = RenderGrid(10, 4, 100);

        CollectionAssert.AreEqual(Enumerable.Range(0, 10).ToArray(), RenderedIndices(component));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:300px");
    }

    [TestMethod]
    public async Task BitVirtualizeLanesShouldScrollToTheRowOfAnItem()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 3, 100);

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(31));

        Assert.AreEqual(1000d, Convert.ToDouble(Context.JSInterop.Invocations[ScrollToOffsetFn][^1].Arguments[1], CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task BitVirtualizeLanesKeyboardShouldMoveAlongAndAcrossTheLanes()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 3, 100);

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));
        Assert.AreEqual(0, Active(component));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));
        Assert.AreEqual(3, Active(component));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowRight"));
        Assert.AreEqual(4, Active(component));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowUp"));
        Assert.AreEqual(1, Active(component));

        // Up from the first row stays put rather than wrapping to the start of the lane.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowUp"));
        Assert.AreEqual(1, Active(component));

        // A page is the rows in view less one, of 3 items each.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("PageDown"));
        Assert.AreEqual(7, Active(component));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("End"));
        Assert.AreEqual(99, Active(component));

        // Down from the last row stays put.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));
        Assert.AreEqual(99, Active(component));
    }

    [TestMethod]
    public async Task BitVirtualizeLanesKeyboardShouldReachThePartlyFilledLastRow()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 3, 100);

        // Item 98 is in row 32; the row below holds only item 99, which a step down from any lane reaches.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown", 98));
        Assert.AreEqual(99, Active(component));
    }

    [TestMethod]
    public async Task BitVirtualizeLanesKeyboardShouldMoveAlongTheLanesOfAHorizontalListWithTheSideArrows()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 2, 100, p => p.Add(x => x.Horizontal, true));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowRight"));
        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowRight"));
        Assert.AreEqual(2, Active(component));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));
        Assert.AreEqual(3, Active(component));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicLanesShouldSizeEachRowByItsLargestItem()
    {
        SetupViewport(300);

        var component = RenderGrid(30, 3, 100, p =>
        {
            p.Add(x => x.Dynamic, true);
            p.Add(x => x.EstimatedItemSize, 50);
        });

        // 10 rows estimated at 50px.
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:500px");

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0, 1, 2], [80d, 120d, 60d]));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:570px");
        Assert.AreEqual(120, TranslateOf(component.Find("[data-bit-vir-index='3']").GetAttribute("style")));

        // The tallest item shrinks: the row follows the next tallest instead of keeping the old size.
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([1], [40d]));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:530px");
    }

    [TestMethod]
    public void BitVirtualizeLanesShouldRelayoutWhenTheLanesChange()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 100);

        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:10000px");
        Assert.IsFalse(component.Find("[data-bit-vir-index='1']").GetAttribute("style")!.Contains("calc("));

        component.Render(p => p.Add(x => x.Lanes, 4));

        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:2500px");
        StringAssert.Contains(component.Find("[data-bit-vir-index='1']").GetAttribute("style"), "width:calc(100% / 4)");
    }

    [TestMethod]
    public void BitVirtualizeMinLaneSizeShouldFitAsManyLanesAsTheWidthHolds()
    {
        SetupViewport(300, 1000);

        var component = RenderGrid(100, 1, 100, p => p.Add(x => x.MinLaneSize, 220f));

        // 1000px holds 4 lanes of at least 220px; 25 rows of 100px.
        StringAssert.Contains(component.Find("[data-bit-vir-index='1']").GetAttribute("style"), "width:calc(100% / 4)");
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:2500px");
    }

    [TestMethod]
    public async Task BitVirtualizeMinLaneSizeShouldFollowTheWidthOfTheList()
    {
        SetupViewport(300, 1000);

        var component = RenderGrid(100, 1, 100, p => p.Add(x => x.MinLaneSize, 220f));

        await component.InvokeAsync(() => component.Instance._Scroll(0, 300, -1, 500));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:5000px");

        // Narrower than a single lane still has one.
        await component.InvokeAsync(() => component.Instance._Scroll(0, 300, -1, 100));
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:10000px");
    }

    [TestMethod]
    public void BitVirtualizeMinLaneSizeShouldFallBackToLanesUntilTheListIsMeasured()
    {
        var component = RenderGrid(100, 2, 100, p => p.Add(x => x.MinLaneSize, 220f));

        StringAssert.Contains(component.Find("[data-bit-vir-index='1']").GetAttribute("style"), "width:calc(100% / 2)");
    }

    [TestMethod]
    public async Task BitVirtualizeLanesShouldKeepTheFirstItemInViewWhenTheLanesChange()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50);

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300)); // item 20 first in view

        component.Render(p => p.Add(x => x.Lanes, 4));

        // Item 20 is now in row 5: the render carries the scroll that keeps it first in view.
        StringAssert.StartsWith(component.Find(".bit-vir-spc").GetAttribute("data-bit-vir-scroll"), "o:250:");
        Assert.AreEqual(20, RenderedIndices(component).First(i => i >= 20));
    }

    [TestMethod]
    public void BitVirtualizeLanesShouldTreatLessThanOneLaneAsOne()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 0, 100);

        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:10000px");
    }



    [TestMethod]
    public void BitVirtualizeGapShouldSpaceTheItemsAlongTheScrollAxis()
    {
        SetupViewport(300);

        var component = RenderGrid(10, 1, 50, p => p.Add(x => x.Gap, 10f));

        // 10 items of 50px and the 9 gaps between them.
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:590px");
        Assert.AreEqual(120, TranslateOf(component.Find("[data-bit-vir-index='2']").GetAttribute("style")));
        StringAssert.Contains(component.Find("[data-bit-vir-index='2']").GetAttribute("style"), "height:50px");
    }

    [TestMethod]
    public async Task BitVirtualizeGapShouldBeTakenIntoAccountWhenScrolling()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50, p => p.Add(x => x.Gap, 10f));

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(20));
        Assert.AreEqual(1200d, Convert.ToDouble(Context.JSInterop.Invocations[ScrollToOffsetFn][^1].Arguments[1], CultureInfo.InvariantCulture));

        // 1200px is the start of item 20 (5 in view, 3 of overscan on each side).
        await component.InvokeAsync(() => component.Instance._Scroll(1200, 300));
        CollectionAssert.AreEqual(Enumerable.Range(17, 11).ToArray(), RenderedIndices(component));
    }

    [TestMethod]
    public void BitVirtualizeGapShouldSpaceTheLanesOfAGrid()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 3, 100, p => p.Add(x => x.Gap, 8f));

        var style = component.Find("[data-bit-vir-index='4']").GetAttribute("style");

        // The second row, in the second lane: 2 gaps of 8px are shared out by the 3 lanes.
        Assert.AreEqual(108, TranslateOf(style));
        StringAssert.Contains(style, "inset-inline-start:calc((100% - 16px) / 3 * 1 + 8px)");
        StringAssert.Contains(style, "width:calc((100% - 16px) / 3)");
    }

    [TestMethod]
    public void BitVirtualizeGapShouldBeLeftOutOfTheLanesThatFit()
    {
        SetupViewport(300, 1000);

        // 4 lanes of 220px and their 3 gaps of 40px take 1000px exactly.
        var component = RenderGrid(100, 1, 100, p =>
        {
            p.Add(x => x.MinLaneSize, 220f);
            p.Add(x => x.Gap, 40f);
        });

        StringAssert.Contains(component.Find("[data-bit-vir-index='1']").GetAttribute("style"), "width:calc((100% - 120px) / 4)");

        component.Render(p => p.Add(x => x.Gap, 41f));

        StringAssert.Contains(component.Find("[data-bit-vir-index='1']").GetAttribute("style"), "width:calc((100% - 82px) / 3)");
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicGapShouldBeAddedToTheMeasuredSizes()
    {
        SetupViewport(300);

        var component = RenderGrid(10, 1, 100, p =>
        {
            p.Add(x => x.Dynamic, true);
            p.Add(x => x.EstimatedItemSize, 50);
            p.Add(x => x.Gap, 10f);
        });

        // 10 items estimated at 50px, 9 gaps.
        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:590px");

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [80d]));

        StringAssert.Contains(component.Find(".bit-vir-spc").GetAttribute("style"), "height:620px");
        Assert.AreEqual(90, TranslateOf(component.Find("[data-bit-vir-index='1']").GetAttribute("style")));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderTheItemAttributesOnTheElementOfEachItem()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50, p =>
        {
            p.Add(x => x.Role, "listbox");
            p.Add(x => x.ItemRole, "option");
            p.Add(x => x.Classes, new() { Item = "own-class" });
            p.Add(x => x.ItemAttributes, i => new Dictionary<string, object>
            {
                ["aria-selected"] = i == 1 ? "true" : "false",
                ["class"] = i == 1 ? "selected" : "",
                ["style"] = "color:red",
                ["role"] = i == 2 ? "presentation" : "option",
                ["tabindex"] = "5",
                ["data-bit-vir-index"] = "99",
            });
        });

        var item = component.Find(".bit-vir-itm.selected");

        Assert.AreEqual("1", item.GetAttribute("data-bit-vir-index"));
        Assert.AreEqual("true", item.GetAttribute("aria-selected"));
        Assert.AreEqual("false", component.Find("[data-bit-vir-index='0']").GetAttribute("aria-selected"));

        // The class and the style are appended to the component's own, ...
        Assert.IsTrue(item.ClassList.Contains("bit-vir-itm"));
        Assert.IsTrue(item.ClassList.Contains("own-class"));
        StringAssert.Contains(item.GetAttribute("style"), "translateY(50px)");
        StringAssert.Contains(item.GetAttribute("style"), "color:red");

        // ... the role overrides the ItemRole, and what the component relies on is left to it.
        Assert.AreEqual("presentation", component.Find("[data-bit-vir-index='2']").GetAttribute("role"));
        Assert.AreEqual("-1", item.GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitVirtualizeScrollerSelectorShouldHandTheScrollingToTheBrowserSide()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50, p => p.Add(x => x.ScrollerSelector, "window"));

        Assert.IsTrue(component.Find(".bit-vir").ClassList.Contains("bit-vir-ext"));
        Assert.AreEqual("window", Context.JSInterop.Invocations[SetupFn][0].Arguments[5]);
    }

    [TestMethod]
    public void BitVirtualizeShouldNotHaveTheExternalClassWithoutAScrollerSelector()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50);

        Assert.IsFalse(component.Find(".bit-vir").ClassList.Contains("bit-vir-ext"));
        Assert.IsNull(Context.JSInterop.Invocations[SetupFn][0].Arguments[5]);
    }

    [TestMethod]
    public void BitVirtualizeShouldSetUpTheBrowserSideAgainForAnotherScroller()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50);

        component.Render(p => p.Add(x => x.ScrollerSelector, ".page"));

        var setups = Context.JSInterop.Invocations[SetupFn];
        Assert.AreEqual(2, setups.Count);
        Assert.AreEqual(".page", setups[1].Arguments[5]);
    }

    [TestMethod]
    public async Task BitVirtualizeShouldRenderOnlyThePartOfAListPartlyInView()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50, p => p.Add(x => x.ScrollerSelector, "window"));

        // The list starts 200px into the 300px viewport: only its first 100px show, so 2 items plus the overscan.
        await component.InvokeAsync(() => component.Instance._Scroll(-200, 300));
        CollectionAssert.AreEqual(Enumerable.Range(0, 5).ToArray(), RenderedIndices(component));

        // Entirely below the viewport: the first item stands ready.
        await component.InvokeAsync(() => component.Instance._Scroll(-5000, 300));
        CollectionAssert.AreEqual(Enumerable.Range(0, 4).ToArray(), RenderedIndices(component));
    }

    [TestMethod]
    public void BitVirtualizeIndexedItemTemplateShouldReceiveTheIndexOfEachItem()
    {
        SetupViewport(300);

        RenderFragment<BitVirtualizeItemContext<string>> indexed = context => builder => builder.AddContent(0, $"{context.Index}:{context.Item}");

        var component = RenderComponent<BitVirtualize<string>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).Select(i => $"row{i}").ToArray());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.IndexedItemTemplate, indexed);
            // The indexed template takes precedence.
            parameters.Add(p => p.ItemTemplate, (RenderFragment<string>)(item => builder => builder.AddContent(0, "plain")));
        });

        Assert.AreEqual("3:row3", component.Find("[data-bit-vir-index='3']").TextContent);
        Assert.IsFalse(component.Markup.Contains("plain"));
    }

    [TestMethod]
    public async Task BitVirtualizeIndexedItemTemplateShouldRenderThePinnedStickyItemWithItsIndex()
    {
        SetupViewport(300);

        RenderFragment<BitVirtualizeItemContext<int>> indexed = context => builder => builder.AddContent(0, $"#{context.Index}");

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 100).ToArray());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.IndexedItemTemplate, indexed);
            parameters.Add(p => p.IsStickyItem, i => i % 10 == 0);
        });

        await component.InvokeAsync(() => component.Instance._Scroll(600, 300));

        Assert.AreEqual("#10", component.Find(".bit-vir-stk").TextContent);
    }

    [TestMethod]
    public void BitVirtualizeShouldLeaveOutThePositionOfItemsWithoutARole()
    {
        SetupViewport(300);

        var component = RenderGrid(100, 1, 50, p => p.Add(x => x.ItemRole, null));

        var item = component.Find("[data-bit-vir-index='0']");

        Assert.IsFalse(item.HasAttribute("role"));
        Assert.IsFalse(item.HasAttribute("aria-setsize"));
        Assert.IsFalse(item.HasAttribute("aria-posinset"));
    }
}
