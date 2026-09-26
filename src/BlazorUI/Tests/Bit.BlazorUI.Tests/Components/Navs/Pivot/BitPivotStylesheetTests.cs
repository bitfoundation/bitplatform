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
    public void BitPivotDismissButtonShouldMeetTheMinimumTargetSize()
    {
        var block = GetBlock(ReadStylesheet(), "\n.bit-pvti-dbt {");

        // spacing(3) is 24px at the default scaling, the floor of WCAG 2.2 SC 2.5.8 for a target inside another one.
        StringAssert.Contains(block, "width: var(--bit-Pivot-dismiss-size, #{spacing(3)});");
        StringAssert.Contains(block, "height: var(--bit-Pivot-dismiss-size, #{spacing(3)});");
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
