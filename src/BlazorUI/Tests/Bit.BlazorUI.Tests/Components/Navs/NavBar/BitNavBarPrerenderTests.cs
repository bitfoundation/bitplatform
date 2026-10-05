using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

/// <summary>
/// What a navbar of options sends before the page is interactive: the options register as they render and the
/// after-render pass never runs on a prerender (or on a static SSR page at all), so whatever the selection
/// needs has to be resolved within the render itself.
/// </summary>
[TestClass]
public class BitNavBarPrerenderTests
{
    [TestMethod]
    public async Task BitNavBarShouldSelectTheOptionMatchingTheUrlBeforeInteractivity()
    {
        var document = await Prerender("/profile", [],
            Option("Home", "/"),
            Option("Profile", "/profile"),
            Option("Settings", "/settings"));

        AssertSelected(document, "Profile");
    }

    [TestMethod]
    public async Task BitNavBarShouldSelectTheFirstOptionMatchingTheUrlBeforeInteractivity()
    {
        // Two options pointing at the same page: the first one in the markup wins, exactly like the match the
        // interactive render runs.
        var document = await Prerender("/profile", [],
            Option("Home", "/"),
            Option("Profile", "/profile"),
            Option("Account", "/profile"));

        AssertSelected(document, "Profile");
    }

    [TestMethod]
    public async Task BitNavBarShouldSelectNothingWhenNoOptionMatchesTheUrlBeforeInteractivity()
    {
        var document = await Prerender("/elsewhere", [],
            Option("Home", "/"),
            Option("Profile", "/profile"));

        Assert.AreEqual(0, document.QuerySelectorAll(".bit-nbr-sel").Length);
        Assert.AreEqual(0, document.QuerySelectorAll("[aria-current]").Length);
    }

    [TestMethod]
    public async Task BitNavBarShouldSelectTheOptionOfTheDefaultSelectedKeyBeforeInteractivity()
    {
        var document = await Prerender("/", new()
        {
            [nameof(BitNavBar<BitNavBarOption>.Mode)] = BitNavMode.Manual,
            [nameof(BitNavBar<BitNavBarOption>.DefaultSelectedKey)] = "products",
        },
            Option("Home", null, "home"),
            Option("Products", null, "products"),
            Option("Settings", null, "settings"));

        AssertSelected(document, "Products");
    }

    [TestMethod]
    public async Task BitNavBarShouldSelectTheOptionOfTheSelectedKeyBeforeInteractivity()
    {
        var document = await Prerender("/", new()
        {
            [nameof(BitNavBar<BitNavBarOption>.Mode)] = BitNavMode.Manual,
            [nameof(BitNavBar<BitNavBarOption>.SelectedKey)] = "settings",
        },
            Option("Home", null, "home"),
            Option("Products", null, "products"),
            Option("Settings", null, "settings"));

        AssertSelected(document, "Settings");
    }

    [TestMethod]
    public async Task BitNavBarShouldNotRaiseOnSelectItemBeforeInteractivity()
    {
        // A prerender only draws the selection: the interactive render that replaces it reports it, after its
        // first render, where a handler is free to call JavaScript or navigate.
        var raised = 0;
        var document = await Prerender("/profile", new()
        {
            [nameof(BitNavBar<BitNavBarOption>.OnSelectItem)] = EventCallback.Factory.Create<BitNavBarOption>(new object(), _ => raised++),
        },
            Option("Home", "/"),
            Option("Profile", "/profile"));

        AssertSelected(document, "Profile");
        Assert.AreEqual(0, raised);
    }



    private static void AssertSelected(IDocument document, string text)
    {
        var selected = document.QuerySelectorAll(".bit-nbr-sel");

        Assert.AreEqual(1, selected.Length);
        Assert.AreEqual(text, selected[0].QuerySelector(".bit-nbr-txc")!.TextContent.Trim());
        Assert.AreEqual(1, document.QuerySelectorAll("[aria-current]").Length);
    }

    private static async Task<IDocument> Prerender(string url, Dictionary<string, object?> parameters, params RenderFragment[] options)
    {
        parameters[nameof(BitNavBar<BitNavBarOption>.ChildContent)] = (RenderFragment)(builder =>
        {
            foreach (var (option, index) in options.Select((o, i) => (o, i)))
            {
                builder.OpenRegion(index);
                option(builder);
                builder.CloseRegion();
            }
        });

        var html = await Prerenderer.RenderAsync<BitNavBar<BitNavBarOption>>(parameters,
            services => services.AddSingleton<NavigationManager>(new TestNavigationManager(url)));

        return new HtmlParser().ParseDocument(html);
    }

    private static RenderFragment Option(string text, string? url, string? key = null) => builder =>
    {
        builder.OpenComponent<BitNavBarOption>(0);
        builder.AddComponentParameter(1, nameof(BitNavBarOption.Text), text);
        builder.AddComponentParameter(2, nameof(BitNavBarOption.Url), url);
        builder.AddComponentParameter(3, nameof(BitNavBarOption.Key), key);
        builder.CloseComponent();
    };

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string path) => Initialize("https://localhost/", $"https://localhost{path}");
    }
}
