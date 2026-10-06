using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Nav;

/// <summary>
/// What a nav of options sends before the page is interactive: the options register as they render and the
/// after-render pass never runs on a prerender (or on a static SSR page at all), so whatever the selection
/// needs has to be resolved within the render itself.
/// </summary>
[TestClass]
public class BitNavPrerenderTests
{
    [TestMethod]
    public async Task BitNavShouldSelectTheOptionMatchingTheUrlBeforeInteractivity()
    {
        var document = await Prerender("/products",
            Option("Home", "/"),
            Option("Products", "/products"),
            Option("Contact", "/contact"));

        var selected = document.QuerySelectorAll(".bit-nav-sel");

        Assert.AreEqual(1, selected.Length);
        Assert.AreEqual("Products", selected[0].QuerySelector(".bit-nav-itx")!.TextContent.Trim());
        Assert.AreEqual("page", selected[0].GetAttribute("aria-current"));
        Assert.AreEqual(1, document.QuerySelectorAll("[aria-current]").Length);
    }

    [TestMethod]
    public async Task BitNavShouldOpenThePathToTheOptionMatchingTheUrlBeforeInteractivity()
    {
        var document = await Prerender("/fruits/apple",
            Option("Home", "/"),
            Option("Fruits", null, children: [Option("Apple", "/fruits/apple")]));

        var selected = document.QuerySelectorAll(".bit-nav-sel");

        Assert.AreEqual(1, selected.Length);
        Assert.AreEqual("Apple", selected[0].QuerySelector(".bit-nav-itx")!.TextContent.Trim());

        // The branch that holds the selected option is opened, otherwise the selection would be out of sight.
        var branch = selected[0].Closest("ul")!;
        Assert.IsFalse(branch.GetAttribute("style")?.Contains("display:none") is true);
    }

    [TestMethod]
    public async Task BitNavShouldPreferTheNestedOptionMatchingTheUrlBeforeInteractivity()
    {
        // The parent matches the URL by its prefix and registers first, but the child it holds matches it too
        // and wins, exactly like the match the interactive render runs over the whole tree.
        var document = await Prerender("/docs/intro",
            Option("Docs", "/docs", match: BitNavMatch.Prefix, children: [Option("Intro", "/docs/intro")]));

        var selected = document.QuerySelectorAll(".bit-nav-sel");

        Assert.AreEqual(1, selected.Length);
        Assert.AreEqual("Intro", selected[0].QuerySelector(".bit-nav-itx")!.TextContent.Trim());
    }

    [TestMethod]
    public async Task BitNavShouldSelectNothingWhenNoOptionMatchesTheUrlBeforeInteractivity()
    {
        var document = await Prerender("/elsewhere",
            Option("Home", "/"),
            Option("Products", "/products"));

        Assert.AreEqual(0, document.QuerySelectorAll(".bit-nav-sel").Length);
        Assert.AreEqual(0, document.QuerySelectorAll("[aria-current]").Length);
    }

    [TestMethod]
    public async Task BitNavShouldSelectTheItemMatchingTheUrlBeforeInteractivity()
    {
        // The Items API, which already resolved its selection before the first render, for comparison.
        var html = await Prerenderer.RenderAsync<BitNav<BitNavItem>>(new Dictionary<string, object?>
        {
            [nameof(BitNav<BitNavItem>.Items)] = new List<BitNavItem>
            {
                new() { Text = "Home", Url = "/" },
                new() { Text = "Products", Url = "/products" },
            },
        }, services => services.AddSingleton<NavigationManager>(new TestNavigationManager("/products")));

        var selected = new HtmlParser().ParseDocument(html).QuerySelectorAll(".bit-nav-sel");

        Assert.AreEqual(1, selected.Length);
        Assert.AreEqual("Products", selected[0].QuerySelector(".bit-nav-itx")!.TextContent.Trim());
    }

    [TestMethod]
    public async Task BitNavShouldNotRaiseOnSelectItemOfTheItemsApiBeforeInteractivity()
    {
        // A prerender only draws the selection: the interactive render that replaces it reports it, where a handler
        // is free to call JavaScript or navigate.
        var raised = 0;
        var html = await Prerenderer.RenderAsync<BitNav<BitNavItem>>(new Dictionary<string, object?>
        {
            [nameof(BitNav<BitNavItem>.Items)] = new List<BitNavItem>
            {
                new() { Text = "Home", Url = "/" },
                new() { Text = "Products", Url = "/products" },
            },
            [nameof(BitNav<BitNavItem>.OnSelectItem)] = EventCallback.Factory.Create<BitNavItem>(new object(), _ => raised++),
        }, services => services.AddSingleton<NavigationManager>(new TestNavigationManager("/products")));

        var selected = new HtmlParser().ParseDocument(html).QuerySelectorAll(".bit-nav-sel");

        Assert.AreEqual(1, selected.Length);
        Assert.AreEqual("Products", selected[0].QuerySelector(".bit-nav-itx")!.TextContent.Trim());
        Assert.AreEqual(0, raised);
    }



    private static async Task<IDocument> Prerender(string url, params RenderFragment[] options)
    {
        var html = await Prerenderer.RenderAsync<BitNav<BitNavOption>>(new Dictionary<string, object?>
        {
            [nameof(BitNav<BitNavOption>.ChildContent)] = Join(options),
        }, services => services.AddSingleton<NavigationManager>(new TestNavigationManager(url)));

        return new HtmlParser().ParseDocument(html);
    }

    private static RenderFragment Option(string text, string? url, BitNavMatch? match = null, params RenderFragment[] children) => builder =>
    {
        builder.OpenComponent<BitNavOption>(0);
        builder.AddComponentParameter(1, nameof(BitNavOption.Text), text);
        builder.AddComponentParameter(2, nameof(BitNavOption.Url), url);
        builder.AddComponentParameter(3, nameof(BitNavOption.Match), match);
        if (children.Length > 0)
        {
            builder.AddComponentParameter(4, nameof(BitNavOption.ChildContent), Join(children));
        }
        builder.CloseComponent();
    };

    private static RenderFragment Join(RenderFragment[] fragments) => builder =>
    {
        foreach (var (fragment, index) in fragments.Select((f, i) => (f, i)))
        {
            builder.OpenRegion(index);
            fragment(builder);
            builder.CloseRegion();
        }
    };

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string path) => Initialize("https://localhost/", $"https://localhost{path}");
    }
}
