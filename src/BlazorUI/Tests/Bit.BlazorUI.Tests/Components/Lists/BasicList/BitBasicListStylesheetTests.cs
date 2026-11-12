using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.BasicList;

/// <summary>
/// Pins what a bUnit render cannot see of the list's stylesheet: the public --bit-BasicList-* variables are read with a
/// fallback and never declared (so they inherit from :root, an ancestor or the Style of an instance), the default
/// LoadMore button is painted from state variables its hover and active rules move rather than from colors they set
/// (so a class the app hands it keeps its paint under the pointer), and the rules reach exactly the parts the markup
/// renders them for.
/// </summary>
[TestClass]
public class BitBasicListStylesheetTests : BunitTestContext
{
    [TestMethod]
    public void BitBasicListShouldReadItsHeightFromThePublicVariableOnAVerticalListOnly()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("height: var(--bit-BasicList-height, #{spacing(62.5)});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bsl {"));

        // A row of items is as tall as its items are, so the horizontal class drops the height of the base class.
        Assert.Contains("height: unset;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bsl-hrz {"));

        var component = RenderComponent<BitBasicList<string>>(parameters =>
        {
            parameters.Add(p => p.Items, Items(3));
            parameters.Add(p => p.Horizontal, true);
        });

        Assert.IsTrue(component.Find(".bit-bsl").ClassList.Contains("bit-bsl-hrz"));
    }

    [TestMethod]
    public void BitBasicListShouldPaintTheDefaultLoadMoreButtonFromTheVariablesItsStatesMove()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\nbutton.bit-bsl-lmb {");

        // The one rule that paints is the one at rest, reading a state variable ahead of the public one; each state
        // only moves the variable, so a class the app hands the button keeps its paint under the pointer.
        Assert.Contains("--bit-bsl-lmb-sfg: initial;", block);
        Assert.Contains("--bit-bsl-lmb-sbg: initial;", block);
        Assert.Contains("color: var(--bit-bsl-lmb-sfg, var(--bit-BasicList-load-more-color, #{$clr-pri}));", block);
        Assert.Contains("background-color: var(--bit-bsl-lmb-sbg, var(--bit-BasicList-load-more-background, transparent));", block);

        var hover = SourceFiles.GetScssBlock(block, "&:hover {");

        Assert.Contains("--bit-bsl-lmb-sfg: var(--bit-BasicList-load-more-hover-color, #{$clr-pri-hover});", hover);
        Assert.Contains("--bit-bsl-lmb-sbg: var(--bit-BasicList-load-more-hover-background, #{$clr-bg-pri-hover});", hover);
        Assert.IsFalse(Regex.IsMatch(hover, @"\n\s*(?:color|background(?:-color)?)\s*:"), "The hover rule paints the button instead of moving its variables.");

        // A pressed button falls back to its hover background, so an app that restyles the hover alone is not left
        // with the default flash while the pointer is down.
        var active = SourceFiles.GetScssBlock(block, "&:active {");

        Assert.Contains("--bit-bsl-lmb-sbg: var(--bit-BasicList-load-more-active-background, var(--bit-BasicList-load-more-hover-background, #{$clr-bg-pri-active}));", active);
        Assert.IsFalse(Regex.IsMatch(active, @"\n\s*(?:color|background(?:-color)?)\s*:"), "The active rule paints the button instead of moving its variables.");

        // A disabled button is read before anything else and paints directly, as every disabled state does.
        Assert.Contains("color: var(--bit-BasicList-disabled-color, #{$clr-fg-dis});", SourceFiles.GetScssBlock(block, "&:disabled {"));
    }

    [TestMethod]
    public void BitBasicListShouldRenderTheDefaultLoadMoreAsTheButtonTheLookIsWrittenFor()
    {
        // The look of the default LoadMore is keyed on the element rather than on the class alone, so a template's
        // clickable wrapper - a plain element with the button role, since a template may render a button of its own -
        // is drawn exactly the way it was written.
        var defaulted = RenderComponent<BitBasicList<string>>(parameters =>
        {
            parameters.Add(p => p.Items, Items(30));
            parameters.Add(p => p.LoadMore, true);
        });

        var button = defaulted.Find(".bit-bsl-lmb");

        Assert.AreEqual("BUTTON", button.TagName);
        Assert.IsNotNull(button.QuerySelector(".bit-bsl-lmt"));

        var templated = RenderComponent<BitBasicList<string>>(parameters =>
        {
            parameters.Add(p => p.Items, Items(30));
            parameters.Add(p => p.LoadMore, true);
            parameters.Add(p => p.LoadMoreTemplate, (RenderFragment<bool>)(loading => builder => builder.AddContent(0, loading ? "Wait" : "More")));
        });

        var wrapper = templated.Find(".bit-bsl-lmb");

        Assert.AreEqual("DIV", wrapper.TagName);
        Assert.AreEqual("button", wrapper.GetAttribute("role"));
        Assert.IsNull(wrapper.QuerySelector(".bit-bsl-lmt"));
    }

    [TestMethod]
    public void BitBasicListShouldDrawItsSpinnersFromThePublicVariables()
    {
        var stylesheet = ReadStylesheet();

        // One ring from the shared mixin, sized and colored by the public variables, for the loading content and the
        // busy LoadMore button alike.
        Assert.Contains("@include spinner-ring(var(--bit-BasicList-spinner-size, #{$siz-icon-md}), var(--bit-BasicList-spinner-color, #{$clr-pri}), $clr-brd-sec);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bsl-spn {"));

        Assert.Contains("color: var(--bit-BasicList-loading-color, #{$clr-fg-sec});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bsl-ldt {"));
        Assert.Contains("padding: var(--bit-BasicList-loading-padding, #{spacing(2)});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bsl-ldc {"));

        var component = RenderComponent<BitBasicList<string>>(parameters =>
        {
            parameters.Add(p => p.Items, Items(3));
            parameters.Add(p => p.Loading, true);
        });

        var loading = component.Find(".bit-bsl-ldc");

        Assert.IsNotNull(loading.QuerySelector(".bit-bsl-spn"));
        Assert.IsNotNull(loading.QuerySelector(".bit-bsl-ldt"));
    }

    [TestMethod]
    public void BitBasicListShouldDrawTheFocusOfTheLoadMoreInsideTheScrollingRegion()
    {
        var stylesheet = ReadStylesheet();

        // The shared ring is a box-shadow outside the box, which the scrolling region clips, so the button's is drawn
        // inside the element instead; both read the same public focus color.
        Assert.Contains("@include focus-ring-inset(var(--bit-BasicList-focus-color, #{$clr-pri-focus})", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bsl-lmb {"));
        Assert.Contains("@include focus-ring-own(var(--bit-BasicList-focus-color));", SourceFiles.GetScssBlock(stylesheet, "\n.bit-bsl {"));
    }

    [TestMethod]
    public void BitBasicListShouldNeverDeclareAPublicVariable()
    {
        var code = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(code, @"--bit-BasicList-[a-z-]+\s*:"), "A public --bit-BasicList-* variable is declared by the stylesheet.");
    }

    [TestMethod]
    public void BitBasicListShouldReadEveryPublicVariableItsHeaderDocuments()
    {
        var stylesheet = ReadStylesheet();
        var code = SourceFiles.StripScssComments(stylesheet);

        var documented = Regex.Matches(stylesheet, @"^//\s+(?<name>--bit-BasicList-[a-z-]+)\s", RegexOptions.Multiline)
                              .Select(m => m.Groups["name"].Value)
                              .Distinct()
                              .ToArray();

        // Guards the guard: a header that documents nothing would let every variable through in silence.
        Assert.IsGreaterThanOrEqualTo(15, documented.Length, $"The header documents only {documented.Length} variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}", code, $"{name} is documented but never read.");
        }

        // And nothing is read that the header does not document.
        var read = Regex.Matches(code, @"var\((?<name>--bit-BasicList-[a-z-]+)").Select(m => m.Groups["name"].Value).Distinct();

        var undocumented = read.Where(n => documented.Contains(n) is false).ToArray();

        Assert.IsEmpty(undocumented, $"Read but not documented: {string.Join(", ", undocumented)}.");
    }

    private static ICollection<string> Items(int count) => Enumerable.Range(1, count).Select(i => $"Item {i}").ToList();

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Lists", "BasicList", "BitBasicList.scss");
}
