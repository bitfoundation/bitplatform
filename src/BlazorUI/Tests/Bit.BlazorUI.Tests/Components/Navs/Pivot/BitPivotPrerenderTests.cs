using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Pivot;

/// <summary>
/// What a pivot sends before the page is interactive: the items register as they render, and the after-render pass,
/// where the pivot sets its script up and asks the browser about the header, never runs on a prerender (or on a static
/// SSR page at all), so the selection, the tabs pattern and the panel have to come out of the render itself, with a
/// JS runtime that throws on every call.
/// </summary>
[TestClass]
public class BitPivotPrerenderTests
{
    [TestMethod]
    public async Task BitPivotShouldRenderTheSelectedTabAndItsPanelBeforeInteractivity()
    {
        var document = await Prerender(new(), "a", "b", "c");

        var tabs = document.QuerySelectorAll("[role=tab]");

        Assert.AreEqual(3, tabs.Length);
        Assert.AreEqual("true", tabs[0].GetAttribute("aria-selected"));
        Assert.AreEqual("false", tabs[1].GetAttribute("aria-selected"));
        Assert.AreEqual("0", tabs[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", tabs[1].GetAttribute("tabindex"));
        Assert.AreEqual("tablist", document.QuerySelector(".bit-pvt-hct")!.GetAttribute("role"));

        var panel = document.QuerySelector("[role=tabpanel]");

        Assert.IsNotNull(panel);
        Assert.AreEqual(tabs[0].Id, panel.GetAttribute("aria-labelledby"));
        Assert.AreEqual(panel.Id, tabs[0].GetAttribute("aria-controls"));
        Assert.AreEqual("a", panel.TextContent.Trim());
    }

    [TestMethod]
    public async Task BitPivotShouldSelectTheTabOfTheDefaultSelectedKeyBeforeInteractivity()
    {
        var document = await Prerender(new() { [nameof(BitPivot.DefaultSelectedKey)] = "b" }, "a", "b", "c");

        var tabs = document.QuerySelectorAll("[role=tab]");

        Assert.AreEqual("false", tabs[0].GetAttribute("aria-selected"));
        Assert.AreEqual("true", tabs[1].GetAttribute("aria-selected"));
        Assert.AreEqual("b", document.QuerySelector("[role=tabpanel]")!.TextContent.Trim());
    }

    [TestMethod]
    public async Task BitPivotShouldRenderTheAffordancesOfItsOverflowBehaviorsBeforeInteractivity()
    {
        // Nothing is folded until the script measures the header, so the menu button is there (hidden by its own
        // style until the script shows it) and the menu is empty.
        var menu = await Prerender(new() { [nameof(BitPivot.OverflowBehavior)] = BitPivotOverflowBehavior.Menu }, "a", "b");

        Assert.IsNotNull(menu.QuerySelector(".bit-pvt-mor"));
        Assert.AreEqual(0, menu.QuerySelectorAll(".bit-pvt-mni").Length);

        // Nothing has been measured to slide to either, so both slide buttons are out.
        var slide = await Prerender(new() { [nameof(BitPivot.OverflowBehavior)] = BitPivotOverflowBehavior.Slide }, "a", "b");

        Assert.IsTrue(slide.QuerySelector(".bit-pvt-spv")!.HasAttribute("disabled"));
        Assert.IsTrue(slide.QuerySelector(".bit-pvt-snx")!.HasAttribute("disabled"));
    }

    private static async Task<IDocument> Prerender(Dictionary<string, object?> parameters, params string[] keys)
    {
        parameters[nameof(BitPivot.ChildContent)] = (RenderFragment)(builder =>
        {
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];

                builder.OpenRegion(i);
                builder.OpenComponent<BitPivotItem>(0);
                builder.AddComponentParameter(1, nameof(BitPivotItem.Key), key);
                builder.AddComponentParameter(2, nameof(BitPivotItem.HeaderText), key);
                builder.AddComponentParameter(3, nameof(BitPivotItem.ChildContent), (RenderFragment)(b => b.AddContent(0, key)));
                builder.CloseComponent();
                builder.CloseRegion();
            }
        });

        var html = await Prerenderer.RenderAsync<BitPivot>(parameters);

        return new HtmlParser().ParseDocument(html);
    }
}
