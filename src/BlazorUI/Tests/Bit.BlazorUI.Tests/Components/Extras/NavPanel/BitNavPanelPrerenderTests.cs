using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NavPanel;

/// <summary>
/// What a nav panel sends before the page is interactive: the after-render pass never runs on a prerender (or
/// on a static SSR page at all), so a search text the panel is handed has to filter the nav within the render.
/// </summary>
[TestClass]
public class BitNavPanelPrerenderTests
{
    private static IList<BitNavItem> TreeItems() =>
    [
        new() { Text = "Home", Url = "/home", Description = "Home page" },
        new()
        {
            Text = "AdminPanel",
            ChildItems =
            [
                new() { Text = "Dashboard", Url = "/dashboard" },
                new() { Text = "Categories", Url = "/categories" },
            ]
        },
        new() { Text = "Settings", Url = "/settings" }
    ];

    [TestMethod]
    public async Task BitNavPanelShouldFilterByTheSearchTextBeforeInteractivity()
    {
        var document = await Prerender("settings");

        CollectionAssert.AreEqual(new[] { "Settings" }, GetItemTexts(document));
    }

    [TestMethod]
    public async Task BitNavPanelShouldFilterTheNestedItemsByTheSearchTextBeforeInteractivity()
    {
        var document = await Prerender("categories");

        CollectionAssert.AreEqual(new[] { "Categories" }, GetItemTexts(document));
    }

    [TestMethod]
    public async Task BitNavPanelShouldShowTheEmptyListMessageForASearchWithoutMatchesBeforeInteractivity()
    {
        var document = await Prerender("nothing-matches-this");

        Assert.AreEqual(0, GetItemTexts(document).Length);
        StringAssert.Contains(document.Body!.TextContent, "Nothing found!");
    }

    [TestMethod]
    public async Task BitNavPanelShouldListEveryItemWithoutASearchTextBeforeInteractivity()
    {
        var document = await Prerender(null);

        // The children of the collapsed AdminPanel are not rendered until it is expanded.
        CollectionAssert.AreEqual(new[] { "Home", "AdminPanel", "Settings" }, GetItemTexts(document), string.Join(",", GetItemTexts(document)));
    }



    private static async Task<IDocument> Prerender(string? searchText)
    {
        var html = await Prerenderer.RenderAsync<BitNavPanel<BitNavItem>>(new Dictionary<string, object?>
        {
            [nameof(BitNavPanel<BitNavItem>.Items)] = TreeItems(),
            [nameof(BitNavPanel<BitNavItem>.SearchText)] = searchText,
        });

        return new HtmlParser().ParseDocument(html);
    }

    private static string[] GetItemTexts(IDocument document) =>
        [.. document.QuerySelectorAll(".bit-nav-ict .bit-nav-itx").Select(e => e.TextContent.Trim())];
}
