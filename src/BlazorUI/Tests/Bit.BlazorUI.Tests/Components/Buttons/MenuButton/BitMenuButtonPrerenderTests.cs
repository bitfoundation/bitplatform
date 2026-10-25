using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

/// <summary>
/// What a menu button sends before the page is interactive: its key guard is set up in the after-render pass, which
/// never runs on a prerender, so the header, the closed menu and its items have to come out whole without it.
/// </summary>
[TestClass]
public class BitMenuButtonPrerenderTests
{
    [TestMethod]
    public async Task BitMenuButtonShouldPrerenderTheHeaderAndTheClosedMenuWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitMenuButton<BitMenuButtonItem>>(new Dictionary<string, object?>
        {
            [nameof(BitMenuButton<BitMenuButtonItem>.Text)] = "Actions",
            [nameof(BitMenuButton<BitMenuButtonItem>.Items)] = new List<BitMenuButtonItem>
            {
                new() { Text = "Item A", Key = "A" },
                new() { Text = "Item B", Key = "B" }
            },
        });

        StringAssert.Contains(html, "bit-mnb-opb");
        StringAssert.Contains(html, "Actions");
        StringAssert.Contains(html, "aria-expanded=\"false\"");
        StringAssert.Contains(html, "role=\"menu\"");
        StringAssert.Contains(html, "Item A");
        StringAssert.Contains(html, "Item B");
        Assert.IsFalse(html.Contains("bit-mnb-omn"));
    }

    [TestMethod]
    public async Task BitMenuButtonShouldPrerenderTheStickyHeaderFromItsItemsWithoutJavaScript()
    {
        var html = await Prerenderer.RenderAsync<BitMenuButton<BitMenuButtonItem>>(new Dictionary<string, object?>
        {
            [nameof(BitMenuButton<BitMenuButtonItem>.Sticky)] = true,
            [nameof(BitMenuButton<BitMenuButtonItem>.Items)] = new List<BitMenuButtonItem>
            {
                new() { Text = "Item A", Key = "A" },
                new() { Text = "Item B", Key = "B", IsSelected = true }
            },
        });

        // the selected item is promoted to the header, and left out of the menu
        StringAssert.Contains(html, "Item B");
        Assert.AreEqual(1, CountOf(html, "bit-mnb-itm"));
    }

    private static int CountOf(string html, string token)
    {
        var count = 0;
        var index = 0;

        while ((index = html.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
