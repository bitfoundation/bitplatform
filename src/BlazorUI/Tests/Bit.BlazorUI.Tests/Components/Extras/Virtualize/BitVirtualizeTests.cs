using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Virtualize;

[TestClass]
public class BitVirtualizeTests : BunitTestContext
{
    private const string SetupFn = "BitBlazorUI.Virtualize.setup";
    private const string ScrollToOffsetFn = "BitBlazorUI.Virtualize.scrollToOffset";
    private const string ScrollToEdgeFn = "BitBlazorUI.Virtualize.scrollToEdge";
    private const string AdjustScrollFn = "BitBlazorUI.Virtualize.adjustScroll";
    private const string FocusIndexFn = "BitBlazorUI.Virtualize.focusIndex";
    private const string UpdateFn = "BitBlazorUI.Virtualize.update";
    private const string SyncFn = "BitBlazorUI.Virtualize.sync";
    private const string DisposeFn = "BitBlazorUI.Virtualize.dispose";

    private static readonly RenderFragment<int> itemTemplate = item => builder => builder.AddContent(0, $"Item {item}");

    // Makes the browser side report a viewport, which is what initializes the component (without it, as in
    // prerendering, the component renders a first screen of items and waits).
    private void SetupViewport(double viewportSize = 300, double scrollOffset = 0)
    {
        Context.JSInterop.Setup<BitVirtualizeMetrics?>(SetupFn, _ => true)
                         .SetResult(new BitVirtualizeMetrics { ViewportSize = viewportSize, ScrollOffset = scrollOffset });
    }

    private IReadOnlyList<JSRuntimeInvocation> Calls(string identifier) => Context.JSInterop.Invocations[identifier];

    private double LastScrollToOffset() => Convert.ToDouble(Calls(ScrollToOffsetFn)[^1].Arguments[1], CultureInfo.InvariantCulture);

    private static int[] RenderedIndices(IRenderedComponent<BitVirtualize<int>> component) =>
        component.FindAll(".bit-vir-blk > .bit-vir-itm").Select(e => int.Parse(e.GetAttribute("data-bit-vir-index")!, CultureInfo.InvariantCulture)).ToArray();

