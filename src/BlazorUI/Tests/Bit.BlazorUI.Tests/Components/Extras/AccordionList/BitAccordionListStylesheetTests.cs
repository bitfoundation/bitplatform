using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccordionList;

/// <summary>
/// Pins the contract of the public --bit-AccordionList-* CSS variables, which a bUnit render cannot see: they are read
/// with a fallback and never declared (so they inherit), and the demo page documents every one of them. Also covers
/// what the Joined list asks of the stylesheet.
/// </summary>
[TestClass]
public class BitAccordionListStylesheetTests : BunitTestContext
{
    private static readonly string[] PublicVariables =
    [
        "--bit-AccordionList-gap",
        "--bit-AccordionList-divider-color",
    ];

    [TestMethod]
    public void BitAccordionListShouldReadEveryPublicVariableWithAFallbackAndNeverDeclareIt()
    {
        var stylesheet = StripComments(ReadStylesheet());

        foreach (var variable in PublicVariables)
        {
            Assert.IsTrue(stylesheet.Contains($"var({variable}, "), $"{variable} is never read with a fallback.");
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"(^|[\s;{{]){Regex.Escape(variable)}\s*:", RegexOptions.Multiline), $"{variable} is declared, which stops it from inheriting.");
        }

        var read = Regex.Matches(stylesheet, @"var\((--bit-AccordionList-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.IsSubsetOf(read.ToArray(), PublicVariables);
    }

    [TestMethod]
    public void BitAccordionListShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        var demo = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "AccordionList", "BitAccordionListDemo.razor.cs");

        var documented = Regex.Matches(demo, @"Name = ""(--bit-AccordionList-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, documented);
    }

    [TestMethod]
    public void BitAccordionListJoinedShouldReachOnlyTheListsOwnItems()
    {
        var stylesheet = StripComments(ReadStylesheet());

        // Every rule of the joined list is chained through the item wrapper, so an accordion nested in a panel keeps
        // its own corners and its own outline.
        var joined = stylesheet[stylesheet.IndexOf(".bit-acl-jnd {", System.StringComparison.Ordinal)..];

        foreach (Match rule in Regex.Matches(joined, @"^\s*(>[^{]+)\{", RegexOptions.Multiline))
        {
            StringAssert.StartsWith(rule.Groups[1].Value.Trim(), "> .bit-acl-itm", $"'{rule.Groups[1].Value.Trim()}' does not start at the item wrapper.");
        }

        StringAssert.Contains(joined, "gap: 0;");

        // The shared line is the top border of the lower item alone, so nothing pairs an item with the one above
        // it: no adjacency, and no overlap sized by one item's border for a line that belongs to the other.
        StringAssert.Contains(joined, "border-block-end-width: 0;");
        Assert.IsFalse(joined.Contains("margin-block-start"), "The shared line is drawn by overlapping two borders again.");
        Assert.IsFalse(Regex.IsMatch(joined, @"\+\s*\.bit-acl-itm"), "A rule pairs adjacent items, which markup between the options breaks.");
    }

    [TestMethod]
    public void BitAccordionListJoinedShouldFindTheLastItemWithoutHas()
    {
        var stylesheet = StripComments(ReadStylesheet());

        // :has() is the newest of the selectors that could find it; the last item is found with the older
        // `of S` syntax instead, behind a plain :last-child for the engines that predate that too.
        Assert.IsFalse(stylesheet.Contains(":has("));
        StringAssert.Contains(stylesheet, "> .bit-acl-itm:last-child > .bit-acd");
        StringAssert.Contains(stylesheet, "> .bit-acl-itm:nth-last-child(1 of .bit-acl-itm) > .bit-acd");
    }

    [TestMethod]
    public void BitAccordionListJoinedShouldSetTheClassAndIgnoreTheGap()
    {
        var component = RenderComponent<BitAccordionList<BitAccordionListItem>>(parameters =>
        {
            parameters.Add(p => p.Items, new List<BitAccordionListItem> { new() { Title = "A" }, new() { Title = "B" } });
            parameters.Add(p => p.Joined, true);
            parameters.Add(p => p.Gap, 16);
        });

        var root = component.Find(".bit-acl");

        Assert.IsTrue(root.ClassList.Contains("bit-acl-jnd"));
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("gap:"));

        component.Render(parameters => parameters.Add(p => p.Joined, false));

        root = component.Find(".bit-acl");

        Assert.IsFalse(root.ClassList.Contains("bit-acl-jnd"));
        StringAssert.Contains(root.GetAttribute("style"), "gap:16px");
    }

    private static string StripComments(string stylesheet)
    {
        return Regex.Replace(stylesheet, @"//[^\n]*", string.Empty);
    }

    private static string ReadStylesheet()
    {
        return ReadFile("Bit.BlazorUI.Extras", "Components", "AccordionList", "BitAccordionList.scss");
    }

    // The path is relative to the BlazorUI folder; the test project copies each file it reads to the same path under
    // the output directory.
    private static string ReadFile(params string[] segments)
    {
        var path = Path.Combine([System.AppContext.BaseDirectory, .. segments]);

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
