using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Pivot;

/// <summary>
/// Pins the public --bit-Pivot-* custom properties, which a bUnit render cannot see: the list the stylesheet
/// documents, the ones it actually reads, and the table the demo page (and so the MCP server) publishes have to
/// be the same list, and none of them may be declared by the component itself or it would stop inheriting.
/// </summary>
[TestClass]
public class BitPivotStylesheetTests
{
    private static readonly Regex PublicVariable = new(@"--bit-Pivot-[a-z-]+[a-z]");

    [TestMethod]
    public void BitPivotShouldReadEveryPublicVariableItDocuments()
    {
        var (documented, body) = SplitStylesheet();

        var read = PublicVariable.Matches(body).Select(m => m.Value).ToHashSet();

        CollectionAssert.AreEquivalent(documented, read.ToList(), "The documented and the consumed --bit-Pivot-* variables differ.");
    }

    [TestMethod]
    public void BitPivotShouldNeverDeclareAPublicVariable()
    {
        var (_, body) = SplitStylesheet();

        Assert.IsFalse(Regex.IsMatch(body, @"(^|[\s;{])--bit-Pivot-[a-z-]+\s*:", RegexOptions.Multiline),
                       "A --bit-Pivot-* variable is declared, so a value set on an ancestor would no longer reach the pivot.");
    }

    [TestMethod]
    public void BitPivotDemoShouldPublishEveryPublicVariable()
    {
        var (documented, _) = SplitStylesheet();

        var demo = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Navs", "Pivot", "BitPivotDemo.razor.cs");

        var published = Regex.Matches(demo, @"Name\s*=\s*""(--bit-Pivot-[a-z-]+)""").Select(m => m.Groups[1].Value).ToList();

        CollectionAssert.AreEquivalent(documented, published, "The componentCssVariables table of the demo page is out of step with the stylesheet.");
    }

