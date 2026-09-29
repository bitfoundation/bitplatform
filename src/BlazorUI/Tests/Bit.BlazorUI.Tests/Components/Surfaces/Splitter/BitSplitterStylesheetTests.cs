using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Splitter;

/// <summary>
/// Pins what a bUnit render cannot see of the splitter: its public --bit-Splitter-* variables, which the stylesheet
/// reads and the header comment of that stylesheet documents, and the private size variables every splitter starts
/// over from rather than inheriting from the one it is nested in.
/// </summary>
[TestClass]
public class BitSplitterStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Splitter-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Splitter-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Splitter-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitSplitterShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray(),
            "The --bit-Splitter-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitSplitterShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every splitter
        // below it: a declaration on the splitter would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitSplitterShouldStartEveryPanelSizeOverRatherThanInheritIt()
    {
        // A splitter nested in the panel of another would otherwise take the outer one's split, minimums and
        // maximums for its own wherever it declares none.
        var root = GetBlock(ReadStylesheet(), "\n.bit-spl {");

        foreach (var name in new[] { "fpn-size", "fpn-grow", "fpn-max", "fpn-min", "spn-size", "spn-grow", "spn-max", "spn-min", "col-size" })
        {
            StringAssert.Contains(root, $"--bit-spl-{name}: initial;");
        }
    }

    [TestMethod]
    public void BitSplitterShouldReadOnlyPrefixedPrivateVariables()
    {
        // Unprefixed custom properties on the root would be inherited by, and collide with, whatever the panels hold.
        var unprefixed = Regex.Matches(ReadStylesheet(), @"var\(\s*(--(?!bit-)[a-zA-Z0-9-]+)")
                              .Select(m => m.Groups[1].Value)
                              .Distinct()
                              .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), unprefixed);
    }

    [TestMethod]
    public void BitSplitterGutterShouldLetTheParametersWinOverTheVariables()
    {
        // GutterSize and GutterHitSize write the private variable inline, which beats the class declaring it from
        // the public one.
        var root = GetBlock(ReadStylesheet(), "\n.bit-spl {");

        StringAssert.Contains(root, "--bit-spl-gtr-size: var(--bit-Splitter-gutter-size, #{spacing(1.25)});");
        StringAssert.Contains(root, "--bit-spl-hit-size: var(--bit-Splitter-gutter-hit-size, #{spacing(3)});");
    }

    [TestMethod]
    public void BitSplitterCoarsePointerShouldStillReadTheHitSizeVariable()
    {
        var stylesheet = ReadStylesheet();
        var coarse = stylesheet[stylesheet.IndexOf("@media (pointer: coarse)", StringComparison.Ordinal)..];

        StringAssert.Contains(coarse, "--bit-spl-hit-size: var(--bit-Splitter-gutter-hit-size, #{spacing(5.5)});");
    }

    [TestMethod]
    public void BitSplitterCollapseButtonShouldReachTheTargetSize()
    {
        // The button is drawn 1.75 spacing units across the gutter; the part answering a press reaches out to 3.
        var button = GetBlock(ReadStylesheet(), "\n.bit-spl-cbt {");

        StringAssert.Contains(button, "&::before {");
        StringAssert.Contains(button, "calc((spacing(1.75) - spacing(3)) / 2)");
    }

    [TestMethod]
    public void BitSplitterGripShouldBeDrawnInAColorWithAContrastFloor()
    {
        // The gutter at rest is the decorative stroke tier, so the grip is what keeps the control at 3:1 (SC 1.4.11):
        // the secondary foreground has that floor over the gutter, the primary stroke does not.
        var grip = GetBlock(ReadStylesheet(), "\n.bit-spl-gti {");

        StringAssert.Contains(grip, "var(--bit-Splitter-gutter-indicator-color, #{$clr-fg-sec})");
    }

    private static string GetBlock(string stylesheet, string selector, string terminator = "\n}")
    {
        var start = stylesheet.IndexOf(selector, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf(terminator, start, StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Surfaces", "Splitter", "BitSplitter.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
