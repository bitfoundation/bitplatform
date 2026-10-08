using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.BasicList;

[TestClass]
public class BitBasicListPrerenderTests : BunitTestContext
{
    private static readonly RenderFragment<int> RowTemplate = i => b => b.AddContent(0, $"row-{i};");

    [TestMethod]
    public async Task BitBasicListShouldPrerenderTheFirstWindowOfVirtualizedItems()
    {
        var html = await Prerenderer.RenderAsync<BitBasicList<int>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<int>.Items)] = Enumerable.Range(0, 1000).ToArray(),
            [nameof(BitBasicList<int>.Virtualize)] = true,
            [nameof(BitBasicList<int>.RowTemplate)] = RowTemplate,
        });

        // ceil(600 / 50) + 3 * 2 + 1 = 19 rows, the rest reserved by a spacer of (1000 - 19) * 50px.
        StringAssert.Contains(html, "row-0;");
        StringAssert.Contains(html, "row-18;");
        Assert.IsFalse(html.Contains("row-19;"));
        StringAssert.Contains(html, "height: 49050px;");
        StringAssert.Contains(html, "bit-bsl-vwn");
    }

    [TestMethod]
    public async Task BitBasicListShouldPrerenderEveryVirtualizedItemThatFitsTheFirstWindow()
    {
        var html = await Prerenderer.RenderAsync<BitBasicList<int>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<int>.Items)] = Enumerable.Range(0, 5).ToArray(),
            [nameof(BitBasicList<int>.Virtualize)] = true,
            [nameof(BitBasicList<int>.RowTemplate)] = RowTemplate,
        });

        for (var i = 0; i < 5; i++)
        {
            Assert.AreEqual(1, Regex.Matches(html, $"row-{i};").Count);
        }
    }

    [TestMethod]
    public async Task BitBasicListShouldPrerenderAWindowSizedByItemSizeAndOverscanCount()
    {
        var html = await Prerenderer.RenderAsync<BitBasicList<int>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<int>.Items)] = Enumerable.Range(0, 100).ToArray(),
            [nameof(BitBasicList<int>.Virtualize)] = true,
            [nameof(BitBasicList<int>.ItemSize)] = 100f,
            [nameof(BitBasicList<int>.OverscanCount)] = 1,
            [nameof(BitBasicList<int>.RowTemplate)] = RowTemplate,
        });

        // ceil(600 / 100) + 1 * 2 + 1 = 9 rows.
        StringAssert.Contains(html, "row-8;");
        Assert.IsFalse(html.Contains("row-9;"));
        StringAssert.Contains(html, "height: 9100px;");
    }

    [TestMethod]
    public async Task BitBasicListShouldPrerenderVirtualizedDefaultRowsAsListItems()
    {
        var html = await Prerenderer.RenderAsync<BitBasicList<string>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<string>.Items)] = new[] { "one", "two" },
            [nameof(BitBasicList<string>.Virtualize)] = true,
        });

        StringAssert.Contains(html, "role=\"list\"");
        StringAssert.Contains(html, "<div role=\"listitem\">one</div>");
        StringAssert.Contains(html, "<div role=\"listitem\">two</div>");
    }

    [TestMethod]
    public async Task BitBasicListShouldPrerenderTheFirstWindowOfAVirtualizedLoadMorePage()
    {
        var html = await Prerenderer.RenderAsync<BitBasicList<int>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<int>.Items)] = Enumerable.Range(0, 100).ToArray(),
            [nameof(BitBasicList<int>.Virtualize)] = true,
            [nameof(BitBasicList<int>.LoadMore)] = true,
            [nameof(BitBasicList<int>.RowTemplate)] = RowTemplate,
        });

        // The first page holds 20 items, 19 of which make the window and the last one its spacer.
        StringAssert.Contains(html, "row-18;");
        Assert.IsFalse(html.Contains("row-19;"));
        StringAssert.Contains(html, "height: 50px;");
        StringAssert.Contains(html, "bit-bsl-lmb");
    }

    [TestMethod]
    public async Task BitBasicListShouldPrerenderVirtualizePlaceholdersWithoutCallingTheProvider()
    {
        var called = false;

        var html = await Prerenderer.RenderAsync<BitBasicList<int>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<int>.Virtualize)] = true,
            [nameof(BitBasicList<int>.ItemsProvider)] = (BitBasicListItemsProvider<int>)(request =>
            {
                called = true;
                return ValueTask.FromResult(BitBasicListItemsProviderResult.From<int>([1], 1));
            }),
            [nameof(BitBasicList<int>.VirtualizePlaceholder)] = (RenderFragment<PlaceholderContext>)(c => b => b.AddContent(0, $"ph-{c.Index};")),
            [nameof(BitBasicList<int>.RowTemplate)] = RowTemplate,
        });

        Assert.IsFalse(called);
        StringAssert.Contains(html, "ph-0;");
        StringAssert.Contains(html, "ph-18;");
        Assert.IsFalse(html.Contains("ph-19;"));
        StringAssert.Contains(html, "bit-bsl-vwp");
    }

    [TestMethod]
    public async Task BitBasicListShouldPrerenderTheRoomOfTheProviderItemsWithoutAPlaceholder()
    {
        var html = await Prerenderer.RenderAsync<BitBasicList<int>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<int>.Virtualize)] = true,
            [nameof(BitBasicList<int>.ItemsProvider)] = (BitBasicListItemsProvider<int>)(request => throw new InvalidOperationException()),
            [nameof(BitBasicList<int>.RowTemplate)] = RowTemplate,
        });

        StringAssert.Contains(html, "height: 950px;");
    }

    [TestMethod]
    public async Task BitBasicListShouldPrerenderTheEmptyContentOfAnEmptyVirtualizedList()
    {
        var html = await Prerenderer.RenderAsync<BitBasicList<int>>(new Dictionary<string, object?>
        {
            [nameof(BitBasicList<int>.Items)] = Array.Empty<int>(),
            [nameof(BitBasicList<int>.Virtualize)] = true,
            [nameof(BitBasicList<int>.EmptyContent)] = (RenderFragment)(b => b.AddMarkupContent(0, "<i class=\"empty\">empty</i>")),
        });

        StringAssert.Contains(html, "class=\"empty\"");
        Assert.IsFalse(html.Contains("bit-bsl-vwn"));
        Assert.IsFalse(html.Contains("role=\"list\""));
    }

    [TestMethod]
    public void BitBasicListShouldHandTheFirstWindowOverToVirtualize()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitBasicList<int>>(p =>
        {
            p.Add(x => x.Items, Enumerable.Range(0, 1000).ToArray());
            p.Add(x => x.Virtualize, true);
            p.Add(x => x.RowTemplate, i => b => b.AddMarkupContent(0, $"<div class=\"row\">{i}</div>"));
        });

        // Virtualize has measured the list (bUnit emulates that), so its rows are the only ones left: each item
        // is rendered once, and the window, its spacer and the class laying Virtualize's spacer over it are gone.
        var rows = component.FindAll(".row").Select(r => r.TextContent).ToArray();

        Assert.IsTrue(rows.Length > 0);
        Assert.AreEqual(rows.Length, rows.Distinct().Count());
        Assert.AreEqual("0", rows[0]);
        Assert.IsFalse(component.Find(".bit-bsl-itm").ClassList.Contains("bit-bsl-vwn"));
        Assert.IsFalse(component.Markup.Contains("height: 49050px;"));
    }

    [TestMethod]
    public void BitBasicListShouldHandTheProviderPlaceholdersOverToVirtualize()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitBasicList<int>>(p =>
        {
            p.Add(x => x.Virtualize, true);
            p.Add(x => x.ItemsProviderDelay, 0);
            p.Add(x => x.ItemsProvider, request =>
            {
                var page = Enumerable.Range(request.StartIndex, Math.Min(request.Count, 50 - request.StartIndex)).ToArray();
                return ValueTask.FromResult(BitBasicListItemsProviderResult.From<int>(page, 50));
            });
            p.Add(x => x.VirtualizePlaceholder, c => b => b.AddMarkupContent(0, $"<div class=\"ph\">{c.Index}</div>"));
            p.Add(x => x.RowTemplate, i => b => b.AddMarkupContent(0, $"<div class=\"row\">{i}</div>"));
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.FindAll(".row").Count > 0));

        Assert.AreEqual(0, component.FindAll(".ph").Count);
        Assert.IsFalse(component.Find(".bit-bsl-itm").ClassList.Contains("bit-bsl-vwn"));
    }

    [TestMethod]
    public void BitBasicListShouldDropTheProviderPlaceholdersWhenTheProviderHasNothing()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitBasicList<int>>(p =>
        {
            p.Add(x => x.Virtualize, true);
            p.Add(x => x.ItemsProviderDelay, 0);
            p.Add(x => x.ItemsProvider, request => ValueTask.FromResult(BitBasicListItemsProviderResult.From<int>([], 0)));
            p.Add(x => x.VirtualizePlaceholder, c => b => b.AddMarkupContent(0, $"<div class=\"ph\">{c.Index}</div>"));
            p.Add(x => x.EmptyContent, b => b.AddMarkupContent(0, "<div class=\"empty\">empty</div>"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".empty").Count));

        Assert.AreEqual(0, component.FindAll(".ph").Count);
    }

    [TestMethod]
    public void BitBasicListShouldStandInForANewVirtualizeAfterTheLoadingContent()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitBasicList<int>>(p =>
        {
            p.Add(x => x.Items, Enumerable.Range(0, 1000).ToArray());
            p.Add(x => x.Virtualize, true);
            p.Add(x => x.RowTemplate, i => b => b.AddMarkupContent(0, $"<div class=\"row\">{i}</div>"));
        });

        component.Render(p => p.Add(x => x.Loading, true));
        component.Render(p => p.Add(x => x.Loading, false));

        var rows = component.FindAll(".row").Select(r => r.TextContent).ToArray();

        Assert.IsTrue(rows.Length > 0);
        Assert.AreEqual(rows.Length, rows.Distinct().Count());
        Assert.IsFalse(component.Find(".bit-bsl-itm").ClassList.Contains("bit-bsl-vwn"));
    }
}