    [TestMethod]
    public void BitPivotShouldResolveTheAccentOfEveryColorFromThePublicVariables()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "--bit-pvt-clr: var(--bit-Pivot-color, #{role($tokens, main)});");
        StringAssert.Contains(stylesheet, "--bit-pvt-clr-focus: var(--bit-Pivot-focus-color, #{role($tokens, focus)});");
        StringAssert.Contains(stylesheet, "--bit-pvt-clr-dis: var(--bit-Pivot-disabled-color, #{role($tokens, dis)});");
        StringAssert.Contains(stylesheet, "--bit-pvt-clr-dis-text: var(--bit-Pivot-disabled-text-color, #{role($tokens, dis-text)});");
    }

    [TestMethod]
    public void BitPivotShouldKeepANestedPivotFromInheritingTheGapAndSizeOfTheOneAroundIt()
    {
        var stylesheet = ReadStylesheet();

        // The Gap parameter is written inline on the root, and a custom property inherits: every pivot resets it.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvt {"), "--bit-pvt-gap: initial;");

        // The size is a step declared on each root rather than a rule reaching into the header of a pivot, which
        // would reach the header of a pivot nested in its panel as well.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvt-sm {"), "--bit-pvt-fs:");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvt-md {"), "--bit-pvt-fs:");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvt-lg {"), "--bit-pvt-fs:");
    }

    [TestMethod]
    public void BitPivotSizesShouldScaleTheHeightOffTheTabToken()
    {
        var stylesheet = ReadStylesheet();

        // A Small header that is as tall as a Medium one is not a size at all.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvt-sm {"), "--bit-pvt-ih: calc(#{$siz-tab} * 0.75);");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvt-md {"), "--bit-pvt-ih: #{$siz-tab};");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvt-lg {"), "--bit-pvt-ih: calc(#{$siz-tab} * 1.25);");

        // The public variable still wins over every size.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pvti {"), "height: var(--bit-Pivot-item-height, var(--bit-pvt-ih, #{$siz-tab}));");
    }

    [TestMethod]
    public void BitPivotDismissButtonShouldMeetTheMinimumTargetSize()
    {
        var block = GetBlock(ReadStylesheet(), "\n.bit-pvti-dbt {");

        // spacing(3) is 24px at the default scaling, the floor of WCAG 2.2 SC 2.5.8 for a target inside another one.
        StringAssert.Contains(block, "width: var(--bit-Pivot-dismiss-size, #{spacing(3)});");
        StringAssert.Contains(block, "height: var(--bit-Pivot-dismiss-size, #{spacing(3)});");
    }



    [TestMethod]
    public void BitPivotShouldNeverReachAPartThroughAPlainDescendantSelector()
    {
        // A rule nested in another one and written as a plain class is a descendant selector, which would reach
        // into the panel of the pivot as well - and so into a pivot nested there, taking on the position, the
        // header type and the state of the one around it. The parts of a pivot are reached from its root with
        // child combinators only (the in-header / in-tablist paths, or & and >).
        var (_, body) = SplitStylesheet();

        // Comments and interpolations carry characters of their own that would read as selectors or blocks.
        var source = Regex.Replace(body, @"//[^\n]*", "");
        source = Regex.Replace(source, @"#\{[^}]*\}", "INTERPOLATION");

        var stack = new System.Collections.Generic.Stack<string>();
        var leaks = new System.Collections.Generic.List<string>();
        var start = 0;

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];

            if (c == ';')
            {
                start = i + 1;
            }
            else if (c == '{')
            {
                var selector = source[start..i].Trim();

                var parent = stack.FirstOrDefault(s => s.StartsWith('@') is false);
                if (parent is not null && selector.StartsWith('@') is false)
                {
                    foreach (var part in selector.Split(',').Select(p => p.Trim()))
                    {
                        if (part.StartsWith('.')) leaks.Add($"{parent} {{ {part} }}");
                    }
                }

                stack.Push(selector);
                start = i + 1;
            }
            else if (c == '}')
            {
                if (stack.Count > 0) stack.Pop();
                start = i + 1;
            }
        }

        Assert.AreEqual(0, leaks.Count, $"Descendant selectors: {string.Join(" | ", leaks)}");
    }

    [TestMethod]
    public void BitPivotShouldDrawTheFocusRingInsideATabTheHeaderClips()
    {
        // The Menu, Slide and Scroll headers clip what overflows them, which would cut an outer ring off.
        var block = GetBlock(ReadStylesheet(), "\n.bit-pvt-mnu,\n.bit-pvt-sld,\n.bit-pvt-scr {");

        StringAssert.Contains(block, "#{$tab}:focus-visible");
        StringAssert.Contains(block, "box-shadow: inset 0 0 0 #{$shp-focus-ring-width} var(--bit-pvt-clr-focus);");
        StringAssert.Contains(block, "outline-offset: calc(-1 * #{$shp-focus-ring-width});");
    }

    [TestMethod]
    public void BitPivotShouldDrawTheDividerFromTheThemeToken()
    {
        var block = GetBlock(ReadStylesheet(), "\n.bit-pvt-hwr {");

        StringAssert.Contains(block, "height: var(--bit-Pivot-divider-thickness, #{$siz-tab-divider});");
        StringAssert.Contains(block, "background-color: var(--bit-Pivot-divider-color, #{$clr-brd-sec});");
    }



    private static (System.Collections.Generic.List<string> Documented, string Body) SplitStylesheet()
    {
        var stylesheet = ReadStylesheet();

        // The documentation is the run of comment lines before the first rule.
        var start = stylesheet.IndexOf("\n.bit-pvt {", System.StringComparison.Ordinal);
        Assert.IsTrue(start > 0, "The root rule was not found in the stylesheet.");

        var header = stylesheet[..start];
        var body = stylesheet[start..];

        var documented = Regex.Matches(header, @"^//\s+(--bit-Pivot-[a-z-]+)", RegexOptions.Multiline)
                              .Select(m => m.Groups[1].Value)
                              .ToList();

        Assert.IsTrue(documented.Count > 0, "The stylesheet documents no public variable.");
        Assert.AreEqual(documented.Count, documented.Distinct().Count(), "A public variable is documented twice.");

        return (documented, body);
    }

    private static string GetBlock(string stylesheet, string selector)
    {
        var start = stylesheet.IndexOf(selector, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet() => ReadFile("Bit.BlazorUI", "Components", "Navs", "Pivot", "BitPivot.scss");

    private static string ReadFile(params string[] parts) => ReadFileFrom(parts);

    private static string ReadFileFrom(string[] parts, [CallerFilePath] string thisFile = "")
    {
        var root = Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..");
        var path = Path.GetFullPath(Path.Combine([root, .. parts]));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
