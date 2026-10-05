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
    public void BitDataGridLegacyVirtualizedShouldHandTheBodyOverToVirtualizeOnceRendered()
    {
        var component = RenderComponent<BitDataGridLegacy<int>>(parameters =>
        {
            parameters.Add(p => p.Items, Enumerable.Range(1, 1000).AsQueryable());
            parameters.Add(p => p.Virtualize, true);
            parameters.Add(p => p.ChildContent, Columns());
        });

        // Once interactive, the rows rendered in place are gone, so none is left over beside the ones Virtualize
        // requests after measuring the viewport (which bUnit's JS never does).
        Assert.IsFalse(component.Markup.Contains("row-"));
        Assert.HasCount(2, component.FindAll("tbody tr"));
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
