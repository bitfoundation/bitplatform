using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.SearchBox;

/// <summary>
/// What a search box sends before the page is interactive (a prerender, or a static SSR page): no OnAfterRender runs,
/// so neither the keyboard guard of its input nor the focus shortcut is set up, and the field still has to be there
/// with the term it was given.
/// </summary>
[TestClass]
public class BitSearchBoxPrerenderTests
{
    private static readonly IReadOnlyList<string> Fruits = ["Apple", "Banana", "Orange"];

    [TestMethod]
    public async Task BitSearchBoxShouldPrerenderItsFieldWithoutTheBrowser()
    {
        var html = await Prerenderer.RenderAsync<BitSearchBox>(new Dictionary<string, object?>
        {
            [nameof(BitSearchBox.Label)] = "Search products",
            [nameof(BitSearchBox.Placeholder)] = "Find a product",
            [nameof(BitSearchBox.DefaultValue)] = "bit",
            [nameof(BitSearchBox.FocusShortcut)] = "Control+K",
            [nameof(BitSearchBox.ShowSearchButton)] = true,
        });

        StringAssert.Contains(html, "Search products");
        StringAssert.Contains(html, "placeholder=\"Find a product\"");
        StringAssert.Contains(html, "value=\"bit\"");
        StringAssert.Contains(html, "role=\"searchbox\"");
        // The renderer writes the plus sign as a character reference.
        StringAssert.Contains(System.Net.WebUtility.HtmlDecode(html), "aria-keyshortcuts=\"Control+K\"");
        StringAssert.Contains(html, "bit-srb-cbt");
        StringAssert.Contains(html, "bit-srb-sbn");
    }

    [TestMethod]
    public async Task BitSearchBoxShouldPrerenderAClosedComboboxWithItsSuggestSource()
    {
        var html = await Prerenderer.RenderAsync<BitSearchBox>(new Dictionary<string, object?>
        {
            [nameof(BitSearchBox.SuggestItems)] = Fruits,
        });

        // The callout is in the markup, closed, and the live region is empty: nothing has been searched for yet.
        StringAssert.Contains(html, "role=\"combobox\"");
        StringAssert.Contains(html, "aria-expanded=\"false\"");
        StringAssert.Contains(html, "bit-srb-cal");
        Assert.IsFalse(html.Contains("bit-srb-itm"));
    }
}