    private static double TranslateOf(string? style)
    {
        var match = Regex.Match(style ?? "", @"translateY\((-?[\d.]+)px\)");
        return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private IRenderedComponent<BitVirtualize<int>> RenderList(int count = 100, float itemSize = 50, Action<ComponentParameterCollectionBuilder<BitVirtualize<int>>>? extra = null)
    {
        return RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, count).ToArray());
            parameters.Add(p => p.ItemSize, itemSize);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            extra?.Invoke(parameters);
        });
    }



    // ---------------------------------------------------------------- rendering

    [TestMethod]
    public void BitVirtualizeShouldRenderInitialItemsFromItems()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 1_000).ToArray());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Assert.IsTrue(component.Markup.Contains("bit-vir"));
        Assert.IsTrue(component.Markup.Contains("bit-vir-spc"));
        Assert.IsTrue(component.Markup.Contains("Item 0"));
        Assert.IsTrue(component.Markup.Contains("Item 1"));
        Assert.IsFalse(component.Markup.Contains("Item 999"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderOnlyTheVisibleWindowPlusOverscan()
    {
        SetupViewport(300);

        var component = RenderList(extra: p => p.Add(x => x.OverscanCount, 2));

        // 300px / 50px = items 0..5 in view, plus 2 overscan items after them.
        CollectionAssert.AreEqual(Enumerable.Range(0, 8).ToArray(), RenderedIndices(component));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderChildContentAsTheItemTemplate()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.ChildContent, item => builder => builder.AddContent(0, $"Child {item}"));
        });

        Assert.IsTrue(component.Markup.Contains("Child 0"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderSpacerWithTotalSize()
    {
        var component = RenderList(100, 56);

        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("height:5600px", spacer.GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderHorizontalClassAndSpacer()
    {
        var component = RenderList(100, 50, p => p.Add(x => x.Horizontal, true));

        var root = component.Find(".bit-vir");
        Assert.IsTrue(root.ClassList.Contains("bit-vir-hor"));

        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("width:5000px", spacer.GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeHorizontalShouldPositionItemsWithTheLogicalInlineStart()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.Horizontal, true));

        // inset-inline-start makes the items lay out from the right in RTL, unlike a physical translateX.
        var second = component.Find("[data-bit-vir-index='1']");
        Assert.AreEqual("inset-inline-start:50px;width:50px", second.GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-vir-blk").GetAttribute("style")!.StartsWith("inset-inline-start:"));
    }

    [TestMethod]
    public void BitVirtualizeFixedItemsShouldBePinnedToTheItemSize()
    {
        SetupViewport(300);

        var component = RenderList(100, 40);

        var second = component.Find("[data-bit-vir-index='1']");
        Assert.AreEqual("transform:translateY(40px);height:40px", second.GetAttribute("style"));
        Assert.IsTrue(second.ClassList.Contains("bit-vir-fix"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderTranslatedBlockWrapper()
    {
        var component = RenderList(100, 50);

        // The rendered window lives inside a translated block so the extent stays reachable past the
        // browser's max-element-size limit; items are direct children of that block.
        var block = component.Find(".bit-vir-spc > .bit-vir-blk");
        Assert.IsNotNull(block);
        Assert.IsTrue(block.GetAttribute("style")!.Contains("translateY"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderEmptyTemplateWhenNoItems()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<int>());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.EmptyTemplate, builder => builder.AddContent(0, "No data"));
        });

        Assert.AreEqual("No data", component.Find(".bit-vir-emp").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-vir-spc").Count);
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderHeaderAndFooterAroundTheItems()
    {
        var component = RenderList(100, 50, p =>
        {
            p.Add(x => x.HeaderTemplate, b => b.AddContent(0, "The header"));
            p.Add(x => x.FooterTemplate, b => b.AddContent(0, "The footer"));
        });

        var children = component.Find(".bit-vir").Children;
        Assert.AreEqual(3, children.Length);
        Assert.IsTrue(children[0].ClassList.Contains("bit-vir-hdr"));
        Assert.AreEqual("The header", children[0].TextContent);
        Assert.IsTrue(children[1].ClassList.Contains("bit-vir-spc"));
        Assert.IsTrue(children[2].ClassList.Contains("bit-vir-ftr"));
        Assert.AreEqual("The footer", children[2].TextContent);
    }

    [TestMethod]
    public void BitVirtualizeShouldKeepHeaderAndFooterAroundTheEmptyTemplate()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Array.Empty<int>());
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.HeaderTemplate, b => b.AddContent(0, "H"));
            parameters.Add(p => p.FooterTemplate, b => b.AddContent(0, "F"));
            parameters.Add(p => p.EmptyTemplate, b => b.AddContent(0, "E"));
        });

        var children = component.Find(".bit-vir").Children;
        CollectionAssert.AreEqual(new[] { "H", "E", "F" }, children.Select(c => c.TextContent).ToArray());
    }

    [TestMethod]
    public void BitVirtualizeShouldRespectClassAndStyle()
    {
        var component = RenderList(10, 50, p =>
        {
            p.Add(x => x.Class, "custom-class");
            p.Add(x => x.Style, "height:100px");
        });

        var root = component.Find(".bit-vir");
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("height:100px"));
    }

    [TestMethod]
    public void BitVirtualizeShouldApplyClassesToEveryPart()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p =>
        {
            p.Add(x => x.IsStickyItem, i => i % 10 == 0);
            p.Add(x => x.HeaderTemplate, b => b.AddContent(0, "H"));
            p.Add(x => x.FooterTemplate, b => b.AddContent(0, "F"));
            p.Add(x => x.Classes, new BitVirtualizeClassStyles { Root = "c-root", Header = "c-hdr", Footer = "c-ftr", Item = "c-itm", Sticky = "c-stk" });
        });

        Assert.IsTrue(component.Find(".bit-vir").ClassList.Contains("c-root"));
        Assert.IsTrue(component.Find(".bit-vir-hdr").ClassList.Contains("c-hdr"));
        Assert.IsTrue(component.Find(".bit-vir-ftr").ClassList.Contains("c-ftr"));
        Assert.IsTrue(component.FindAll(".bit-vir-itm").All(e => e.ClassList.Contains("c-itm")));
        Assert.IsTrue(component.Find(".bit-vir-stk").ClassList.Contains("c-stk"));
    }

    [TestMethod]
    public void BitVirtualizeShouldApplyStylesToEveryPart()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p =>
        {
            p.Add(x => x.IsStickyItem, i => i % 10 == 0);
            p.Add(x => x.HeaderTemplate, b => b.AddContent(0, "H"));
            p.Add(x => x.FooterTemplate, b => b.AddContent(0, "F"));
            p.Add(x => x.Styles, new BitVirtualizeClassStyles { Root = "color:red", Header = "color:green", Footer = "color:blue", Item = "color:gray", Sticky = "color:pink" });
        });

        Assert.IsTrue(component.Find(".bit-vir").GetAttribute("style")!.Contains("color:red"));
        Assert.AreEqual("color:green", component.Find(".bit-vir-hdr").GetAttribute("style"));
        Assert.AreEqual("color:blue", component.Find(".bit-vir-ftr").GetAttribute("style"));
        // The part style is appended to the positioning style, never replacing it.
        Assert.AreEqual("transform:translateY(50px);height:50px;color:gray", component.Find("[data-bit-vir-index='1']").GetAttribute("style"));
        Assert.AreEqual("height:50px;color:pink", component.Find(".bit-vir-stk").GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldApplyClassesAndStylesToTheEmptyAndLoadingParts()
    {
        var empty = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Array.Empty<int>());
            parameters.Add(p => p.EmptyTemplate, b => b.AddContent(0, "E"));
            parameters.Add(p => p.Classes, new BitVirtualizeClassStyles { Empty = "c-emp" });
            parameters.Add(p => p.Styles, new BitVirtualizeClassStyles { Empty = "color:red" });
        });

        Assert.IsTrue(empty.Find(".bit-vir-emp").ClassList.Contains("c-emp"));
        Assert.AreEqual("color:red", empty.Find(".bit-vir-emp").GetAttribute("style"));

        var loading = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => new ValueTask<BitVirtualizeItemsProviderResult<int>>(new TaskCompletionSource<BitVirtualizeItemsProviderResult<int>>().Task));
            parameters.Add(p => p.LoadingTemplate, b => b.AddContent(0, "L"));
            parameters.Add(p => p.Classes, new BitVirtualizeClassStyles { Loading = "c-ldg" });
            parameters.Add(p => p.Styles, new BitVirtualizeClassStyles { Loading = "color:blue" });
        });

        Assert.IsTrue(loading.Find(".bit-vir-ldg").ClassList.Contains("c-ldg"));
        Assert.AreEqual("color:blue", loading.Find(".bit-vir-ldg").GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeAlignToEndShouldAddItsClass()
    {
        var component = RenderList(10, 50, p => p.Add(x => x.AlignToEnd, true));

        Assert.IsTrue(component.Find(".bit-vir").ClassList.Contains("bit-vir-ate"));

        component.Render(p => p.Add(x => x.AlignToEnd, false));

        Assert.IsFalse(component.Find(".bit-vir").ClassList.Contains("bit-vir-ate"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderTheDirection()
    {
        var component = RenderList(10, 50, p => p.Add(x => x.Dir, BitDir.Rtl));

        var root = component.Find(".bit-vir");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }



    // ---------------------------------------------------------------- accessibility

    [TestMethod]
    public void BitVirtualizeShouldRenderListSemanticsWithPositionInTheWholeSet()
    {
        SetupViewport(300);

        var component = RenderList(1_000, 50, p => p.Add(x => x.AriaLabel, "Numbers"));

        var root = component.Find(".bit-vir");
        Assert.AreEqual("list", root.GetAttribute("role"));
        Assert.AreEqual("Numbers", root.GetAttribute("aria-label"));

        var item = component.Find("[data-bit-vir-index='2']");
        Assert.AreEqual("listitem", item.GetAttribute("role"));
        Assert.AreEqual("1000", item.GetAttribute("aria-setsize"));
        Assert.AreEqual("3", item.GetAttribute("aria-posinset"));
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderCustomRoles()
    {
        var component = RenderList(10, 50, p =>
        {
            p.Add(x => x.Role, "feed");
            p.Add(x => x.ItemRole, "article");
        });

        Assert.AreEqual("feed", component.Find(".bit-vir").GetAttribute("role"));
        Assert.IsTrue(component.FindAll(".bit-vir-itm").All(e => e.GetAttribute("role") == "article"));

        component.Render(p =>
        {
            p.Add(x => x.Role, null);
            p.Add(x => x.ItemRole, null);
        });

        Assert.IsFalse(component.Find(".bit-vir").HasAttribute("role"));
        Assert.IsFalse(component.Find(".bit-vir-itm").HasAttribute("role"));
    }

    [TestMethod]
    public void BitVirtualizeShouldHideSpacerAndBlockFromAssistiveTechnologies()
    {
        var component = RenderList(10, 50);

        Assert.AreEqual("presentation", component.Find(".bit-vir-spc").GetAttribute("role"));
        Assert.AreEqual("presentation", component.Find(".bit-vir-blk").GetAttribute("role"));
    }

    [TestMethod]
    public void BitVirtualizeRootShouldBeTabbableUntilAnItemIsActive()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        Assert.AreEqual("0", component.Find(".bit-vir").GetAttribute("tabindex"));
        Assert.IsTrue(component.FindAll(".bit-vir-itm").All(e => e.GetAttribute("tabindex") == "-1"));
    }

    [TestMethod]
    public void BitVirtualizeRootShouldUseTheProvidedTabIndex()
    {
        var component = RenderList(10, 50, p => p.Add(x => x.TabIndex, "3"));

        Assert.AreEqual("3", component.Find(".bit-vir").GetAttribute("tabindex"));
    }



    // ---------------------------------------------------------------- keyboard navigation

    [TestMethod]
    public async Task BitVirtualizeShouldMoveRovingFocusOnKeyboardNavigation()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        // Navigate to the last item; it becomes the active (roving tabindex=0) element.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("End"));

        component.WaitForAssertion(() =>
        {
            var active = component.Find("[data-bit-vir-index='99']");
            Assert.AreEqual("0", active.GetAttribute("tabindex"));
        });

        Assert.AreEqual(99, Convert.ToInt32(Calls(FocusIndexFn)[^1].Arguments[1], CultureInfo.InvariantCulture));
        Assert.AreEqual(4700, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeFirstArrowKeyShouldActivateTheFirstVisibleItem()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));

        Assert.AreEqual("0", component.Find("[data-bit-vir-index='0']").GetAttribute("tabindex"));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));

        Assert.AreEqual("-1", component.Find("[data-bit-vir-index='0']").GetAttribute("tabindex"));
        Assert.AreEqual("0", component.Find("[data-bit-vir-index='1']").GetAttribute("tabindex"));
        // While the roving item is rendered, the root leaves the tab order.
        Assert.AreEqual("-1", component.Find(".bit-vir").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitVirtualizeKeyboardNavigationShouldContinueFromTheFocusedItem()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        // The item the user clicked (focused) is reported by the browser along with the key.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown", 4));

        Assert.AreEqual("0", component.Find("[data-bit-vir-index='5']").GetAttribute("tabindex"));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowUp", 5));

        Assert.AreEqual("0", component.Find("[data-bit-vir-index='4']").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitVirtualizeKeyboardNavigationShouldSupportAllKeys()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);
        int Active() => int.Parse(component.Find(".bit-vir-itm[tabindex='0']").GetAttribute("data-bit-vir-index")!, CultureInfo.InvariantCulture);

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowRight"));
        Assert.AreEqual(0, Active());

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowRight"));
        Assert.AreEqual(1, Active());

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowLeft"));
        Assert.AreEqual(0, Active());

        // Clamped at the start.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowUp"));
        Assert.AreEqual(0, Active());

        // A page is the number of fully visible items less one (300 / 50 - 1).
        await component.InvokeAsync(() => component.Instance._KeyNavigate("PageDown"));
        Assert.AreEqual(5, Active());

        await component.InvokeAsync(() => component.Instance._KeyNavigate("End"));
        Assert.AreEqual(99, Active());

        // Clamped at the end.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));
        Assert.AreEqual(99, Active());

        await component.InvokeAsync(() => component.Instance._KeyNavigate("PageUp"));
        Assert.AreEqual(94, Active());

        await component.InvokeAsync(() => component.Instance._KeyNavigate("Home"));
        Assert.AreEqual(0, Active());

        // Unknown keys change nothing.
        await component.InvokeAsync(() => component.Instance._KeyNavigate("Enter"));
        Assert.AreEqual(0, Active());
    }

    [TestMethod]
    public async Task BitVirtualizeRootShouldReturnToTheTabOrderWhenTheActiveItemScrollsAway()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance._KeyNavigate("ArrowDown"));
        Assert.AreEqual("-1", component.Find(".bit-vir").GetAttribute("tabindex"));

        // Scroll far away with the mouse: the active item is no longer rendered, so the root must be tabbable again.
        await component.InvokeAsync(() => component.Instance._Scroll(3000, 300));

        Assert.AreEqual(0, component.FindAll("[data-bit-vir-index='0']").Count);
        Assert.AreEqual("0", component.Find(".bit-vir").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task BitVirtualizeActiveIndexShouldBeClampedWhenTheItemsShrink()
    {
        SetupViewport(300);

        var list = Enumerable.Range(0, 100).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance._KeyNavigate("End"));

        component.Render(p => p.Add(x => x.Items, Enumerable.Range(0, 5).ToList()));

        Assert.AreEqual("0", component.Find("[data-bit-vir-index='4']").GetAttribute("tabindex"));
    }



    // ---------------------------------------------------------------- scrolling

    [TestMethod]
    public void BitVirtualizeShouldPassTheOptionsToTheBrowserOnSetup()
    {
        SetupViewport(300);

        var component = RenderList(100, 40, p => p.Add(x => x.Horizontal, true));

        var setup = Calls(SetupFn)[0];
        Assert.AreEqual(component.Instance.UniqueId, setup.Arguments[0]);
        Assert.AreEqual(true, setup.Arguments[2]);  // horizontal
        Assert.AreEqual(false, setup.Arguments[3]); // dynamic
        Assert.AreEqual(40d, setup.Arguments[4]);   // one fixed item of movement may be coalesced
    }

    [TestMethod]
    public void BitVirtualizeShouldNotCoalesceScrollingInDynamicModeOrWithoutOverscan()
    {
        SetupViewport(300);

        RenderList(100, 40, p => p.Add(x => x.Dynamic, true));
        RenderList(100, 40, p => p.Add(x => x.OverscanCount, 0));

        Assert.AreEqual(0d, Calls(SetupFn)[0].Arguments[4]);
        Assert.AreEqual(0d, Calls(SetupFn)[1].Arguments[4]);
    }

    [TestMethod]
    public async Task BitVirtualizeShouldRenderTheWindowAtTheReportedScrollOffset()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.OverscanCount, 2));

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));

        // Items 20..25 are in view; 2 overscan items on each side.
        CollectionAssert.AreEqual(Enumerable.Range(18, 10).ToArray(), RenderedIndices(component));
        Assert.AreEqual("transform:translateY(900px)", component.Find(".bit-vir-blk").GetAttribute("style"));
        Assert.AreEqual("transform:translateY(100px);height:50px", component.Find("[data-bit-vir-index='20']").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeShouldReportTheVisibleRange()
    {
        SetupViewport(300);

        var ranges = new List<(int Start, int End)>();
        var component = RenderList(100, 50, p => p.Add(x => x.OnVisibleRangeChanged, range => ranges.Add(range)));

        Assert.AreEqual((0, 6), ranges[^1]);

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));

        // The item starting exactly at the bottom edge (26) is not in view.
        Assert.AreEqual((20, 26), ranges[^1]);

        var count = ranges.Count;
        await component.InvokeAsync(() => component.Instance._Scroll(1010, 300));

        // A partially scrolled item still counts; the range grew by the item peeking in at the bottom.
        Assert.AreEqual((20, 27), ranges[^1]);
        Assert.AreEqual(count + 1, ranges.Count);

        await component.InvokeAsync(() => component.Instance._Scroll(1011, 300));

        // An unchanged range is not reported again.
        Assert.AreEqual(count + 1, ranges.Count);
    }

    [TestMethod]
    public async Task BitVirtualizeScrollToIndexShouldHonorTheAlignment()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(20, BitVirtualizeScrollAlignment.Start));
        Assert.AreEqual(1000, LastScrollToOffset());

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(20, BitVirtualizeScrollAlignment.Center));
        Assert.AreEqual(875, LastScrollToOffset());

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(20, BitVirtualizeScrollAlignment.End));
        Assert.AreEqual(750, LastScrollToOffset());

        // Auto: already fully visible (750..1050 shows 15..20) -> stays.
        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(20, BitVirtualizeScrollAlignment.Auto));
        Assert.AreEqual(750, LastScrollToOffset());

        // Auto: above the viewport -> aligned to the start.
        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(2, BitVirtualizeScrollAlignment.Auto));
        Assert.AreEqual(100, LastScrollToOffset());

        // Auto: below the viewport -> aligned to the end.
        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(30, BitVirtualizeScrollAlignment.Auto));
        Assert.AreEqual(1250, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeScrollToIndexShouldClampTheIndexAndTheOffset()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(5_000));
        Assert.AreEqual(4700, LastScrollToOffset());

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(-3));
        Assert.AreEqual(0, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeInstantScrollShouldRenderTheTargetWindowRightAway()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(60));

        // Rendered without waiting for the browser to report the new scroll position.
        Assert.AreEqual(1, component.FindAll("[data-bit-vir-index='60']").Count);
        Assert.AreEqual(false, Calls(ScrollToOffsetFn)[^1].Arguments[2]);
    }

    [TestMethod]
    public async Task BitVirtualizeSmoothScrollShouldBePassedToTheBrowser()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(60, smooth: true));

        Assert.AreEqual(3000, LastScrollToOffset());
        Assert.AreEqual(true, Calls(ScrollToOffsetFn)[^1].Arguments[2]);
        // The window follows the browser as it animates, instead of jumping ahead.
        Assert.AreEqual(0, component.FindAll("[data-bit-vir-index='60']").Count);
    }

    [TestMethod]
    public async Task BitVirtualizeScrollToOffsetAndScrollByShouldScrollInPixels()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance.ScrollToOffsetAsync(1234));
        Assert.AreEqual(1234, LastScrollToOffset());

        await component.InvokeAsync(() => component.Instance.ScrollByAsync(100));
        Assert.AreEqual(1334, LastScrollToOffset());

        await component.InvokeAsync(() => component.Instance.ScrollByAsync(-2000));
        Assert.AreEqual(0, LastScrollToOffset());

        await component.InvokeAsync(() => component.Instance.ScrollToOffsetAsync(1_000_000));
        Assert.AreEqual(4700, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeScrollToStartAndEndShouldScrollToTheRealEdges()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance.ScrollToEndAsync());

        var toEnd = Calls(ScrollToEdgeFn)[^1];
        Assert.AreEqual(true, toEnd.Arguments[1]);
        Assert.AreEqual(false, toEnd.Arguments[2]);
        Assert.AreEqual(1, component.FindAll("[data-bit-vir-index='99']").Count);

        await component.InvokeAsync(() => component.Instance.ScrollToStartAsync(smooth: true));

        var toStart = Calls(ScrollToEdgeFn)[^1];
        Assert.AreEqual(false, toStart.Arguments[1]);
        Assert.AreEqual(true, toStart.Arguments[2]);
    }

    [TestMethod]
    public void BitVirtualizeShouldScrollToTheInitialIndex()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.InitialIndex, 30));

        Assert.AreEqual(1500, LastScrollToOffset());
        Assert.AreEqual(1, component.FindAll("[data-bit-vir-index='30']").Count);
    }

    [TestMethod]
    public void BitVirtualizeShouldApplyTheInitialIndexOnceTheItemsArrive()
    {
        SetupViewport(300);

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Array.Empty<int>());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.InitialIndex, 30);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Assert.AreEqual(0, Calls(ScrollToOffsetFn).Count);

        component.Render(p => p.Add(x => x.Items, Enumerable.Range(0, 100).ToArray()));

        Assert.AreEqual(1500, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeScrollRequestedBeforeTheItemsArriveShouldBeAppliedAfterwards()
    {
        SetupViewport(300);

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Array.Empty<int>());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.InitialIndex, 10);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(50));
        Assert.AreEqual(0, Calls(ScrollToOffsetFn).Count);

        component.Render(p => p.Add(x => x.Items, Enumerable.Range(0, 100).ToArray()));

        // The explicit request wins over the InitialIndex, and is applied only once.
        Assert.AreEqual(1, Calls(ScrollToOffsetFn).Count);
        Assert.AreEqual(2500, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeScrollMethodsShouldBeSafeBeforeInitialization()
    {
        // No viewport reported (e.g. prerendering): nothing to scroll, nothing thrown.
        var component = RenderList(100, 50);

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(10));
        await component.InvokeAsync(() => component.Instance.ScrollToOffsetAsync(10));
        await component.InvokeAsync(() => component.Instance.ScrollByAsync(10));
        await component.InvokeAsync(() => component.Instance.ScrollToStartAsync());
        await component.InvokeAsync(() => component.Instance.ScrollToEndAsync());
        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        Assert.AreEqual(0, Calls(ScrollToOffsetFn).Count);
        Assert.AreEqual(0, Calls(ScrollToEdgeFn).Count);
    }



    // ---------------------------------------------------------------- huge (scaled) lists

    [TestMethod]
    public void BitVirtualizeShouldCapTheSpacerOfHugeLists()
    {
        SetupViewport(300);

        // 1M * 56px = 56M px, past the maximum size browsers can render for an element.
        var component = RenderList(1_000_000, 56);

        Assert.AreEqual("height:10000000px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeHugeListsShouldReachTheLastItemExactly()
    {
        SetupViewport(300);

        var component = RenderList(1_000_000, 56);

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(999_999));

        // The real end of the capped spacer is the virtual end of the list.
        Assert.AreEqual(10_000_000 - 300, LastScrollToOffset(), 0.01);

        var indices = RenderedIndices(component);
        Assert.AreEqual(999_999, indices[^1]);
        Assert.IsTrue(indices.Length <= 12, $"rendered {indices.Length} items");

        // The last item's bottom lands exactly on the bottom of the spacer.
        var block = TranslateOf(component.Find(".bit-vir-blk").GetAttribute("style"));
        var last = TranslateOf(component.Find("[data-bit-vir-index='999999']").GetAttribute("style"));
        Assert.AreEqual(10_000_000, block + last + 56, 0.01);
    }

    [TestMethod]
    public async Task BitVirtualizeHugeListsShouldMapTheScrollPositionProportionally()
    {
        SetupViewport(300);

        var component = RenderList(1_000_000, 56);

        // Half of the real scroll range is half of the virtual one.
        await component.InvokeAsync(() => component.Instance._Scroll((10_000_000 - 300) / 2d, 300));

        var expectedFirst = (int)((56_000_000 - 300) / 2d / 56);
        var indices = RenderedIndices(component);
        Assert.IsTrue(indices.Contains(expectedFirst));
        Assert.IsTrue(indices.Length <= 12, $"rendered {indices.Length} items");

        // The item at the virtual scroll offset is placed at the real one (the top of the viewport).
        var block = TranslateOf(component.Find(".bit-vir-blk").GetAttribute("style"));
        var item = TranslateOf(component.Find($"[data-bit-vir-index='{expectedFirst}']").GetAttribute("style"));
        var intoItem = (56_000_000 - 300) / 2d - expectedFirst * 56d;
        Assert.AreEqual((10_000_000 - 300) / 2d, block + item + intoItem, 0.02);

        // Fewer real pixels of movement are coalesced, since each moves the list further.
        Assert.IsTrue(Convert.ToDouble(Calls(UpdateFn)[^1].Arguments[3], CultureInfo.InvariantCulture) < 56);
    }



    // ---------------------------------------------------------------- edge callbacks

    [TestMethod]
    public void BitVirtualizeOnEndReachedShouldFireOncePerItemCount()
    {
        SetupViewport(300);

        var fired = 0;
        var list = Enumerable.Range(0, 5).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.OnEndReached, () => fired++);
        });

        // Five items fit in the viewport: the end is in view right away.
        Assert.AreEqual(1, fired);

        component.Render(p => p.Add(x => x.Items, list));
        Assert.AreEqual(1, fired);

        list.Add(5);
        component.Render(p => p.Add(x => x.Items, list));

        // Still at the end with a new count (e.g. the appended batch did not fill the viewport): fire again.
        Assert.AreEqual(2, fired);
    }

    [TestMethod]
    public async Task BitVirtualizeOnEndReachedShouldHonorTheThreshold()
    {
        SetupViewport(300);

        var fired = 0;
        var component = RenderList(100, 50, p =>
        {
            p.Add(x => x.ReachedThreshold, 10);
            p.Add(x => x.OnEndReached, () => fired++);
        });

        await component.InvokeAsync(() => component.Instance._Scroll(3900, 300)); // visible 78..84
        Assert.AreEqual(0, fired);

        await component.InvokeAsync(() => component.Instance._Scroll(4200, 300)); // visible 84..90
        Assert.AreEqual(1, fired);

        await component.InvokeAsync(() => component.Instance._Scroll(4700, 300));
        Assert.AreEqual(1, fired);
    }

    [TestMethod]
    public async Task BitVirtualizeOnStartReachedShouldNotFireForTheInitialPosition()
    {
        SetupViewport(300);

        var fired = 0;
        var list = Enumerable.Range(0, 100).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.OnStartReached, () => fired++);
        });

        Assert.AreEqual(0, fired);

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));
        await component.InvokeAsync(() => component.Instance._Scroll(0, 300));
        Assert.AreEqual(1, fired);

        await component.InvokeAsync(() => component.Instance._Scroll(10, 300));
        Assert.AreEqual(1, fired);

        // A prepended batch too small to leave the start must not stall the loading of more.
        list.InsertRange(0, [-2, -1]);
        component.Render(p => p.Add(x => x.Items, list));
        Assert.AreEqual(2, fired);
    }

    [TestMethod]
    public void BitVirtualizeCallbackFailuresShouldSurfaceThroughTheRenderer()
    {
        SetupViewport(300);

        // Fired from a synchronous recompute, the handler's failure is still dispatched to the renderer
        // (which surfaces it, e.g. to an ErrorBoundary) instead of being silently dropped.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderList(100, 50, p => p.Add(x => x.OnVisibleRangeChanged, (Action<(int, int)>)(_ => throw new InvalidOperationException("boom")))));

        Assert.AreEqual("boom", exception.Message);
    }



    // ---------------------------------------------------------------- data changes

    [TestMethod]
    public void BitVirtualizeShouldRecomputeWhenSameInstanceCountChanges()
    {
        var list = Enumerable.Range(0, 10).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Assert.AreEqual("height:500px", component.Find(".bit-vir-spc").GetAttribute("style"));

        // Mutate the same list instance (add 10 items) and re-render with the same reference.
        list.AddRange(Enumerable.Range(10, 10));
        component.Render(parameters => parameters.Add(p => p.Items, list));

        Assert.AreEqual("height:1000px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeRefreshDataAsyncShouldPickUpInPlaceMutations()
    {
        SetupViewport(300);

        var list = Enumerable.Range(0, 10).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        list[0] = 42; // same count: not detectable without a refresh
        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        Assert.AreEqual("Item 42", component.Find("[data-bit-vir-index='0']").TextContent);
    }

    [TestMethod]
    public void BitVirtualizeShouldRecomputeWhenSizingParametersChange()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        component.Render(p =>
        {
            p.Add(x => x.ItemSize, 100);
            p.Add(x => x.OverscanCount, 0);
        });

        Assert.AreEqual("height:10000px", component.Find(".bit-vir-spc").GetAttribute("style"));
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, RenderedIndices(component));
    }

    [TestMethod]
    public void BitVirtualizeShouldClearTheItemsWhenTheyAreRemoved()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.EmptyTemplate, b => b.AddContent(0, "Nothing")));

        component.Render(p => p.Add(x => x.Items, null));

        Assert.AreEqual(0, component.FindAll(".bit-vir-itm").Count);
        Assert.AreEqual("Nothing", component.Find(".bit-vir-emp").TextContent);
    }

    [TestMethod]
    public async Task BitVirtualizeShouldNotFailWhenTheItemsShrinkInPlaceBeforeARefresh()
    {
        SetupViewport(300);

        var list = Enumerable.Range(0, 100).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance._Scroll(4700, 300));

        // The list shrinks behind the component's back and a scroll renders before the parent does.
        list.RemoveRange(10, 90);
        await component.InvokeAsync(() => component.Instance._Scroll(4650, 300));

        Assert.IsTrue(RenderedIndices(component).All(i => i < 10));
    }

    [TestMethod]
    public void BitVirtualizeReversedShouldScrollToEndOnInitialRender()
    {
        SetupViewport(300);

        RenderList(100, 50, p => p.Add(x => x.Reversed, true));

        // Reversed (chat) mode anchors to the (real) bottom on first render, footer included.
        var call = Calls(ScrollToEdgeFn)[^1];
        Assert.AreEqual(true, call.Arguments[1]);
        Assert.AreEqual(0, Calls(ScrollToOffsetFn).Count);
    }

    [TestMethod]
    public void BitVirtualizeReversedShouldIgnoreTheInitialIndex()
    {
        SetupViewport(300);

        RenderList(100, 50, p =>
        {
            p.Add(x => x.Reversed, true);
            p.Add(x => x.InitialIndex, 10);
        });

        Assert.AreEqual(1, Calls(ScrollToEdgeFn).Count);
        Assert.AreEqual(0, Calls(ScrollToOffsetFn).Count);
    }

    [TestMethod]
    public void BitVirtualizeReversedShouldScrollToEndOnceItemsArriveLater()
    {
        SetupViewport(300);

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Array.Empty<int>());
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        Assert.AreEqual(0, Calls(ScrollToEdgeFn).Count);

        // Messages loaded asynchronously by the parent.
        component.Render(p => p.Add(x => x.Items, Enumerable.Range(0, 100).ToArray()));

        Assert.AreEqual(1, Calls(ScrollToEdgeFn).Count);
    }

    [TestMethod]
    public async Task BitVirtualizeReversedShouldFollowAppendsWhileAtTheEnd()
    {
        SetupViewport(300);

        var list = Enumerable.Range(0, 100).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance._Scroll(4700, 300));
        var edgeCalls = Calls(ScrollToEdgeFn).Count;

        list.Add(100);
        component.Render(p => p.Add(x => x.Items, list));

        Assert.AreEqual(edgeCalls + 1, Calls(ScrollToEdgeFn).Count);
        Assert.AreEqual(1, component.FindAll("[data-bit-vir-index='100']").Count);
    }

    [TestMethod]
    public async Task BitVirtualizeReversedShouldNotFollowAppendsWhileReadingHistory()
    {
        SetupViewport(300);

        var list = Enumerable.Range(0, 100).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.ItemKey, i => i);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));
        var edgeCalls = Calls(ScrollToEdgeFn).Count;
        var offsetCalls = Calls(ScrollToOffsetFn).Count;

        list.Add(100);
        component.Render(p => p.Add(x => x.Items, list));

        Assert.AreEqual(edgeCalls, Calls(ScrollToEdgeFn).Count);
        Assert.AreEqual(offsetCalls, Calls(ScrollToOffsetFn).Count);
    }

    [TestMethod]
    public async Task BitVirtualizeReversedShouldKeepThePinnedEndWhenTheViewportShrinks()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.Reversed, true));

        await component.InvokeAsync(() => component.Instance._Scroll(4700, 300));
        var edgeCalls = Calls(ScrollToEdgeFn).Count;

        // E.g. a soft keyboard opened: the same offset is now 100px short of the end.
        await component.InvokeAsync(() => component.Instance._Scroll(4700, 200));

        Assert.AreEqual(edgeCalls + 1, Calls(ScrollToEdgeFn).Count);
    }

    [TestMethod]
    public async Task BitVirtualizeReversedShouldKeepTheViewportAfterPrependingWithoutAnItemKey()
    {
        SetupViewport(300);

        var list = Enumerable.Range(0, 100).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));

        list.InsertRange(0, Enumerable.Range(-10, 10));
        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        // Same distance from the end: 10 prepended items of 50px further down.
        Assert.AreEqual(1500, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeItemKeyShouldKeepTheItemInViewAcrossInsertionsAbove()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.ItemKey, i => i));

        await component.InvokeAsync(() => component.Instance._Scroll(1010, 300));

        component.Render(p => p.Add(x => x.Items, Enumerable.Range(-10, 110).ToArray()));

        // Item 20 moved from index 20 to 30; the 10px scrolled into it are kept.
        Assert.AreEqual(1510, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeItemKeyShouldKeepTheItemInViewAcrossRemovalsAbove()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.ItemKey, i => i));

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));

        component.Render(p => p.Add(x => x.Items, Enumerable.Range(5, 95).ToArray()));

        Assert.AreEqual(750, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeItemKeyShouldFindTheItemInViewAfterAReorder()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.ItemKey, i => i));

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));

        component.Render(p => p.Add(x => x.Items, Enumerable.Range(0, 100).Reverse().ToArray()));

        // Item 20 is now at index 79.
        Assert.AreEqual(3950, LastScrollToOffset());
    }

    [TestMethod]
    public void BitVirtualizeItemKeyShouldShowInsertedItemsWhenAtTheTop()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.ItemKey, i => i));

        component.Render(p => p.Add(x => x.Items, Enumerable.Range(-10, 110).ToArray()));

        Assert.AreEqual(0, Calls(ScrollToOffsetFn).Count);
        Assert.AreEqual("Item -10", component.Find("[data-bit-vir-index='0']").TextContent);
    }

    [TestMethod]
    public void BitVirtualizeItemKeyShouldKeyTheRowsByIdentity()
    {
        SetupViewport(300);

        var component = RenderComponent<BitVirtualize<string>>(parameters =>
        {
            parameters.Add(p => p.Items, new[] { "a", "b", "c" });
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemKey, s => s);
            parameters.Add(p => p.ItemTemplate, s => b => b.AddContent(0, s));
        });

        component.Render(p => p.Add(x => x.Items, new[] { "z", "a", "b", "c" }));

        // Keyed by identity, every row renders its own item at its new index.
        CollectionAssert.AreEqual(new[] { "z", "a", "b", "c" }, component.FindAll(".bit-vir-itm").Select(e => e.TextContent).ToArray());
        Assert.AreEqual("2", component.FindAll(".bit-vir-itm").Single(e => e.TextContent == "b").GetAttribute("data-bit-vir-index"));
    }



    // ---------------------------------------------------------------- dynamic sizes

    [TestMethod]
    public void BitVirtualizeDynamicShouldSizeSpacerFromEstimatedItemSize()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 200).ToArray());
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.EstimatedItemSize, 40);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // In Dynamic mode the scrollable extent starts as EstimatedItemSize * count: 200 * 40 = 8000.
        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("height:8000px", spacer.GetAttribute("style"));
        Assert.IsFalse(component.Find(".bit-vir-itm").ClassList.Contains("bit-vir-fix"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicShouldUpdateTotalSizeWhenItemsMeasured()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.EstimatedItemSize, 40);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        // Report a measured size 100px larger than the estimate; the prefix-sum tree should
        // apply the delta to the total: (10 * 40) + 100 = 500.
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [140d]));

        var spacer = component.Find(".bit-vir-spc");
        Assert.AreEqual("height:500px", spacer.GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicShouldPositionItemsByTheirMeasuredSizes()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.Dynamic, true));

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0, 1], [80d, 20d]));

        Assert.AreEqual("transform:translateY(80px)", component.Find("[data-bit-vir-index='1']").GetAttribute("style"));
        Assert.AreEqual("transform:translateY(100px)", component.Find("[data-bit-vir-index='2']").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicShouldAnchorTheScrollWhenItemsAboveAreMeasured()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.Dynamic, true));

        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));

        // Item 18 is in the overscan above the first visible item (20) and turned out 100px taller.
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([18], [150d]));

        Assert.AreEqual(100d, Convert.ToDouble(Calls(AdjustScrollFn)[^1].Arguments[1], CultureInfo.InvariantCulture));

        // Items below the first visible one do not move what is in view.
        var adjustments = Calls(AdjustScrollFn).Count;
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([22], [150d]));
        Assert.AreEqual(adjustments, Calls(AdjustScrollFn).Count);
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicShouldIgnoreInvalidMeasurements()
    {
        var component = RenderList(10, 40, p =>
        {
            p.Add(x => x.Dynamic, true);
            p.Add(x => x.EstimatedItemSize, 40);
        });

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [double.NaN]));
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([1], [-10]));
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([50], [100]));
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([2, 3], [100]));

        Assert.AreEqual("height:400px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicMeasurementsShouldBeIgnoredInFixedMode()
    {
        var component = RenderList(10, 40);

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [140d]));

        Assert.AreEqual("height:400px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicShouldKeepMeasuredSizesAcrossAnAppend()
    {
        var list = Enumerable.Range(0, 10).ToList();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, list);
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.EstimatedItemSize, 40);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [140d]));

        list.AddRange(Enumerable.Range(10, 10));
        component.Render(p => p.Add(x => x.Items, list));

        Assert.AreEqual("height:900px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicMeasurementsShouldFollowTheirItemsByKey()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(0, 10).ToArray());
            parameters.Add(p => p.Dynamic, true);
            parameters.Add(p => p.EstimatedItemSize, 40);
            parameters.Add(p => p.ItemKey, i => i);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [140d]));

        // Item 0 moves to index 1 behind a new item: its size moves along, the new one is estimated.
        component.Render(p => p.Add(x => x.Items, new[] { -1 }.Concat(Enumerable.Range(0, 10)).ToArray()));

        Assert.AreEqual("height:540px", component.Find(".bit-vir-spc").GetAttribute("style"));
        Assert.AreEqual("transform:translateY(180px)", component.Find("[data-bit-vir-index='2']").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicScrollToIndexShouldRealignOnceTheTargetIsMeasured()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.Dynamic, true));

        await component.InvokeAsync(() => component.Instance.ScrollToIndexAsync(50));
        Assert.AreEqual(2500, LastScrollToOffset());

        // The items before the target turn out taller than estimated.
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([47, 48, 49, 50], [100d, 100d, 100d, 100d]));

        // The target now starts 150px further down: aligned to it again.
        Assert.AreEqual(2650, LastScrollToOffset());

        // Once the target has been measured, later measurements do not pull the viewport back.
        var calls = Calls(ScrollToOffsetFn).Count;
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([51], [100d]));
        Assert.AreEqual(calls, Calls(ScrollToOffsetFn).Count);
    }

    [TestMethod]
    public async Task BitVirtualizeDynamicKeyboardNavigationShouldKeepTheTargetInViewOnceMeasured()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.Dynamic, true));

        await component.InvokeAsync(() => component.Instance._KeyNavigate("End"));
        Assert.AreEqual(4700, LastScrollToOffset());

        // The last items turn out taller than estimated: the end moved, and the focused item is kept in view.
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([97, 98, 99], [100d, 100d, 100d]));

        Assert.AreEqual(4850, LastScrollToOffset());
    }

    [TestMethod]
    public async Task BitVirtualizeShouldTreatAnOffsetInsideTheHeaderAsTheStart()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.HeaderTemplate, b => b.AddContent(0, "H")));

        // The browser reports offsets relative to the first item: negative while the header is in view.
        await component.InvokeAsync(() => component.Instance._Scroll(-40, 300));

        Assert.AreEqual(0, RenderedIndices(component)[0]);
        Assert.AreEqual("transform:translateY(0px)", component.Find(".bit-vir-blk").GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldSwitchDynamicModeAtRuntime()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);

        component.Render(p =>
        {
            p.Add(x => x.Dynamic, true);
            p.Add(x => x.EstimatedItemSize, 30);
        });

        Assert.AreEqual("height:3000px", component.Find(".bit-vir-spc").GetAttribute("style"));

        component.Render(p => p.Add(x => x.Dynamic, false));

        Assert.AreEqual("height:5000px", component.Find(".bit-vir-spc").GetAttribute("style"));
        Assert.IsTrue(Calls(UpdateFn).Count >= 2);
    }

    [TestMethod]
    public async Task BitVirtualizeChangingTheOrientationShouldDropTheMeasuredSizes()
    {
        var component = RenderList(10, 40, p =>
        {
            p.Add(x => x.Dynamic, true);
            p.Add(x => x.EstimatedItemSize, 40);
        });

        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [140d]));
        Assert.AreEqual("height:500px", component.Find(".bit-vir-spc").GetAttribute("style"));

        component.Render(p => p.Add(x => x.Horizontal, true));

        Assert.AreEqual("width:400px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public void BitVirtualizeShouldUpdateTheBrowserSideWhenTheDirectionChanges()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.Horizontal, true));
        component.Render(p => p.Add(x => x.Class, "unrelated"));
        Assert.AreEqual(0, Calls(UpdateFn).Count);

        // An RTL horizontal list reads and writes its scroll position mirrored.
        component.Render(p => p.Add(x => x.Dir, BitDir.Rtl));

        Assert.AreEqual(1, Calls(UpdateFn).Count);
    }

    [TestMethod]
    public void BitVirtualizeShouldSyncTheBrowserSideAfterRendersWhenNeeded()
    {
        SetupViewport(300);

        var fixedList = RenderList(100, 50);
        fixedList.Render(p => p.Add(x => x.Class, "x"));
        Assert.AreEqual(0, Calls(SyncFn).Count);

        var dynamicList = RenderList(100, 50, p => p.Add(x => x.Dynamic, true));
        dynamicList.Render(p => p.Add(x => x.Class, "x"));
        Assert.IsTrue(Calls(SyncFn).Any(c => (string)c.Arguments[0]! == dynamicList.Instance.UniqueId));
    }



    // ---------------------------------------------------------------- sticky items

    [TestMethod]
    public void BitVirtualizeShouldRenderStickyHeaderForStickyItems()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.IsStickyItem, i => i % 10 == 0));

        // Index 0 is sticky and sits at the top of the viewport, so it gets pinned as the active header.
        var sticky = component.Find(".bit-vir-stk");
        Assert.AreEqual("Item 0", sticky.TextContent);
        Assert.AreEqual("true", sticky.GetAttribute("aria-hidden"));
        Assert.AreEqual("50", sticky.GetAttribute("data-bit-vir-sticky-size"));
        Assert.AreEqual("500", sticky.GetAttribute("data-bit-vir-sticky-next"));
    }

    [TestMethod]
    public async Task BitVirtualizeStickyHeaderShouldFollowTheCurrentGroup()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p =>
        {
            p.Add(x => x.IsStickyItem, i => i % 10 == 0);
            p.Add(x => x.StickyTemplate, item => b => b.AddContent(0, $"Group {item / 10}"));
        });

        await component.InvokeAsync(() => component.Instance._Scroll(1250, 300)); // first visible: 25

        var sticky = component.Find(".bit-vir-stk");
        Assert.AreEqual("Group 2", sticky.TextContent);
        Assert.AreEqual("1500", sticky.GetAttribute("data-bit-vir-sticky-next"));
    }

    [TestMethod]
    public void BitVirtualizeShouldApplyAStickyPredicateSetAtRuntime()
    {
        SetupViewport(300);

        var component = RenderList(100, 50);
        Assert.AreEqual(0, component.FindAll(".bit-vir-stk").Count);

        component.Render(p => p.Add(x => x.IsStickyItem, i => i % 10 == 0));
        Assert.AreEqual("Item 0", component.Find(".bit-vir-stk").TextContent);

        component.Render(p => p.Add(x => x.IsStickyItem, null));
        Assert.AreEqual(0, component.FindAll(".bit-vir-stk").Count);
    }

    [TestMethod]
    public void BitVirtualizeShouldNotRenderAStickyHeaderBeforeTheFirstStickyItem()
    {
        SetupViewport(300);

        var component = RenderList(100, 50, p => p.Add(x => x.IsStickyItem, i => i == 50));

        Assert.AreEqual(0, component.FindAll(".bit-vir-stk").Count);
    }



    // ---------------------------------------------------------------- items provider

    private sealed class ControlledProvider
    {
        public List<BitVirtualizeItemsProviderRequest> Requests { get; } = [];
        public List<TaskCompletionSource<BitVirtualizeItemsProviderResult<int>>> Pending { get; } = [];
        public int Total { get; set; } = 1_000;

        public ValueTask<BitVirtualizeItemsProviderResult<int>> Provide(BitVirtualizeItemsProviderRequest request)
        {
            Requests.Add(request);
            var tcs = new TaskCompletionSource<BitVirtualizeItemsProviderResult<int>>();
            Pending.Add(tcs);
            return new ValueTask<BitVirtualizeItemsProviderResult<int>>(tcs.Task);
        }

        public void Complete(int requestIndex)
        {
            var request = Requests[requestIndex];
            var items = Enumerable.Range(request.StartIndex, Math.Max(0, Math.Min(request.Count, Total - request.StartIndex))).ToList();
            Pending[requestIndex].TrySetResult(new BitVirtualizeItemsProviderResult<int>(items, Total));
        }
    }

    private IRenderedComponent<BitVirtualize<int>> RenderProvider(ControlledProvider provider, Action<ComponentParameterCollectionBuilder<BitVirtualize<int>>>? extra = null)
    {
        return RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, provider.Provide);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.PlaceholderTemplate, ctx => b => b.AddContent(0, $"Placeholder {ctx.Index} ({ctx.Size})"));
            extra?.Invoke(parameters);
        });
    }

    [TestMethod]
    public void BitVirtualizeShouldRenderItemsFromProvider()
    {
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, provider);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Markup.Contains("Item 0"));
            Assert.IsTrue(component.Markup.Contains("Item 1"));
            Assert.IsTrue(component.Markup.Contains("Item 2"));
        });

        static ValueTask<BitVirtualizeItemsProviderResult<int>> provider(BitVirtualizeItemsProviderRequest request)
        {
            var items = Enumerable.Range(request.StartIndex, request.Count).ToList();
            return ValueTask.FromResult(new BitVirtualizeItemsProviderResult<int>(items, 100));
        }
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldRequestTheRenderedWindow()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider);

        // The count is unknown at first: a screen of items plus the overscan on both sides.
        Assert.AreEqual(0, provider.Requests[0].StartIndex);
        Assert.AreEqual(13, provider.Requests[0].Count);

        await component.InvokeAsync(() => provider.Complete(0));
        Assert.AreEqual("height:50000px", component.Find(".bit-vir-spc").GetAttribute("style"));

        _ = component.InvokeAsync(() => component.Instance._Scroll(2000, 300));

        // Visible 40..45, rendered 37..48.
        Assert.AreEqual(37, provider.Requests[^1].StartIndex);
        Assert.AreEqual(12, provider.Requests[^1].Count);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldRenderPlaceholdersWhileLoading()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider);
        await component.InvokeAsync(() => provider.Complete(0));

        _ = component.InvokeAsync(() => component.Instance._Scroll(2000, 300));

        // Rendered right away, before the provider responds.
        var placeholder = component.Find("[data-bit-vir-index='40']");
        Assert.AreEqual("Placeholder 40 (50)", placeholder.TextContent);
        Assert.AreEqual("true", placeholder.GetAttribute("aria-busy"));
        Assert.AreEqual("true", component.Find(".bit-vir").GetAttribute("aria-busy"));

        await component.InvokeAsync(() => provider.Complete(1));

        var item = component.Find("[data-bit-vir-index='40']");
        Assert.AreEqual("Item 40", item.TextContent);
        Assert.IsFalse(item.HasAttribute("aria-busy"));
        Assert.IsFalse(component.Find(".bit-vir").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldSkipUnloadedItemsWithoutAPlaceholder()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, provider.Provide);
            parameters.Add(p => p.ItemSize, 50);
            parameters.Add(p => p.ItemTemplate, itemTemplate);
        });
        await component.InvokeAsync(() => provider.Complete(0));

        _ = component.InvokeAsync(() => component.Instance._Scroll(2000, 300));

        Assert.AreEqual(0, component.FindAll(".bit-vir-itm").Count);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldCancelSupersededRequests()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider);
        await component.InvokeAsync(() => provider.Complete(0));

        _ = component.InvokeAsync(() => component.Instance._Scroll(2000, 300));
        _ = component.InvokeAsync(() => component.Instance._Scroll(20000, 300));

        Assert.IsTrue(provider.Requests[1].CancellationToken.IsCancellationRequested);
        Assert.IsFalse(provider.Requests[2].CancellationToken.IsCancellationRequested);

        // A late response to the superseded request is ignored.
        await component.InvokeAsync(() => provider.Complete(1));
        Assert.AreEqual(0, component.FindAll("[data-bit-vir-index='40']").Count);

        await component.InvokeAsync(() => provider.Complete(2));
        Assert.AreEqual("Item 400", component.Find("[data-bit-vir-index='400']").TextContent);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldNotReloadCachedItems()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider);
        await component.InvokeAsync(() => provider.Complete(0));

        _ = component.InvokeAsync(() => component.Instance._Scroll(2000, 300));
        await component.InvokeAsync(() => provider.Complete(1));
        var requests = provider.Requests.Count;

        _ = component.InvokeAsync(() => component.Instance._Scroll(0, 300));

        Assert.AreEqual(requests, provider.Requests.Count);
        Assert.AreEqual("Item 0", component.Find("[data-bit-vir-index='0']").TextContent);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderRefreshDataAsyncShouldRequestAgain()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider);
        await component.InvokeAsync(() => provider.Complete(0));

        provider.Total = 3;
        _ = component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        Assert.AreEqual(2, provider.Requests.Count);

        await component.InvokeAsync(() => provider.Complete(1));

        Assert.AreEqual("height:150px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldShowTheLoadingTemplateUntilTheFirstLoad()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider, p => p.Add(x => x.LoadingTemplate, b => b.AddContent(0, "Loading...")));

        Assert.AreEqual("Loading...", component.Find(".bit-vir-ldg").TextContent);

        await component.InvokeAsync(() => provider.Complete(0));

        Assert.AreEqual(0, component.FindAll(".bit-vir-ldg").Count);
        Assert.AreEqual("Item 0", component.Find("[data-bit-vir-index='0']").TextContent);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldShowTheEmptyTemplateForAnEmptySource()
    {
        SetupViewport(300);

        var provider = new ControlledProvider { Total = 0 };
        var component = RenderProvider(provider, p => p.Add(x => x.EmptyTemplate, b => b.AddContent(0, "Empty")));

        await component.InvokeAsync(() => provider.Complete(0));

        Assert.AreEqual("Empty", component.Find(".bit-vir-emp").TextContent);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderShouldTolerateMalformedResults()
    {
        SetupViewport(300);

        var component = RenderComponent<BitVirtualize<int>>(parameters =>
        {
            parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromResult(new BitVirtualizeItemsProviderResult<int>(null!, -5)));
            parameters.Add(p => p.ItemTemplate, itemTemplate);
            parameters.Add(p => p.EmptyTemplate, b => b.AddContent(0, "Empty"));
        });

        await component.InvokeAsync(() => component.Instance.RefreshDataAsync());

        Assert.AreEqual("Empty", component.Find(".bit-vir-emp").TextContent);
    }

    [TestMethod]
    public void BitVirtualizeProviderFailuresShouldSurfaceThroughTheRenderer()
    {
        SetupViewport(300);

        // Dispatched to the renderer (and so to an ErrorBoundary) rather than lost in a browser-initiated call.
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderComponent<BitVirtualize<int>>(parameters =>
            {
                parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromException<BitVirtualizeItemsProviderResult<int>>(new InvalidOperationException("provider failed")));
                parameters.Add(p => p.ItemTemplate, itemTemplate);
            }));

        Assert.AreEqual("provider failed", exception.Message);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderPlaceholderKeysShouldNotCollideWithItemKeys()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        // The keys of the loaded items (i + 20) overlap the indices of the placeholders around them.
        var component = RenderProvider(provider, p => p.Add(x => x.ItemKey, i => i + 20));
        await component.InvokeAsync(() => provider.Complete(0));

        _ = component.InvokeAsync(() => component.Instance._Scroll(500, 300));

        Assert.AreEqual("Item 12", component.Find("[data-bit-vir-index='12']").TextContent);
        Assert.AreEqual("Placeholder 13 (50)", component.Find("[data-bit-vir-index='13']").TextContent);
    }

    [TestMethod]
    public async Task BitVirtualizeProviderStickyItemsShouldApplyToTheLoadedWindow()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider, p => p.Add(x => x.IsStickyItem, i => i % 10 == 0));
        await component.InvokeAsync(() => provider.Complete(0));

        Assert.AreEqual("Item 0", component.Find(".bit-vir-stk").TextContent);
    }

    [TestMethod]
    public async Task BitVirtualizeShouldSwitchFromItemsToAProvider()
    {
        SetupViewport(300);

        var component = RenderList(10, 50);

        var provider = new ControlledProvider();
        component.Render(p =>
        {
            p.Add(x => x.Items, null);
            p.Add(x => x.ItemsProvider, provider.Provide);
        });

        Assert.AreEqual(1, provider.Requests.Count);

        await component.InvokeAsync(() => provider.Complete(0));

        Assert.AreEqual("height:50000px", component.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeShouldSwitchFromAProviderToItems()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider);
        await component.InvokeAsync(() => provider.Complete(0));

        component.Render(p =>
        {
            p.Add(x => x.ItemsProvider, null);
            p.Add(x => x.Items, new[] { 7, 8, 9 });
        });

        Assert.AreEqual("height:150px", component.Find(".bit-vir-spc").GetAttribute("style"));
        Assert.AreEqual("Item 7", component.Find("[data-bit-vir-index='0']").TextContent);
    }

    [TestMethod]
    public void BitVirtualizeShouldThrowWhenBothItemsAndItemsProviderAreSet()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            RenderComponent<BitVirtualize<int>>(parameters =>
            {
                parameters.Add(p => p.Items, new List<int> { 1, 2, 3 });
                parameters.Add(p => p.ItemsProvider, _ => ValueTask.FromResult(new BitVirtualizeItemsProviderResult<int>([], 0)));
                parameters.Add(p => p.ItemTemplate, itemTemplate);
            });
        });
    }



    // ---------------------------------------------------------------- robustness

    [TestMethod]
    public void BitVirtualizeShouldTolerateInvalidSizes()
    {
        SetupViewport(300);

        var component = RenderList(100, 0, p =>
        {
            p.Add(x => x.OverscanCount, -5);
            p.Add(x => x.ReachedThreshold, -5);
        });

        // A zero item size is treated as 1px; a negative overscan as none.
        Assert.AreEqual("height:100px", component.Find(".bit-vir-spc").GetAttribute("style"));
        Assert.AreEqual(100, RenderedIndices(component).Length);

        var dynamicList = RenderList(100, 50, p =>
        {
            p.Add(x => x.Dynamic, true);
            p.Add(x => x.EstimatedItemSize, -10);
        });

        Assert.AreEqual("height:100px", dynamicList.Find(".bit-vir-spc").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitVirtualizeShouldDisposeJsInterop()
    {
        var component = RenderList(10, 50);

        Context.JSInterop.SetupVoid(DisposeFn, component.Instance.UniqueId).SetVoidResult();

        await component.Instance.DisposeAsync();

        var invocation = Context.JSInterop.VerifyInvoke(DisposeFn);
        Assert.AreEqual(component.Instance.UniqueId, invocation.Arguments[0]);
    }

    [TestMethod]
    public async Task BitVirtualizeDisposeShouldCancelAPendingProviderRequest()
    {
        SetupViewport(300);

        var provider = new ControlledProvider();
        var component = RenderProvider(provider);

        await component.Instance.DisposeAsync();

        Assert.IsTrue(provider.Requests[0].CancellationToken.IsCancellationRequested);
    }

    [TestMethod]
    public async Task BitVirtualizeScrollShouldBeNoOpAfterDispose()
    {
        var component = RenderList(10, 50);

        Context.JSInterop.SetupVoid(DisposeFn, component.Instance.UniqueId).SetVoidResult();
        await component.Instance.DisposeAsync();

        // Messages that arrive after disposal must be ignored without throwing.
        await component.InvokeAsync(() => component.Instance._Scroll(1000, 300));
        await component.InvokeAsync(() => component.Instance._ItemsMeasured([0], [10]));
        await component.InvokeAsync(() => component.Instance._KeyNavigate("End"));
    }



    // ---------------------------------------------------------------- the prefix-sum tree

    [TestMethod]
    public void BitVirtualizePrefixSumTreeShouldMatchANaiveModel()
    {
        var random = new Random(7);
        var tree = new TreeProxy(200, 10);
        var sizes = Enumerable.Repeat(10d, 200).ToList();

        for (var round = 0; round < 500; round++)
        {
            var index = random.Next(sizes.Count);
            var size = random.Next(1, 100);
            var delta = tree.SetSize(index, size);
            Assert.AreEqual(size - sizes[index], delta);
            sizes[index] = size;

            if (round % 50 == 0)
            {
                // Grow or shrink, keeping the surviving sizes.
                var newCount = random.Next(1, 300);
                tree.Resize(newCount, 10);
                sizes = [.. sizes.Take(newCount), .. Enumerable.Repeat(10d, Math.Max(0, newCount - sizes.Count))];
            }

            AssertMatches(tree, sizes);
        }

        tree.Reset(5, 3);
        AssertMatches(tree, [3, 3, 3, 3, 3]);

        static void AssertMatches(TreeProxy tree, List<double> sizes)
        {
            Assert.AreEqual(sizes.Count, tree.Count);
            Assert.AreEqual(sizes.Sum(), tree.Total, 1e-6);

            var prefix = 0d;
            for (var i = 0; i < sizes.Count; i++)
            {
                Assert.AreEqual(sizes[i], tree.GetSize(i));
                Assert.AreEqual(prefix, tree.PrefixSum(i), 1e-6);

                // Any offset inside an item resolves to that item.
                Assert.AreEqual(i, tree.FindIndex(prefix));
                Assert.AreEqual(i, tree.FindIndex(prefix + sizes[i] / 2));
                prefix += sizes[i];
            }

            Assert.AreEqual(prefix, tree.PrefixSum(sizes.Count + 5), 1e-6);
            Assert.AreEqual(0, tree.FindIndex(-10));
            Assert.AreEqual(sizes.Count - 1, tree.FindIndex(prefix + 1000));
        }
    }

    // The tree is internal to the (strong-named) Extras assembly, so it is driven through reflection.
    private sealed class TreeProxy
    {
        private static readonly Type _type = typeof(BitVirtualize<int>).Assembly.GetType("Bit.BlazorUI.BitVirtualizePrefixSumTree", throwOnError: true)!;
        private readonly object _tree;

        public TreeProxy(int count, double defaultValue) => _tree = Activator.CreateInstance(_type, count, defaultValue)!;

        public int Count => (int)_type.GetProperty("Count")!.GetValue(_tree)!;
        public double Total => (double)_type.GetProperty("Total")!.GetValue(_tree)!;
        public double SetSize(int index, double value) => (double)Call("SetSize", index, value)!;
        public double GetSize(int index) => (double)Call("GetSize", index)!;
        public double PrefixSum(int index) => (double)Call("PrefixSum", index)!;
        public int FindIndex(double offset) => (int)Call("FindIndex", offset)!;
        public void Resize(int count, double defaultValue) => Call("Resize", count, defaultValue);
        public void Reset(int count, double defaultValue) => Call("Reset", count, defaultValue);

        private object? Call(string name, params object[] args) => _type.GetMethod(name)!.Invoke(_tree, args);
    }
}
