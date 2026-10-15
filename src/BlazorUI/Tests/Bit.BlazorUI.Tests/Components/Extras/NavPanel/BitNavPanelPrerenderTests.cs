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



    [TestMethod]
    public async Task BitNavPanelShouldPrerenderAnAlwaysDrawerAsAModalDialog()
    {
        // The two breakpoints whose shape is the same on every screen need no browser to find it out, so a prerendered
        // page already carries the dialog, the overlay and the shape class.
        var html = await Prerenderer.RenderAsync<BitNavPanel<BitNavItem>>(new Dictionary<string, object?>
        {
            [nameof(BitNavPanel<BitNavItem>.Items)] = TreeItems(),
            [nameof(BitNavPanel<BitNavItem>.IsOpen)] = true,
            [nameof(BitNavPanel<BitNavItem>.DrawerBreakpoint)] = BitNavPanelBreakpoint.Always,
        });

        var document = new HtmlParser().ParseDocument(html);
        var root = document.QuerySelector(".bit-npn")!;

        Assert.IsTrue(root.ClassList.Contains("bit-npn-drw"));
        Assert.AreEqual("dialog", root.GetAttribute("role"));
        Assert.AreEqual("true", root.GetAttribute("aria-modal"));
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
        Assert.AreEqual("Navigation", root.GetAttribute("aria-label"));
        Assert.IsTrue(document.QuerySelector(".bit-npn-ovl")!.ClassList.Contains("bit-npn-drw"));
    }

    [TestMethod]
    public async Task BitNavPanelShouldPrerenderTheBreakpointUntilTheBrowserAnswers()
    {
        // Until the browser has reported the screen, the breakpoint itself is the class the stylesheet splits at, the
        // overlay carries it too, and nothing says dialog yet.
        var html = await Prerenderer.RenderAsync<BitNavPanel<BitNavItem>>(new Dictionary<string, object?>
        {
            [nameof(BitNavPanel<BitNavItem>.Items)] = TreeItems(),
            [nameof(BitNavPanel<BitNavItem>.IsOpen)] = true,
            [nameof(BitNavPanel<BitNavItem>.DrawerBreakpoint)] = BitNavPanelBreakpoint.Lg,
        });

        var document = new HtmlParser().ParseDocument(html);
        var root = document.QuerySelector(".bit-npn")!;

        Assert.IsTrue(root.ClassList.Contains("bit-npn-blg"));
        Assert.IsFalse(root.ClassList.Contains("bit-npn-drw"));
        Assert.IsFalse(root.ClassList.Contains("bit-npn-col"));
        Assert.IsNull(root.GetAttribute("role"));
        Assert.IsTrue(document.QuerySelector(".bit-npn-ovl")!.ClassList.Contains("bit-npn-blg"));
    }

    [TestMethod]
    public async Task BitNavPanelShouldPrerenderANeverDrawerAsAColumnWithoutAnOverlay()
    {
        var html = await Prerenderer.RenderAsync<BitNavPanel<BitNavItem>>(new Dictionary<string, object?>
        {
            [nameof(BitNavPanel<BitNavItem>.Items)] = TreeItems(),
            [nameof(BitNavPanel<BitNavItem>.IsOpen)] = true,
            [nameof(BitNavPanel<BitNavItem>.DrawerBreakpoint)] = BitNavPanelBreakpoint.Never,
        });

        var document = new HtmlParser().ParseDocument(html);
        var root = document.QuerySelector(".bit-npn")!;

        Assert.IsTrue(root.ClassList.Contains("bit-npn-col"));
        Assert.IsNull(root.GetAttribute("role"));
        Assert.IsNull(document.QuerySelector(".bit-npn-ovl"));
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
