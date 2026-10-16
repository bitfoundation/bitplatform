using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Legacy.Tests.DataGrid;

[TestClass]
public class BitDataGridLegacyPrerenderTests : BunitTestContext
{
    // ceil(600 / 50) rows for an assumed viewport, plus the overscan Virtualize adds on both sides and one more row.
    private const int ExpectedWindow = 19;

    [TestMethod]
    public async Task BitDataGridLegacyVirtualizedShouldPrerenderTheFirstRowsOfItems()
    {
        var html = await Prerenderer.RenderAsync<BitDataGridLegacy<int>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGridLegacy<int>.Items)] = Enumerable.Range(1, 1000).AsQueryable(),
            [nameof(BitDataGridLegacy<int>.Virtualize)] = true,
            [nameof(BitDataGridLegacy<int>.ItemSize)] = 50f,
            [nameof(BitDataGridLegacy<int>.ChildContent)] = Columns(),
        });

        StringAssert.Contains(html, "row-1;");
        StringAssert.Contains(html, $"row-{ExpectedWindow};");
        Assert.IsFalse(html.Contains($"row-{ExpectedWindow + 1};"));

        // The rows not rendered yet are reserved by a spacer, so the scrollbar is already the right length.
        StringAssert.Contains(html, $"height: {(1000 - ExpectedWindow) * 50}px");
        StringAssert.Contains(html, "aria-rowcount=\"1001\"");
    }

    [TestMethod]
    public async Task BitDataGridLegacyVirtualizedShouldPrerenderAllOfFewItemsWithoutASpacer()
    {
        var html = await Prerenderer.RenderAsync<BitDataGridLegacy<int>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGridLegacy<int>.Items)] = Enumerable.Range(1, 5).AsQueryable(),
            [nameof(BitDataGridLegacy<int>.Virtualize)] = true,
            [nameof(BitDataGridLegacy<int>.ChildContent)] = Columns(),
        });

        Assert.AreEqual(5, Regex.Matches(html, "row-\\d+;").Count);
        Assert.IsFalse(html.Contains("height:"));
    }

    [TestMethod]
    public async Task BitDataGridLegacyVirtualizedShouldPrerenderPlaceholdersWithoutCallingItemsProvider()
    {
        var providerCalls = 0;
        BitDataGridItemsProvider<int> provider = request =>
        {
            providerCalls++;
            return ValueTask.FromResult(BitDataGridLegacyItemsProviderResult.From<int>([1, 2, 3], 3));
        };

        var html = await Prerenderer.RenderAsync<BitDataGridLegacy<int>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGridLegacy<int>.ItemsProvider)] = provider,
            [nameof(BitDataGridLegacy<int>.Virtualize)] = true,
            [nameof(BitDataGridLegacy<int>.ItemSize)] = 50f,
            [nameof(BitDataGridLegacy<int>.ChildContent)] = Columns(),
        });

        Assert.AreEqual(0, providerCalls);
        Assert.AreEqual(ExpectedWindow, Regex.Matches(html, "bit-qkg-plh").Count);
        Assert.IsFalse(html.Contains("row-"));
    }

    [TestMethod]
    public void BitDataGridLegacyVirtualizedShouldKeepTheFirstRowsBesideVirtualizeUntilItHasRows()
    {
        var component = RenderComponent<BitDataGridLegacy<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(1, 1000).AsQueryable());
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.ChildContent, Columns());
        });

        // Once interactive, Virtualize is mounted (its two spacers), but it renders no row until its JS has measured
        // the viewport (which bUnit's JS never does), so the rows rendered in place stay rather than blanking the body.
        StringAssert.Contains(component.Markup, "row-1;");
        StringAssert.Contains(component.Markup, $"row-{ExpectedWindow};");
        Assert.HasCount(2 + ExpectedWindow + 1, component.FindAll("tbody tr"));
    }

    [TestMethod]
    public async Task BitDataGridLegacyShouldPrerenderEveryRowOfANonVirtualizedGrid()
    {
        var html = await Prerenderer.RenderAsync<BitDataGridLegacy<int>>(new Dictionary<string, object?>
        {
            [nameof(BitDataGridLegacy<int>.Items)] = Enumerable.Range(1, 5).AsQueryable(),
            [nameof(BitDataGridLegacy<int>.Class)] = "custom-grid",
            [nameof(BitDataGridLegacy<int>.ChildContent)] = Columns(),
        });

        // The whole page of rows is in the markup, under its header, with nothing left to a script.
        Assert.AreEqual(5, Regex.Matches(html, "row-\\d+;").Count);
        StringAssert.Contains(html, "bit-qkg custom-grid");
        StringAssert.Contains(html, "scope=\"col\"");
        StringAssert.Contains(html, "bit-qkg-hct");
        StringAssert.Contains(html, "aria-rowcount=\"6\"");
        StringAssert.Contains(html, "aria-rowindex=\"2\"");
    }

    [TestMethod]
    public void BitDataGridLegacyShouldRenderTheRowsOfANonVirtualizedGridWithTheirClassesAndStyles()
    {
        var component = RenderComponent<BitDataGridLegacy<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(1, 3).AsQueryable());
            parameters.Add(p => p.RowClass, "row-class");
            parameters.Add(p => p.RowClassSelector, i => i == 2 ? "second" : "");
            parameters.Add(p => p.RowStyle, "color:red");
            parameters.Add(p => p.RowStyleSelector, i => i == 2 ? "font-weight:bold" : "");
            parameters.Add(p => p.ChildContent, Columns());
        });

        var rows = component.FindAll("tbody tr");

        Assert.HasCount(3, rows);
        Assert.AreEqual("row-1;", rows[0].TextContent.Trim());
        Assert.IsTrue(rows[0].ClassList.Contains("row-class"));
        Assert.IsFalse(rows[0].ClassList.Contains("second"));
        Assert.IsTrue(rows[1].ClassList.Contains("row-class"));
        Assert.IsTrue(rows[1].ClassList.Contains("second"));
        StringAssert.Contains(rows[0].GetAttribute("style"), "color:red");
        StringAssert.Contains(rows[1].GetAttribute("style"), "font-weight:bold");
        Assert.AreEqual("Value", component.Find("td").GetAttribute("data-title"));
    }

    [TestMethod]
    public void BitDataGridLegacyShouldFillTheLastPageWithEmptyRows()
    {
        var component = RenderComponent<BitDataGridLegacy<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(1, 3).AsQueryable());
            parameters.Add(p => p.Pagination, new BitDataGridLegacyPaginationState { ItemsPerPage = 5 });
            parameters.Add(p => p.ChildContent, Columns());
        });

        // The page always holds its full count of rows, so the layout does not jump on the last page.
        Assert.HasCount(3, component.FindAll("tbody tr[aria-rowindex]"));
        Assert.HasCount(2, component.FindAll("tbody tr.bit-qkg-empty-row"));
    }

    [TestMethod]
    public void BitDataGridLegacyShouldRefuseBothItemsAndAnItemsProvider()
    {
        BitDataGridItemsProvider<int> provider = _ => ValueTask.FromResult(BitDataGridLegacyItemsProviderResult.From<int>([1], 1));

        Assert.ThrowsExactly<InvalidOperationException>(() => RenderComponent<BitDataGridLegacy<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(1, 3).AsQueryable());
            parameters.Add(p => p.ItemsProvider, provider);
            parameters.Add(p => p.ChildContent, Columns());
        }));
    }

    [TestMethod]
    public async Task BitDataGridLegacyShouldDisposeWithoutError()
    {
        var component = RenderComponent<BitDataGridLegacy<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(1, 3).AsQueryable());
            parameters.Add(p => p.ResizableColumns, true);
            parameters.Add(p => p.ChildContent, Columns());
        });

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.DataGrid.init");

        await component.Instance.DisposeAsync();
        // Disposing again is a no-op rather than a second teardown.
        await component.Instance.DisposeAsync();
    }

    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<BitDataGridLegacyTemplateColumn<int>>(0);
        builder.AddAttribute(1, nameof(BitDataGridLegacyTemplateColumn<int>.Title), "Value");
        builder.AddAttribute(2, nameof(BitDataGridLegacyTemplateColumn<int>.ChildContent),
            (RenderFragment<int>)(i => b => b.AddContent(0, $"row-{i};")));
        builder.CloseComponent();
    };
}
