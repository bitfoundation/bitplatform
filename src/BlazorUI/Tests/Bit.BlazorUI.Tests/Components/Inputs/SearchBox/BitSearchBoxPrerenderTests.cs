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

        Assert.Contains("Search products", html);
        Assert.Contains("placeholder=\"Find a product\"", html);
        Assert.Contains("value=\"bit\"", html);
        Assert.Contains("role=\"searchbox\"", html);
        // The renderer writes the plus sign as a character reference.
        Assert.Contains("aria-keyshortcuts=\"Control+K\"", System.Net.WebUtility.HtmlDecode(html));
        Assert.Contains("bit-srb-cbt", html);
        Assert.Contains("bit-srb-sbn", html);
    }

    [TestMethod]
    public async Task BitSearchBoxShouldPrerenderAClosedComboboxWithItsSuggestSource()
    {
        var html = await Prerenderer.RenderAsync<BitSearchBox>(new Dictionary<string, object?>
        {
            [nameof(BitSearchBox.SuggestItems)] = Fruits,
        });

        // The callout is in the markup, closed, and the live region is empty: nothing has been searched for yet.
        Assert.Contains("role=\"combobox\"", html);
        Assert.Contains("aria-expanded=\"false\"", html);
        Assert.Contains("bit-srb-cal", html);
        Assert.DoesNotContain("bit-srb-itm", html);
    }
}
